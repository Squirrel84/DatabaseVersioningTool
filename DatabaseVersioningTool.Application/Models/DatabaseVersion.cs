using DatabaseVersioningTool.DataAccess.Models.Interfaces;

namespace DatabaseVersioningTool.DataAccess.Models
{
    public class DatabaseVersion : IDatabaseVersion
    {
        public string From { get; set; }
        public string To { get; set; }
        public string Path { get; set; }
        public string Name { get; set; }
    }
}
