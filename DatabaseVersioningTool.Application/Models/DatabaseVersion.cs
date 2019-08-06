using DatabaseVersioningTool.Application.Models.Interfaces;

namespace DatabaseVersioningTool.Application.Models
{
    public class DatabaseVersion : IDatabaseVersion
    {
        public string From { get; set; }
        public string To { get; set; }
        public string Path { get; set; }
        public string Name { get; set; }
    }
}
