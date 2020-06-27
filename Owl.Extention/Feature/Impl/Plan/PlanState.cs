using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Util;
namespace Owl.Feature.iPlan
{
    public abstract class PlanStateProvider : Provider
    {
        public abstract IEnumerable<KeyValuePair<string, DateTime>> GetStates();

        public abstract void Complete(PlanDescription plan, bool success, DateTime start, DateTime end, string summary);
    }

    public class EmbedPlanStateProvider : PlanStateProvider
    {
        Dictionary<string, DateTime> running = new Dictionary<string, DateTime>();
        public override IEnumerable<KeyValuePair<string, DateTime>> GetStates()
        {
            return running.AsEnumerable();
        }

        public override void Complete(PlanDescription plan, bool success, DateTime start, DateTime end, string summary)
        {
            // if (success)
            running[plan.Name] = start;
        }

        public override int Priority
        {
            get { return 1; }
        }
    }

    public class PlanStateEngine : Engine<PlanStateProvider, PlanStateEngine>
    {
        /// <summary>
        /// 获取计划的执行情况
        /// </summary>
        /// <returns></returns>
        public static Dictionary<string, DateTime> GetStates()
        {
            Dictionary<string, DateTime> states = new Dictionary<string, DateTime>();
            foreach (var provider in Providers)
            {
                foreach (var pair in provider.GetStates())
                {
                    if (!states.ContainsKey(pair.Key))
                        states[pair.Key] = pair.Value;
                }
            }
            return states;
        }

        public static void Complete(PlanDescription plan, bool success, DateTime start, DateTime end, string summary)
        {
            Execute(s => s.Complete, plan, success, start, end, summary);
        }
    }
}
