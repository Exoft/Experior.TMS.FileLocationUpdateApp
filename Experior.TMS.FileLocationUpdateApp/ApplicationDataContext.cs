using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using Experior.TMS.FileLocationUpdateApp.Entities;
using IBM.Data.DB2;
using IBM.Data.DB2.EntityFramework;

namespace Experior.TMS.FileLocationUpdateApp
{
    public class ApplicationDataContext : DbContext
    {
        public ApplicationDataContext(DbConnection connection) : base(connection, true)
        {}

        public DbSet<DocumentFile> DocumentFiles { get; set; }
        
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            Database.SetInitializer<ApplicationDataContext>(null);
            base.OnModelCreating(modelBuilder);
        }

        public static ApplicationDataContext Create(string connectionString)
        {
            var connectionFactory = new DB2ConnectionFactory();
            var connection = connectionFactory.CreateConnection(connectionString);
            
            return new ApplicationDataContext(connection);
        }
    }
}