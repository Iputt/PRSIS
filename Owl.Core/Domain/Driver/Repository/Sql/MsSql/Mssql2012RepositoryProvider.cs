using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;

namespace Owl.Domain.Driver.Repository.Sql
{
    public class Mssql2012RepositoryProvider<TRoot> : MssqlRepositoryProvider<TRoot>
        where TRoot : AggRoot
    {

        protected override IEnumerable<TRoot> Execute(Expression<Func<TRoot, bool>> expression, SortBy sortby, int start, int count, string[] selector)
        {
            return base.Execute(expression, WrapSort(sortby), start, count, selector);
        }
        protected override IEnumerable<TransferObject> Execute2(Expression<Func<TRoot, bool>> expression, SortBy sortby, int start, int count, bool translate, string[] selector)
        {
            return base.Execute2(expression, WrapSort(sortby), start, count, translate, selector);
        }
        protected override string GetQueryString(SqlUnit fields,QueryCommand where, SqlSort sortby, int start, int limit, bool child)
        {
            SqlUnit unit = sortby.GetUnit(child ? true : false);
            string orderbys = unit.StrOrder;
            fields.AddTable(where.Tables.ToArray());
            fields.AddTable(unit.Tables.ToArray());
            var whereexp = where.CommandText;
            if (limit == 0)
            {
                return string.Format("select {0} from {1} where {2} {3}", fields.StrField, fields.StrTable, whereexp, orderbys);
            }
            else
            {
                return string.Format("select {0} from {1} where {2} {3} offset {4} rows fetch next {5} rows only", fields.StrField, fields.StrTable, whereexp, orderbys, start, limit);
            }
        }
    }
}
