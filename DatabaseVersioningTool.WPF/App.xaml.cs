using System.Configuration;
using System.Windows;

using DatabaseVersioningTool.DataAccess;

namespace DatabaseVersioningTool.WPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static string DatabaseName = "master";
        public static DatabaseConnection GetDatabaseConnection()
        {
            return new SqlDatabaseConnection(DatabaseName, new ConnectionStringBuilder((string)ConfigurationManager.AppSettings["ServerName"], DatabaseName).Build());
        }
    }
}
