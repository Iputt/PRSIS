using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
namespace Owl.Domain.Driver.Repository.Sql
{
    public class NpgsqlTranslator : SqlTranslator
    {
        protected override bool HasBoll
        {
            get
            {
                return true;
            }
        }
        protected override string BuildIsNull(string left, string right)
        {
            return string.Format("COALESCE({0},{1})", left, right);
        }

        protected override string BuildContain(string left, string right)
        {
			return string.Format("POSITION({0} in {1}) > 0", right, left);
        }
        protected override string BuildIndexOf(string left, string right)
        {
            return string.Format("POSITION({0} in {1}) - 1", left, right);
        }
        protected override string BuildTop1(string cmd)
        {
            return string.Format("select {0} limit 1 ", cmd);
        }
    }
}
