using DatabaseVersioningTool.Application;
using DatabaseVersioningTool.Application.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;

namespace DatabaseVersioningTool.DataAccess.Sql
{
    public class SqlDatabaseManager : DatabaseManager<SqlDatabaseUpdate>
    {
        private static List<string> DatabaseNames { get; set; }

        private DatabaseUpdate CreateUpdate(string sql)
        {
            DatabaseUpdate dbUpdate = new DatabaseUpdate();

            StringBuilder sbScriptUpdater = new StringBuilder();
            sbScriptUpdater.AppendLine("SET XACT_ABORT ON");
            sbScriptUpdater.AppendLine("BEGIN TRANSACTION");
            sbScriptUpdater.AppendLine("\tBEGIN TRY ");
            sbScriptUpdater.AppendLine();
            sbScriptUpdater.Append(sql);
            sbScriptUpdater.AppendLine();
            sbScriptUpdater.AppendLine("\t\tCOMMIT TRANSACTION");
            sbScriptUpdater.AppendLine("\tEND TRY");
            sbScriptUpdater.AppendLine("\tBEGIN CATCH");
            sbScriptUpdater.AppendLine("\t\tROLLBACK TRANSACTION");
            sbScriptUpdater.AppendLine("\t\tDECLARE @Msg NVARCHAR(MAX)");
            sbScriptUpdater.AppendLine("\t\tSELECT @Msg=ERROR_MESSAGE()");
            sbScriptUpdater.AppendLine("\t\tRAISERROR(N'Error Occured: %s',10,100,@msg) WITH LOG");
            sbScriptUpdater.AppendLine("\tSELECT ERROR_NUMBER() AS ErrorNumber, @Msg AS ErrorMessage");
            sbScriptUpdater.AppendLine("\tEND CATCH");
            sbScriptUpdater.AppendLine("SET XACT_ABORT OFF");
            sbScriptUpdater.AppendLine("SELECT 0 AS ErrorNumber, 'Success' AS ErrorMessage");

            dbUpdate.Content = sbScriptUpdater.ToString();
            sbScriptUpdater = null;

            return dbUpdate;
        }

        public override IEnumerable<string> GetDatabaseNames(DatabaseConnection databaseConnection)
        {
            string errorMessage = string.Empty;
            if (DatabaseNames == null)
            {
                DatabaseNames = new List<string>();

                DataTable dtNames = databaseConnection.ExecuteReader("EXEC sp_databases", out errorMessage);
                if (dtNames == null)
                {
                    throw new Exception($"Error fetching database names from database - { errorMessage }");
                }

                foreach (DataRow row in dtNames.Rows)
                {
                    DatabaseNames.Add((string)row["DATABASE_NAME"]);
                }
            }
            return DatabaseNames;
        }

        public override void RestoreDatabase(DatabaseConnection databaseConnection, string dbName, string filePath)
        {
            string errorMessage = string.Empty;

            DataTable resultsTable = databaseConnection.ExecuteReader(string.Format("RESTORE FILELISTONLY FROM DISK = '{0}'", filePath), out errorMessage);

            if (resultsTable == null)
            {
                throw new Exception($"Error restoring database - { errorMessage }");
            }

            List<string> logicalNames = new List<string>();

            foreach (DataRow row in resultsTable.Rows)
            {
                logicalNames.Add((string)row[0]);
            }
            resultsTable = null;

            string dataPath = databaseConnection.ExecuteScalar<string>("SELECT SUBSTRING(physical_name, 1, CHARINDEX(N'master.mdf', LOWER(physical_name)) - 1) FROM master.sys.master_files WHERE database_id = 1 AND FILE_ID = 1");

            databaseConnection.ExcecuteScript(string.Format("ALTER DATABASE {0} SET SINGLE_USER WITH ROLLBACK IMMEDIATE", dbName));

            databaseConnection.ExcecuteScript(string.Format("USE master RESTORE DATABASE {0} FROM DISK = '{1}' WITH MOVE '{2}' TO '{4}{2}.mdf', MOVE '{3}' TO '{4}{3}.ldf'", dbName, filePath, logicalNames[0], logicalNames[1], dataPath));

            databaseConnection.ExcecuteScript(string.Format("ALTER DATABASE {0} SET MULTI_USER", dbName));
        }

