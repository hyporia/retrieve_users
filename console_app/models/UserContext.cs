using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace console_app.models
{
	public class UserContext : DbContext
	{
		public UserContext() : base("userupd") { }

		public DbSet<User> Users { get; set; }
	}
}
