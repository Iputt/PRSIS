using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Owl.Feature.iPlan
{
    public class PlanEngine : Engine<PlanProvider, PlanEngine>
    {
        public static IEnumerable<PlanDescription> GetDescriptions()
        {
            return Execute3<PlanDescription>(s => s.GetDescription);
        }

        public static PlanDescription GetDescription(string name)
        {
            return Execute2<string, PlanDescription>(s => s.GetDescription, name);
        }

        public static IEnumerable<Trigger> GetTriggers(string planname)
        {
            return Execute3<string, Trigger>(s => s.GetTriggers, planname);
        }
    }
}
