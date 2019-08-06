using DatabaseVersioningTool.Application.Models.Interfaces;

namespace DatabaseVersioningTool.DataAccess.Sql
{
    public class SqlDatabaseUpdate : IDatabaseUpdate
    {
        public string Sql { get; set; }
    }
}
