using System.Configuration;
using System.Windows;

using DatabaseVersioningTool.DataAccess;
using DatabaseVersioningTool.DataAccess.Interfaces;
using DatabaseVersioningTool.DataAccess.Sql;

namespace DatabaseVersioningTool.WPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static string DatabaseName = "master";

        private static object padlock = new object();

        private static IDatabaseManager databaseManager = null;

        public static DatabaseConnection GetDatabaseConnection()
        {
            return new SqlDatabaseConnection(DatabaseName, new SqlConnectionStringBuilder((string)ConfigurationManager.AppSettings["ServerName"], DatabaseName).Build());
        }

        public static IDatabaseManager DatabaseManager
        {
            get
            {
                if (databaseManager == null)
                {
                    lock (padlock)
                    {
                        if (databaseManager == null)
                        {
                            databaseManager = new SqlDatabaseManager();
                        }
                    }
                }

                return databaseManager;
            }
        }
    }
}
