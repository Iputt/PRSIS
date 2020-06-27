using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
namespace Owl.Domain.Driver.Repository.Sql
{
    public class NpgsqlRepositoryContext : SqlRepositoryContext
    {
        protected override string PrefixName
        {
            get { return "npgsql"; }
        }
        protected override DbConnection CreateConnection(string connectionstring)
        {
            return new Npgsql.NpgsqlConnection(connectionstring);
        }
    }
}
