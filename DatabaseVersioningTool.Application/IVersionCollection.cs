using System.Collections.Generic;

namespace DatabaseVersioningTool.DataAccess
{
    public interface IDatabaseVersionCollection<C> where C : IDatabaseVersion
    {
        string Name { get; set; }
        IList<C> Versions { get; }
        void AddVersion(C item);
    }
}
