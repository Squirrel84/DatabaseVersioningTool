using System;
using System.Data;
using System.Data.SqlClient;

namespace DatabaseVersioningTool.DataAccess
{
    public class SqlDatabaseConnection : DatabaseConnection
    {
        public SqlDatabaseConnection(string databaseName, string connectionString) : base(databaseName, connectionString)
        {
        }

        public SqlConnection CreateOpenConnection()
        {
            var conn = new SqlConnection(this.ConnectionString);

            conn.Open();

            return conn;
        }

        public override void ExcecuteScript(string sql)
        {
            try
            {
                using (IDbConnection conn = this.CreateOpenConnection())
                {
                    IDbCommand command = conn.CreateCommand();
                    command.CommandText = sql;
                    command.CommandType = CommandType.Text;
                    command.CommandTimeout = 30;
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception e)
            {
                throw new Exception("Error executing script", e);
            }
        }

        public override DataTable ExecuteReader(string sql, out string errorMessage, CommandBehavior behaviour = CommandBehavior.Default)
        {
            errorMessage = string.Empty;
            DataTable table = new DataTable();
            try
            {
                using (IDbConnection conn = this.CreateOpenConnection())
                {
                    IDbCommand command = conn.CreateCommand();
                    command.CommandText = sql;
                    command.CommandType = CommandType.Text;
                    command.CommandTimeout = 30;
                    IDataReader reader = command.ExecuteReader(behaviour);
                    table.Load(reader);
                    return table;
                }
            }
            catch (Exception e)
            {
                errorMessage = e.Message;
                return null;
            }
        }

        public override T ExecuteScalar<T>(string sql)
        {
            try
            {
                using (IDbConnection conn = this.CreateOpenConnection())
                {
                    IDbCommand command = conn.CreateCommand();
                    command.CommandText = sql;
                    command.CommandType = CommandType.Text;
                    command.CommandTimeout = 30;
                    return (T)command.ExecuteScalar();
                }
            }
            catch (Exception)
            {
                return default(T);
            }
        }
    }
}
