using System.Configuration;

using DatabaseVersioningTool.DataAccess;
using DatabaseVersioningTool.Application.Interfaces;
using DatabaseVersioningTool.DataAccess.Sql;
using NLog;

namespace DatabaseVersioningTool.WPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        public static string DatabaseName = "master";

        private static object padlock = new object();

        private static IDatabaseManager<SqlDatabaseUpdate> databaseManager = null;

        private static ILogger logger = null;

        public App()
        {
            logger = LogManager.GetLogger("DatabaseMaintainer");
        }

        public static DatabaseConnection DatabaseConnection
        {
            get
            {
                return new SqlDatabaseConnection(DatabaseName, new SqlConnectionStringBuilder((string)ConfigurationManager.AppSettings["ServerName"], DatabaseName).Build());
            }
        }

        public static IDatabaseManager<SqlDatabaseUpdate> DatabaseManager
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
                            databaseManager.Logger = logger;
                        }
                    }
                }

                return databaseManager;
            }
        }
    }
}
