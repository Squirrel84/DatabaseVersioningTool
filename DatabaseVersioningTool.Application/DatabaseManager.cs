using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using DatabaseVersioningTool.DataAccess.Models;

namespace DatabaseVersioningTool.DataAccess
{
    public interface IDatabaseManager
    {
        VersionTracker<DatabaseVersionCollection, DatabaseVersion> VersionTracker { get; }

        string GenerateVersionLabel();
        string GetDatabaseVersion(DatabaseConnection databaseConnection);
        IEnumerable<string> GetDatabaseNames(DatabaseConnection databaseConnection);
        void RestoreDatabase(DatabaseConnection databaseConnection, string dbName, string filePath);
        void BackUpDatabase(DatabaseConnection databaseConnection, string path);
        void CreateDatabaseUpdate(DatabaseConnection databaseConnection, string sql);

        void ValidateSQL(DatabaseConnection databaseConnection, string sql);

        void UpgradeDatabaseToVersion(DatabaseConnection databaseConnection, string targetVersion);
        void GenerateCreateScripts(DatabaseConnection databaseConnection);
    }

}
