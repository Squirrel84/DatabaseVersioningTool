using System;
using System.IO;
using System.Text;

namespace DatabaseVersioningTool.DataAccess
{
    public class FileManager
    {
        public string VersionFilePath { get; private set; }

        public string GetUpgradeFileName(string dbName, string version)
        {
            return String.Format("{0}_{1}", dbName, version);
        }

        private string GetUpgradeFolderName(string upgradeName)
        {
            string current = Directory.GetCurrentDirectory();
            return System.IO.Path.Combine(current, String.Format("{0}_Update", upgradeName));
        }

        private string GetUpgradePathAndFileName(string upgradePath, string upgradeName)
        {
            return System.IO.Path.Combine(upgradePath, string.Format("{0}.damm", upgradeName));
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

        public void Initialise()
        {
            string current = Directory.GetCurrentDirectory();
            string folderName = "DATA";
            string versionConfigPath = System.IO.Path.Combine(current, folderName);
            if (!System.IO.Directory.Exists(versionConfigPath))
            {
                System.IO.Directory.CreateDirectory(versionConfigPath);
            }

            versionConfigPath = System.IO.Path.Combine(versionConfigPath, "Versions.config");
            if (!System.IO.File.Exists(versionConfigPath))
            {
                using (var fs = System.IO.File.Create(versionConfigPath))
                {
                    using (TextWriter tw = new StreamWriter(fs, Encoding.UTF8))
                    {
                        tw.WriteLine("<Versions></Versions>");
                    }
                }
            }

            VersionFilePath = versionConfigPath;
        }

        internal string GeneratePhysicalUpdate(string dbName, string version, string sql)
        {
            string upgradeName = GetUpgradeFileName(dbName, version);
            string upgradePath = GetUpgradeFolderName(upgradeName);

            System.IO.Directory.CreateDirectory(upgradePath);

            string upgradeFullPath = GetUpgradePathAndFileName(upgradePath, upgradeName);

            if (!System.IO.File.Exists(upgradeFullPath))
            {
                using (System.IO.FileStream fs = System.IO.File.Create(upgradeFullPath))
                {
                    System.IO.StreamWriter file = new StreamWriter(fs);
                    file.Write(sql);
                    file.Close();
                }

            }

            return upgradeFullPath;
        }

        internal string GetSqlScript(string dbName, string version)
        {
            string upgradeName = GetUpgradeFileName(dbName, version);
            string upgradePath = GetUpgradeFolderName(upgradeName);

            string upgradeFullPath = GetUpgradePathAndFileName(upgradePath, upgradeName);

            string sql = string.Empty;

            if (System.IO.File.Exists(upgradeFullPath))
            {
                using (System.IO.FileStream fs = System.IO.File.OpenRead(upgradeFullPath))
                {
                    System.IO.StreamReader file = new StreamReader(fs);
                    sql = file.ReadToEnd();
                }
            }
            return sql;
        }
    }
}
