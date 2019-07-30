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

        public string GetAccountSqlIsRunningUnder(out string errorMessage)
        {
            DataTable result = ExecuteReader(@"EXECUTE master.dbo.xp_instance_regread N'HKEY_LOCAL_MACHINE', N'SYSTEM\CurrentControlSet\Services\MSSQLSERVER', N'ObjectName'", out errorMessage, CommandBehavior.SingleRow);
            if (result != null)
            {
                return (string)result.Rows[0][1];
            }
            return "ERROR";
        }
    }
}
