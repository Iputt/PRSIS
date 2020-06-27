using Owl.Feature.iScript;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Owl.Feature.Impl.Script.Api
{
    public class DateApi : ScriptRuntimeApi
    {
        public override string Name => "date";

        protected override int Priority => 1;

        public DateTime AddYears(DateTime org, int value)
        {
            return org.AddYears(value);
        }

        public DateTime AddMonths(DateTime org, int value)
        {
            return org.AddMonths(value);
        }

        public DateTime AddDays(DateTime org, int days)
        {
            return org.AddDays(days);
        }

        public DateTime AddHours(DateTime org, int value)
        {
            return org.AddHours(value);
        }

        public string ToString(DateTime org, string format)
        {
            return org.ToLocalTime().ToString(format);
        }
    }
}
