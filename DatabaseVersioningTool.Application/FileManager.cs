using System;
using System.IO;
using System.Text;

namespace DatabaseVersioningTool.Application
{
    public class FileManager
    {
        private const string ScriptFolderName = "VersionScripts";
        private const string VersionFileName = "Versions.config";
        private string VersionFolderPath = null;

        public string VersionFilePath { get; private set; }

        private string GetUpgradeFolderName(string dbName)
        {
            return System.IO.Path.Combine(VersionFolderPath, $"{dbName}");
        }

        private string GetUpgradePathAndFileName(string dbName, string versionName)
        {
            return System.IO.Path.Combine(GetUpgradeFolderName(dbName), $"{versionName}.sql");
        }

        public static FileManager Manager
        {
            get
            {
                if (_manager == null)
                {
                    _manager = new FileManager();
                }
                return _manager;
            }
        }
        private static FileManager _manager { get; set; }

        public void Initialise(string scriptDirectory)
        {            
            this.VersionFolderPath = System.IO.Path.Combine(scriptDirectory, ScriptFolderName);

            if (!System.IO.Directory.Exists(this.VersionFolderPath))
            {
                System.IO.Directory.CreateDirectory(this.VersionFolderPath);
            }

            EnsureVersionFileExists(this.VersionFolderPath);
        }

        private void EnsureVersionFileExists(string folderPath)
        {
            this.VersionFilePath = System.IO.Path.Combine(folderPath, VersionFileName);
            if (!System.IO.File.Exists(this.VersionFilePath))
            {
                using (var fs = System.IO.File.Create(this.VersionFilePath))
                {
                    using (TextWriter tw = new StreamWriter(fs, Encoding.UTF8))
                    {
                        tw.WriteLine("<Versions></Versions>");
                    }
                }
            }
        }

        public string GeneratePhysicalUpdate(string dbName, string versionName, string sql)
        {
            string upgradePath = GetUpgradeFolderName(dbName);
            System.IO.Directory.CreateDirectory(upgradePath);

            string upgradeFullPath = GetUpgradePathAndFileName(dbName, versionName);

            if (!System.IO.File.Exists(upgradeFullPath))
            {
                using (System.IO.FileStream fs = System.IO.File.Create(upgradeFullPath))
                {
                    using (System.IO.StreamWriter file = new StreamWriter(fs))
                    {
                        file.Write(sql);
                    }
                }
            }

            return upgradeFullPath;
        }

        public string GetSqlScript(string dbName, string versionName)
        {
            string upgradeFullPath = GetUpgradePathAndFileName(dbName, versionName);

            string sql = string.Empty;

            if (System.IO.File.Exists(upgradeFullPath))
            {
                using (System.IO.FileStream fs = System.IO.File.OpenRead(upgradeFullPath))
                {
                    using (System.IO.StreamReader file = new StreamReader(fs))
                    {
                        sql = file.ReadToEnd();
                    }
                }
            }
            return sql;
        }
    }
}
