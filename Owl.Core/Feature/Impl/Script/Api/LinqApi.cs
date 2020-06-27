using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Collections;

namespace Owl.Feature.iScript.Api
{
    public class LinqApi : ScriptRuntimeApi
    {
        public override string Name => "linq";

        protected override int Priority => 1;

        public IEnumerable<IGrouping<object, object>> GroupBy(IEnumerable<object> elements, Func<object, object> keySelector)
        {
            return elements.GroupBy(keySelector);
        }

        public object Find(IEnumerable<object> elements, Func<object, bool> predicate)
        {
            return elements.Where(predicate).ToArray();
        }
    }
}
