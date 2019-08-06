using DatabaseVersioningTool.Application.Models.Interfaces;
using System.Collections.Generic;

namespace DatabaseVersioningTool.Application.Models
{
    public class DatabaseVersionCollection : IDatabaseVersionCollection<DatabaseVersion>
    {
        public IList<DatabaseVersion> Versions { get { return _versions; } }
        private List<DatabaseVersion> _versions;

        public DatabaseVersionCollection()
        {
            _versions = new List<DatabaseVersion>();
        }

        public void AddVersion(DatabaseVersion version)
        {
            _versions.Add(version);
        }

        public string Name { get; set; }
    }
}
