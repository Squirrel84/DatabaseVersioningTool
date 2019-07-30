namespace DatabaseVersioningTool.DataAccess
{
    public interface IDatabaseVersion
    {
        string Id { get; }
        string From { get; }
        string To { get; }
    }
}
