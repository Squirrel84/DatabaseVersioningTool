using DatabaseVersioningTool.Application.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseVersioningTool.DataAccess.Sql
{
    public class SqlDatabaseUpdate : IDatabaseUpdate
    {
        public string Sql { get; set; }
    }
}
