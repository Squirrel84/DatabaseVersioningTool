using System;

namespace DatabaseVersioningTool.DataAccess
{
    public class ConnectionStringBuilder
    {
        private string _serverName = null;
        private string _username = null;
        private string _password = null;

        public string DatabaseName { get; }

        public ConnectionStringBuilder(string servername, string databasename)
        {
            _serverName = servername;
            this.DatabaseName = databasename;
        }

        public ConnectionStringBuilder(string servername, string databasename, string username, string password) : this(servername, databasename)
        {
            _username = username;
            _password = password;
        }

        public string Build()
        {
            return String.Format(
                @"Data Source={0};Initial Catalog={1};{2}",
                _serverName,
                this.DatabaseName,
                String.IsNullOrEmpty(_username) ? "Integrated Security=SSPI;Trusted_Connection=Yes;" : String.Format("User Id={0};Password={1};", _username, _password));
        }

       
    }
}
