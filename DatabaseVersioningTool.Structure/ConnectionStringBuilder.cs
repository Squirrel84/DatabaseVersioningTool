using System;

namespace DatabaseVersioningTool.DataAccess
{
    public abstract class ConnectionStringBuilder
    {
        protected string ServerName { get; }
        protected int PortNumber { get; }
        protected string Username { get; }
        protected string Password { get; }
        public string DatabaseName { get; }

        public ConnectionStringBuilder(string servername, string databasename, int port, string username, string password)
        {
            this.ServerName = servername;
            this.DatabaseName = databasename;
            this.PortNumber = port;
            this.Username = username;
            this.Password = password;
        }

        public abstract string Build();
    }
}
