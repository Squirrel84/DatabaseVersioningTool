using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseVersioningTool.DataAccess.Sql
{
    public class SqlConnectionStringBuilder : ConnectionStringBuilder
    {
        private const int SQLDefaultPortNumber = 1433;

        public SqlConnectionStringBuilder(string servername, string databasename) 
            : base(servername, databasename, SQLDefaultPortNumber, null, null)
        {
        }

        public SqlConnectionStringBuilder(string servername, string databasename, int port)
            : base(servername, databasename, port, null, null)
        {
        }

        public SqlConnectionStringBuilder(string servername, string databasename, string username, string password)
            : base(servername, databasename, SQLDefaultPortNumber, username, password)
        {
        }

        public SqlConnectionStringBuilder(string servername, string databasename, int port, string username, string password) 
            : base(servername, databasename, port, username, password)
        {
        }

        public override string Build()
        {
            return String.Format(
                @"Data Source={0};Initial Catalog={1};{2}",
                this.ServerName,
                this.DatabaseName,
                String.IsNullOrEmpty(this.Username) ? "Integrated Security=SSPI;Trusted_Connection=Yes;" : String.Format("User Id={0};Password={1};", this.Username, this.Password));
        }
    }
}
