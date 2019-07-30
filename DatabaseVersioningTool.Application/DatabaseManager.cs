using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace DatabaseVersioningTool.DataAccess
{
    public class DatabaseManager
    {
        public VersionTracker<DatabaseVersionCollection, DatabaseVersion> VersionTracker { get; set; }
        private static List<string> DatabaseNames { get; set; }

        public DatabaseManager()
        {
            this.VersionTracker = new VersionTracker<DatabaseVersionCollection, DatabaseVersion>();
        }

        public IEnumerable<string> GetDatabaseNames(DatabaseConnection dbAccess, out string errorMessage)
        {
            errorMessage = string.Empty;
            if (DatabaseNames == null)
            {
                DatabaseNames = new List<string>();

                DataTable dtNames = dbAccess.ExecuteReader("EXEC sp_databases", out errorMessage);
                if (dtNames != null)
                {
                    foreach (DataRow row in dtNames.Rows)
                    {
                        DatabaseNames.Add((string)row["DATABASE_NAME"]);
                    }
                }
            }
            return DatabaseNames;
        }

        public string GetAccountSqlIsRunningUnder(DatabaseConnection dbAccess, out string errorMessage)
        {
            errorMessage = string.Empty;
            return dbAccess.GetAccountSqlIsRunningUnder(out errorMessage).Replace('\\', ' ').Replace(" ", string.Empty);
        }

        public bool RestoreDatabase(DatabaseConnection dbAccess, string dbName, string filePath, out string errorMessage)
        {
            errorMessage = string.Empty;

            DataTable resultsTable = dbAccess.ExecuteReader(string.Format("RESTORE FILELISTONLY FROM DISK = '{0}'", filePath), out errorMessage);

            if (resultsTable != null)
            {
                List<string> logicalNames = new List<string>();

                foreach (DataRow row in resultsTable.Rows)
                {
                    logicalNames.Add((string)row[0]);
                }
                resultsTable = null;

                string dataPath = dbAccess.ExecuteScalar<string>("SELECT SUBSTRING(physical_name, 1, CHARINDEX(N'master.mdf', LOWER(physical_name)) - 1) FROM master.sys.master_files WHERE database_id = 1 AND FILE_ID = 1");

                dbAccess.ExcecuteScript(string.Format("ALTER DATABASE {0} SET SINGLE_USER WITH ROLLBACK IMMEDIATE", dbName));

                dbAccess.ExcecuteScript(string.Format("USE master RESTORE DATABASE {0} FROM DISK = '{1}' WITH MOVE '{2}' TO '{4}{2}.mdf', MOVE '{3}' TO '{4}{3}.ldf'", dbName, filePath, logicalNames[0], logicalNames[1], dataPath));

                dbAccess.ExcecuteScript(string.Format("ALTER DATABASE {0} SET MULTI_USER", dbName));

                return true;
            }
            return false;
        }

        public bool BackUpDatabase(DatabaseConnection dbAccess, string path)
        {
            string filePath = string.Format(@"{0}\{1}_{2}.bak", path, dbAccess.DatabaseName, DateTime.UtcNow.ToString().Replace("/", string.Empty).Replace(":", string.Empty).Replace(" ", string.Empty));
            dbAccess.ExcecuteScript(string.Format("BACKUP DATABASE {0} TO DISK = '{1}' WITH FORMAT", dbAccess.DatabaseName, filePath));

            return true;
        }

        public void CreateDatabaseUpdate(DatabaseConnection dbAccess, string sql)
        {
            var result = CreateUpdate(sql);

            string currentVersion = GetDatabaseVersion(dbAccess);
            string newVersion = GetNextVersionNumber();

            string path = FileManager.Manager.GeneratePhysicalUpdate(dbAccess.DatabaseName, newVersion, result.Content);
                
            DatabaseVersionCollection databaseVersion = VersionTracker.GetDatabaseVersion(dbAccess.DatabaseName);
            databaseVersion.AddVersion(new DatabaseVersion() { To = newVersion, From = currentVersion, Path = path });
            VersionTracker.WriteFile();
        }

        private DatabaseUpdate CreateUpdate(string sql)
        {
            DatabaseUpdate dbUpdate = new DatabaseUpdate();

            StringBuilder sbScriptUpdater = new StringBuilder();
            sbScriptUpdater.Append(" SET XACT_ABORT ON");
            sbScriptUpdater.Append(" BEGIN TRANSACTION");
            sbScriptUpdater.Append(" BEGIN TRY ");
            sbScriptUpdater.Append(sql);
            sbScriptUpdater.Append(" COMMIT TRANSACTION");
            sbScriptUpdater.Append(" END TRY");
            sbScriptUpdater.Append(" BEGIN CATCH");
            sbScriptUpdater.Append(" ROLLBACK TRANSACTION");
            sbScriptUpdater.Append(" DECLARE @Msg NVARCHAR(MAX)");
            sbScriptUpdater.Append(" SELECT @Msg=ERROR_MESSAGE()");
            sbScriptUpdater.Append(" RAISERROR(N'Error Occured: %s',10,100,@msg) WITH LOG");
            sbScriptUpdater.Append(" SELECT ERROR_NUMBER() AS ErrorNumber, @Msg AS ErrorMessage");
            sbScriptUpdater.Append(" END CATCH");
            sbScriptUpdater.Append(" SET XACT_ABORT OFF");
            sbScriptUpdater.Append(" SELECT 0 AS ErrorNumber, 'Success' AS ErrorMessage");

            dbUpdate.Content = sbScriptUpdater.ToString();
            sbScriptUpdater = null;

            return dbUpdate;
        }

        public void RunUpdate(DatabaseConnection dbAccess, string sql)
        {
            dbAccess.ExcecuteScript(sql);
        }

        private void IncrementVersion(DatabaseConnection dbAccess, string versionNumber)
        {
            StringBuilder sbScriptUpdater = new StringBuilder();
            sbScriptUpdater.Append("BEGIN TRY");
            sbScriptUpdater.Append(" EXEC sp_dropextendedproperty @name = N'Version';");
            sbScriptUpdater.Append(" END TRY");
            sbScriptUpdater.Append(" BEGIN CATCH");
            sbScriptUpdater.Append(" END CATCH");

            //USE [master] GO EXEC [GDDB].sys.sp_addextendedproperty @name = N'Version', @value = N'1.0' GO

            dbAccess.ExcecuteScript(sbScriptUpdater.ToString());
            sbScriptUpdater = null;

            dbAccess.ExcecuteScript(string.Format(" EXEC sys.sp_addextendedproperty @name = N'Version', @value = N'{0}';", versionNumber));
        }

        public string GetDatabaseVersion(DatabaseConnection dbAccess)
        {
            string version = dbAccess.ExecuteScalar<string>(" select value from sys.extended_properties p WHERE class = 0 AND name = 'Version'");
            if (String.IsNullOrEmpty(version))
            {
                return "1.0";
            }
            return version;
        }

        private string GetNextVersionNumber()
        {
            DateTime date = DateTime.UtcNow;
            return string.Format("{0}{1}{2}_{3}{4}{5}_{6}", date.Year, date.Month, date.Day, date.Hour, date.Minute, date.Second, date.Millisecond);
        }

        public void Upgrade(DatabaseConnection dbAccess, string versionName)
        {
            DatabaseVersionCollection databaseVersions = VersionTracker.GetDatabaseVersion(dbAccess.DatabaseName);
            DatabaseVersion version = databaseVersions.Versions.Single(x => x.To == versionName);

            string sql = FileManager.Manager.GetSqlScript(dbAccess.DatabaseName, versionName);
            RunUpdate(dbAccess, sql);

            IncrementVersion(dbAccess, version.To);
        }
    }
}
