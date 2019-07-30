namespace DatabaseVersioningTool.DataAccess
{
    public class DatabaseVersion : IDatabaseVersion
    {
        public string From { get; set; }
        public string To { get; set; }
        public string Path { get; set; }
        public string Id { get { return null; } }
    }
}
