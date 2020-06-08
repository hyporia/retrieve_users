using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace console_app.models
{
	public class User
	{
		[DatabaseGenerated(DatabaseGeneratedOption.None)]
		public int Id { get; set; }
		public string Name { get; set; }
		public string City { get; set; }
		public string Email { get; set; }
		public string Phone { get; set; }
		public DateTime UpdDate { get; set; }
	}
}
