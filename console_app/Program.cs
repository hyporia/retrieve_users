using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Data.Sql;
using System.Data.SqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using console_app.models;
using System.Data.Entity.Migrations;
using System.Threading;
using System.Threading.Tasks.Dataflow;
using System.Data;
using System.Runtime.InteropServices;

namespace console_app
{
	class Program
	{
		/// <summary>
		/// Путь к файлу 10000 пользователей с повторяющимися id
		/// </summary>
		static string filePath = "../../users10000.txt";
		static string logFilePath = "../../log.txt";
		static string connectionString = @"Server=localhost\SQLEXPRESS;Database=userupd;Trusted_Connection=True;";
		static readonly object _syncObject = new object();

		/// <summary>
		/// Генерация строки sql запроса обновления или добавления пользователя
		/// </summary>
		/// <param name="user">Сущность пользователя</param>
		/// <returns></returns>
		static string createAddOrUpdateSql(User user)
		{
			return $"begin tran" +
					$"	update users with(serializable) set Name = '{user.Name}', Email = '{user.Email}', City = '{user.City}', Phone = '{user.Phone}', UpdDate = '{user.UpdDate}'" +
					$"	where Id = {user.Id} and '{user.UpdDate}' > UpdDate;" +
					$"	set @updated = @@rowcount " +
					$"	declare @temp int; set @temp = -1; " +
					$"	if @updated = 0 " +
					$"	begin 	  " +
					$"		set @temp = (select count(*) from users where Id = {user.Id}) " +
					$"		if @temp = 0 " +
					$"		begin" +
					$"			insert into users(Id, Name, Email, City, Phone, UpdDate) " +
					$"			values({user.Id}, '{user.Name}', '{user.Email}', '{user.City}', '{user.Phone}', '{user.UpdDate}'); " +
					$"		end " +
					$"	end" +
					$"	set @existed = @temp " +
					$"commit tran";
		}

		/// <summary>
		/// Функция для логгирования
		/// </summary>
		/// <param name="fileStreamWriter"></param>
		/// <param name="change"></param>
		static void Log(StreamWriter fileStreamWriter, string change)
		{
			try
			{
				Console.WriteLine(change);
			}
			catch(Exception ex)	{}
			try
			{
				lock (_syncObject)
				{
					fileStreamWriter.WriteLine(change);
				}
			}catch(Exception ex){}

		}

		/// <summary>
		/// Создание выходного параметра для sql запроса
		/// </summary>
		/// <param name="parameterName"></param>
		/// <returns></returns>
		static SqlParameter createOutputIntSqlParameter(string parameterName)
		{
			return new SqlParameter(parameterName, SqlDbType.Int)
			{
				Direction = ParameterDirection.Output
			};
		}

		/// <summary>
		/// Чтение количества потоков с конфига
		/// </summary>
		/// <returns></returns>
		static int getThreadNumberFromConfig()
		{
			string value = System.Configuration.ConfigurationManager.AppSettings["threadNumber"];
			int threadNumber;
			if (Int32.TryParse(value, out threadNumber))
			{
				return threadNumber;
			}
			else
			{
				return 4;
			}
		}

		static async Task Main(string[] args)
		{
			try
			{
				var cancellationTokenSource = new CancellationTokenSource();
				var logWriter = new StreamWriter(logFilePath);
				var threadNumber = getThreadNumberFromConfig();

				// параллельный блок
				var block = new ActionBlock<string>(jsonString =>
				{
					using (var sqlConnection = new SqlConnection(connectionString))
					{
						sqlConnection.Open();
						var user = JsonConvert.DeserializeObject<User>(jsonString, new JsonSerializerSettings { DateFormatString = "dd.MM.yyyy h:mm:ss" });
						var updated = createOutputIntSqlParameter("@updated");
						var existed = createOutputIntSqlParameter("@existed");
						var command = new SqlCommand(createAddOrUpdateSql(user), sqlConnection);
						command.Parameters.Add(updated);
						command.Parameters.Add(existed);
						int count = command.ExecuteNonQuery();
						int updatedCount = (int)updated.Value;
						if ((int)updated.Value > 0)
						{
							Log(logWriter, $"update: {jsonString}");// Console.WriteLine($"update: {jsonString}");
						}
						else if ((int)existed.Value == 0)
						{
							Log(logWriter, $"insert: {jsonString}");// Console.WriteLine($"insert: {jsonString}");
						}

					}
				}, new ExecutionDataflowBlockOptions
				{
					MaxDegreeOfParallelism = threadNumber,
					CancellationToken = cancellationTokenSource.Token
				}) ;
				// конец параллельного блока

				foreach (var line in File.ReadLines(filePath))
				{
					await block.SendAsync(line);
				}
				block.Complete();
				while (!block.Completion.IsCompleted)
				{
					if (Console.ReadKey(true).Key == ConsoleKey.Escape)
					{
						cancellationTokenSource.Cancel();
						Thread.Sleep(50);
						Console.WriteLine("Escape is pressed...");
					}
				}
				logWriter.Flush();
				logWriter.Close();

			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.ToString());
			}
		}
	}
}