        public override void BackUpDatabase(DatabaseConnection databaseConnection, string filePath)
        {
            filePath = string.Format(@"{0}\{1}_{2}.bak", filePath, databaseConnection.DatabaseName, DateTime.UtcNow.ToString().Replace("/", string.Empty).Replace(":", string.Empty).Replace(" ", string.Empty));
            databaseConnection.ExcecuteScript(string.Format("BACKUP DATABASE {0} TO DISK = '{1}' WITH FORMAT", databaseConnection.DatabaseName, filePath));
        }

        public override void CreateDatabaseUpdate(DatabaseConnection databaseConnection, SqlDatabaseUpdate update)
        {
            var result = CreateUpdate(update.Sql);

            string currentVersion = GetDatabaseVersion(databaseConnection);
            string newVersion = GenerateVersionLabel();

            string path = FileManager.Manager.GeneratePhysicalUpdate(databaseConnection.DatabaseName, newVersion, result.Content);

            DatabaseVersionCollection databaseVersion = VersionTracker.GetDatabaseVersions(databaseConnection.DatabaseName);
            databaseVersion.AddVersion(new DatabaseVersion() { Name = newVersion, To = newVersion, From = currentVersion, Path = path });
            VersionTracker.WriteFile();
        }

        public override void CreateInitialVersion(DatabaseConnection databaseConnection, SqlDatabaseUpdate update)
        {
            var result = CreateUpdate(update.Sql);

            string path = FileManager.Manager.GeneratePhysicalUpdate(databaseConnection.DatabaseName, DatabaseManager.CreateVersionNumber, result.Content);

            DatabaseVersionCollection databaseVersion = VersionTracker.GetDatabaseVersions(databaseConnection.DatabaseName);
            databaseVersion.AddVersion(new DatabaseVersion() { Name = DatabaseManager.CreateVersionNumber, From = DatabaseManager.CreateVersionNumber, To = DatabaseManager.InitialVersionNumber, Path = path });
            VersionTracker.WriteFile();
        }

        public override void ValidateUpdate(DatabaseConnection databaseConnection, SqlDatabaseUpdate update)
        {
            databaseConnection.ExcecuteScript(update.Sql);
        }

        public override string GetDatabaseVersion(DatabaseConnection databaseConnection)
        {
            string version = databaseConnection.ExecuteScalar<string>("SELECT value FROM sys.extended_properties p WHERE class = 0 AND name = 'Version'");
            if (String.IsNullOrEmpty(version))
            {
                return DatabaseManager.InitialVersionNumber;
            }
            return version;
        }

        public override void UpgradeDatabaseToVersion(DatabaseConnection databaseConnection, string targetVersion)
        {
            string currentVersion = GetDatabaseVersion(databaseConnection);

            IEnumerable<DatabaseVersion> versionsToRun = VersionTracker.GetAvailableVersions(databaseConnection.DatabaseName, currentVersion, targetVersion);

            foreach (var version in versionsToRun)
            {
                string sql = FileManager.Manager.GetSqlScript(databaseConnection.DatabaseName, version.Name);

                databaseConnection.ExcecuteScript(sql);
                IncrementVersion(databaseConnection, version.To);
            }
        }

        public override void GenerateCreateScripts(DatabaseConnection databaseConnection)
        {
            string sqlText = File.ReadAllText($"{Environment.CurrentDirectory}\\SqlScripts\\GenerateCreateScripts.sql");

            string sqlCreateScript =  databaseConnection.ExecuteScalar<string>(sqlText);

            this.CreateInitialVersion(databaseConnection, new SqlDatabaseUpdate() { Sql = sqlCreateScript });
        }

        private void IncrementVersion(DatabaseConnection databaseConnection, string versionNumber)
        {
            StringBuilder sbScriptUpdater = new StringBuilder();
            sbScriptUpdater.AppendLine("BEGIN TRY");
            sbScriptUpdater.AppendLine("\tEXEC sp_dropextendedproperty @name = N'Version';");
            sbScriptUpdater.AppendLine("END TRY");
            sbScriptUpdater.AppendLine("BEGIN CATCH");
            sbScriptUpdater.AppendLine("END CATCH");

            databaseConnection.ExcecuteScript(sbScriptUpdater.ToString());
            sbScriptUpdater = null;

            databaseConnection.ExcecuteScript(string.Format("EXEC sys.sp_addextendedproperty @name = N'Version', @value = N'{0}';", versionNumber));
        }
    }
}
