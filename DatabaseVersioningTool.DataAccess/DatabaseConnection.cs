using System.Data;

namespace DatabaseVersioningTool.DataAccess
{
    public abstract class DatabaseConnection
    {
        public string DatabaseName { get; }
        public string ConnectionString { get; }

        public DatabaseConnection(string databaseName, string connectionString)
        {
            this.DatabaseName = databaseName;
            this.ConnectionString = connectionString;
        }

        public abstract void ExcecuteScript(string sql);

        public abstract DataTable ExecuteReader(string sql, out string errorMessage, CommandBehavior behaviour = CommandBehavior.Default);

        public abstract T ExecuteScalar<T>(string sql);
    }
}
