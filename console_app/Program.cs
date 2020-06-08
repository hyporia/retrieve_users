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

namespace console_app
{
	class Program
	{
		static string filePath = "../../users3.txt";
		static string connectionString = @"Server=localhost\SQLEXPRESS;Database=userupd;Trusted_Connection=True;";
		static void fillFile()
		{
			string[] names = new string[] { "Liam", "Noah", "William", "James", "Oliver", "Benjamin", "Elijah", "Lucas", "Mason", "Logan", "Alexander", "Ethan", "Jacob", "Michael", "Daniel" };
			string[] cities = new string[] { "Mexico", "New York", "Los Angeles", "Toronto", "Chicago", "Houston", "Havana", "Montreal", "Philadelphia", "Phoenix", "San Antonio", "Guadalajara", "Puebla", "San Diego" };
			var today = DateTime.Now.AddDays(-1000);
			var dates = Enumerable.Range(0, 1000)
					  .Select(i => today.AddDays(i))
					  .ToList();
			var random = new Random(123);
			using (var streamWriter = new StreamWriter(filePath))
			{
				for (int i = 0; i < 1000000; i++)
				{
					var name = names[random.Next(0, names.Length)];
					var city = cities[random.Next(0, cities.Length)];
					var id = random.Next(1, 10000);
					var date = dates[random.Next(0, dates.Count)];
					var email = $"{name}{random.Next(0, 10000)}";
					var phone = random.Next(1000, 1000000);
					streamWriter.WriteLine($"{{ \'id\': \'{id}\', \'name\': \'{name}\', \'city\': \'{city}\', \'email\': \'{email}\', \'phone\': \'{phone}\', \'updDate\': \'{date}\'}}");
				}
			}
		}

		/// <summary>
		/// Entity framework
		/// </summary>
		/// <param name="state"></param>
		static void addOrUpdateUser(object state)
		{
			var jsonString = state as string;
			using (var context = new UserContext())
			{
				var user = JsonConvert.DeserializeObject<User>(jsonString, new JsonSerializerSettings { DateFormatString = "dd.MM.yyyy h:mm:ss" });
				context.Users.AddOrUpdate(user);
				context.SaveChanges();
			}
		}

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
		//static string createInsertSql(User user)
		//{
		//	return $"begin tran" +
		//			$"		insert into users(Id, Name, Email, City, Phone, UpdDate) " +
		//			$"		values({user.Id}, '{user.Name}', '{user.Email}', '{user.City}', '{user.Phone}', '{user.UpdDate}')" +
		//			$"commit tran";
		//}

		//static string createUpdateSql(User user)
		//{
		//	return $"begin tran" +
		//			$"	update users with(serializable) set Name = '{user.Name}', Email = '{user.Email}', City = '{user.City}', Phone = '{user.Phone}', UpdDate = '{user.UpdDate}'" +
		//			$"	where Id = {user.Id} and {user.UpdDate} > UpdDate" +
		//			$"  set @kek = @@rowcount " +
		//			$"commit tran";
		//}


		//static string createSelectSql(int Id)
		//{
		//	return $"begin tran " +
		//		$"select count(*) from users where Id = {Id} " +
		//		$"commit tran";
		//}

		//static void addOrUpdateUserJob(object state)
		//{
		//	var jsonString = state as string;
		//	using (var sqlConnection = new SqlConnection(connectionString))
		//	{
		//		sqlConnection.Open();
		//		var user = JsonConvert.DeserializeObject<User>(jsonString, new JsonSerializerSettings { DateFormatString = "dd.MM.yyyy h:mm:ss" });
		//		var command = new SqlCommand(createAddOrUpdateSql(user), sqlConnection);
		//		int count = command.ExecuteNonQuery();
		//	}
		//}

		//static async void addOrUpdateUserJobAsync(object state)
		//{
		//	var jsonString = state as string;
		//	using (var sqlConnection = new SqlConnection(connectionString))
		//	{
		//		sqlConnection.Open();
		//		var user = JsonConvert.DeserializeObject<User>(jsonString, new JsonSerializerSettings { DateFormatString = "dd.MM.yyyy h:mm:ss" });
		//		var command = new SqlCommand(createAddOrUpdateSql(user), sqlConnection);
		//		int count = command.ExecuteNonQuery();
		//		Console.WriteLine(jsonString);
		//	}
		//}

		static SqlParameter createOutputIntSqlParameter(string parameterName)
		{
			return new SqlParameter(parameterName, SqlDbType.Int)
			{
				Direction = ParameterDirection.Output
			};
		}

		static async Task Main(string[] args)
		{
			try
			{
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
						if ((int)updated.Value == 0 && (int)existed.Value == 0)
						{
							Console.WriteLine($"inserted: {jsonString}");
						}
						else
						{
							Console.WriteLine($"updated: {jsonString}");
						}

					}
				}, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 4 });

				foreach (var line in File.ReadLines(filePath))
				{
					await block.SendAsync(line);
				}
				block.Complete();
				await block.Completion;
				Console.WriteLine("complete");
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.ToString());
			}
		}
	}
}
