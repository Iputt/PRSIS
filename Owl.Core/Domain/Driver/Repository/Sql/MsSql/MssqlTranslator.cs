using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
namespace Owl.Domain.Driver.Repository.Sql
{
    public class MssqlTranslator : SqlTranslator
    {
        protected override string Wrapper(string name)
        {
            return string.Format("[{0}]", name);
        }
        protected override string BuildIsNull(string left, string right)
        {
            return string.Format("isnull({0},{1})", left, right);
        }

        protected override string BuildContain(string left, string right)
        {
            return string.Format("CharIndex({0},{1})>0", right, left);
        }
        protected override string BuildIndexOf(string left, string right)
        {
            return string.Format("CharIndex({0},{1}) -1", right, left);
        }
        public override string BuildTop1(string cmd)
        {
            return string.Format("select top 1 {0}", cmd);
        }
        
    }
}
