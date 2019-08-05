using DatabaseVersioningTool.DataAccess.Interfaces;
using DatabaseVersioningTool.DataAccess.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace DatabaseVersioningTool.DataAccess
{
    public class SqlDatabaseManager : IDatabaseManager
    {
        public VersionTracker<DatabaseVersionCollection, DatabaseVersion> VersionTracker { get; set; }
        private static List<string> DatabaseNames { get; set; }

        public string GenerateVersionLabel()
        {
            DateTime date = DateTime.UtcNow;
            return string.Format("{0}{1}{2}_{3}{4}{5}_{6}", date.Year.ToString("0000"), date.Month.ToString("00"), date.Day.ToString("00"), date.Hour.ToString("00"), date.Minute.ToString("00"), date.Second.ToString("00"), date.Millisecond.ToString("000"));
        }

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


        public SqlDatabaseManager()
        {
            this.VersionTracker = new VersionTracker<DatabaseVersionCollection, DatabaseVersion>();
        }

        public IEnumerable<string> GetDatabaseNames(DatabaseConnection dbAccess)
        {
            string errorMessage = string.Empty;
            if (DatabaseNames == null)
            {
                DatabaseNames = new List<string>();

                DataTable dtNames = dbAccess.ExecuteReader("EXEC sp_databases", out errorMessage);
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

        public void RestoreDatabase(DatabaseConnection dbAccess, string dbName, string filePath)
        {
            string errorMessage = string.Empty;

            DataTable resultsTable = dbAccess.ExecuteReader(string.Format("RESTORE FILELISTONLY FROM DISK = '{0}'", filePath), out errorMessage);

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

            string dataPath = dbAccess.ExecuteScalar<string>("SELECT SUBSTRING(physical_name, 1, CHARINDEX(N'master.mdf', LOWER(physical_name)) - 1) FROM master.sys.master_files WHERE database_id = 1 AND FILE_ID = 1");

            dbAccess.ExcecuteScript(string.Format("ALTER DATABASE {0} SET SINGLE_USER WITH ROLLBACK IMMEDIATE", dbName));

            dbAccess.ExcecuteScript(string.Format("USE master RESTORE DATABASE {0} FROM DISK = '{1}' WITH MOVE '{2}' TO '{4}{2}.mdf', MOVE '{3}' TO '{4}{3}.ldf'", dbName, filePath, logicalNames[0], logicalNames[1], dataPath));

            dbAccess.ExcecuteScript(string.Format("ALTER DATABASE {0} SET MULTI_USER", dbName));
        }

        public void BackUpDatabase(DatabaseConnection dbAccess, string path)
        {
            string filePath = string.Format(@"{0}\{1}_{2}.bak", path, dbAccess.DatabaseName, DateTime.UtcNow.ToString().Replace("/", string.Empty).Replace(":", string.Empty).Replace(" ", string.Empty));
            dbAccess.ExcecuteScript(string.Format("BACKUP DATABASE {0} TO DISK = '{1}' WITH FORMAT", dbAccess.DatabaseName, filePath));
        }

        public void CreateDatabaseUpdate(DatabaseConnection dbAccess, string sql)
        {
            var result = CreateUpdate(sql);

            string currentVersion = GetDatabaseVersion(dbAccess);
            string newVersion = GenerateVersionLabel();

            string path = FileManager.Manager.GeneratePhysicalUpdate(dbAccess.DatabaseName, newVersion, result.Content);

            DatabaseVersionCollection databaseVersion = VersionTracker.GetDatabaseVersions(dbAccess.DatabaseName);
            databaseVersion.AddVersion(new DatabaseVersion() { Name = newVersion, To = newVersion, From = currentVersion, Path = path });
            VersionTracker.WriteFile();
        }

        public void ValidateSQL(DatabaseConnection dbAccess, string sql)
        {
            dbAccess.ExcecuteScript(sql);
        }

        private void IncrementVersion(DatabaseConnection dbAccess, string versionNumber)
        {
            StringBuilder sbScriptUpdater = new StringBuilder();
            sbScriptUpdater.AppendLine("BEGIN TRY");
            sbScriptUpdater.AppendLine("\tEXEC sp_dropextendedproperty @name = N'Version';");
            sbScriptUpdater.AppendLine("END TRY");
            sbScriptUpdater.AppendLine("BEGIN CATCH");
            sbScriptUpdater.AppendLine("END CATCH");

            dbAccess.ExcecuteScript(sbScriptUpdater.ToString());
            sbScriptUpdater = null;

            dbAccess.ExcecuteScript(string.Format("EXEC sys.sp_addextendedproperty @name = N'Version', @value = N'{0}';", versionNumber));
        }

        public string GetDatabaseVersion(DatabaseConnection dbAccess)
        {
            string version = dbAccess.ExecuteScalar<string>("SELECT value FROM sys.extended_properties p WHERE class = 0 AND name = 'Version'");
            if (String.IsNullOrEmpty(version))
            {
                return "1.0";
            }
            return version;
        }

        public void UpgradeDatabaseToVersion(DatabaseConnection dbAccess, string targetVersion)
        {
            string currentVersion = GetDatabaseVersion(dbAccess);

            DatabaseVersionCollection databaseVersions = VersionTracker.GetDatabaseVersions(dbAccess.DatabaseName);

            IEnumerable<DatabaseVersion> versionsToRun = GetVersionsToRun(currentVersion, targetVersion, databaseVersions);

            foreach (var version in versionsToRun)
            {
                string sql = FileManager.Manager.GetSqlScript(dbAccess.DatabaseName, version.Name);

                dbAccess.ExcecuteScript(sql);
                IncrementVersion(dbAccess, version.To);
            }
        }

        private IEnumerable<DatabaseVersion> GetVersionsToRun(string currentVersion, string targetVersion, DatabaseVersionCollection databaseVersions)
        {
            List<DatabaseVersion> versionScriptsToRun = new List<DatabaseVersion>();
            string versionToTrack = currentVersion;
            bool versionScriptToRun = true;

            while (versionScriptToRun)
            {
                var databaseVersion = databaseVersions.Versions.FirstOrDefault(x => x.From == versionToTrack);

                versionScriptToRun = databaseVersion != null;

                if (!versionScriptToRun)
                {
                    break;
                }

                versionScriptsToRun.Add(databaseVersion);
                versionToTrack = databaseVersion.To;

                if (targetVersion == versionToTrack)
                {
                    break;
                }
            }

            return versionScriptsToRun;
        }

        public void GenerateCreateScripts(DatabaseConnection dbAccess)
        {
            throw new NotImplementedException();
        }
    }
}
