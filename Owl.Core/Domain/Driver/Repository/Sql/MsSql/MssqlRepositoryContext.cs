using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
namespace Owl.Domain.Driver.Repository.Sql
{
    public class MssqlRepositoryContext : SqlRepositoryContext
    {
        protected override string PrefixName
        {
            get { return "mssql"; }
        }
        protected override DbConnection CreateConnection(string connectionstring)
        {
            return new System.Data.SqlClient.SqlConnection(connectionstring);
        }
    }
}
