using DatabaseVersioningTool.Application.Models.Interfaces;
using DatabaseVersioningTool.Application.Interfaces;
using DatabaseVersioningTool.Application.Models;
using System;
using System.Collections.Generic;
using System.Text;
using DatabaseVersioningTool.DataAccess;

namespace DatabaseVersioningTool.Application
{
    public abstract class DatabaseManager
    {
        public const string CreateVersionNumber = "0.0.0.0";
        public const string InitialVersionNumber = "1.0.0.0";
    }

    public abstract class DatabaseManager<T> : IDatabaseManager<T> where T : IDatabaseUpdate
    {
        public VersionTracker<DatabaseVersionCollection, DatabaseVersion> VersionTracker { get; private set; }

        public DatabaseManager()
        {
            this.VersionTracker = new VersionTracker<DatabaseVersionCollection, DatabaseVersion>();
        }

        public virtual string GenerateVersionLabel()
        {
            DateTime date = DateTime.UtcNow;
            return string.Format("{0}{1}{2}_{3}{4}{5}_{6}", date.Year.ToString("0000"), date.Month.ToString("00"), date.Day.ToString("00"), date.Hour.ToString("00"), date.Minute.ToString("00"), date.Second.ToString("00"), date.Millisecond.ToString("000"));
        }

        public abstract void BackUpDatabase(DatabaseConnection databaseConnection, string path);

        public abstract void CreateInitialVersion(DatabaseConnection databaseConnection, T databaseUpdate);

        public abstract void CreateDatabaseUpdate(DatabaseConnection databaseConnection, T databaseUpdate);

        public abstract void GenerateCreateScripts(DatabaseConnection databaseConnection);

        public abstract IEnumerable<string> GetDatabaseNames(DatabaseConnection databaseConnection);

        public abstract string GetDatabaseVersion(DatabaseConnection databaseConnection);

        public abstract void RestoreDatabase(DatabaseConnection databaseConnection, string dbName, string filePath);

        public abstract void UpgradeDatabaseToVersion(DatabaseConnection databaseConnection, string targetVersion);

        public abstract void ValidateUpdate(DatabaseConnection databaseConnection, T databaseUpdate);
    }
}
