using System.Collections.Generic;
using DatabaseVersioningTool.Application.Models.Interfaces;
using DatabaseVersioningTool.DataAccess.Models;

namespace DatabaseVersioningTool.DataAccess.Interfaces
{
    public interface IDatabaseManager
    {
        VersionTracker<DatabaseVersionCollection, DatabaseVersion> VersionTracker { get; }
        string GenerateVersionLabel();
        string GetDatabaseVersion(DatabaseConnection databaseConnection);
        IEnumerable<string> GetDatabaseNames(DatabaseConnection databaseConnection);
        void RestoreDatabase(DatabaseConnection databaseConnection, string dbName, string filePath);
        void BackUpDatabase(DatabaseConnection databaseConnection, string filePath);
        void UpgradeDatabaseToVersion(DatabaseConnection databaseConnection, string targetVersion);
        void GenerateCreateScripts(DatabaseConnection databaseConnection);
    }
    public interface IDatabaseManager<T> : IDatabaseManager where T : IDatabaseUpdate
    {
        
        void CreateInitialVersion(DatabaseConnection databaseConnection, T databaseUpdate);
        void CreateDatabaseUpdate(DatabaseConnection databaseConnection, T databaseUpdate);
        void ValidateUpdate(DatabaseConnection databaseConnection, T databaseUpdate);
    }

}
