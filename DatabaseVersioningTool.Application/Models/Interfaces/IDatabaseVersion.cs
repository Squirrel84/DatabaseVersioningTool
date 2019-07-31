namespace DatabaseVersioningTool.DataAccess.Models.Interfaces
{
    public interface IDatabaseVersion
    {
        string Name { get; }
        string From { get; }
        string To { get; }
    }
}
