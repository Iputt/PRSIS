using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Util;
using Owl.Feature.iPlan;
using Owl.Domain;
namespace Owl.Feature
{
    public class Plan
    {
        static bool Running = false;
        static object locker = new object();
        static string TaskName;
        /// <summary>
        /// 启动计划调度器，每分钟调度一次
        /// </summary>
        [OnApplicationGetMC(Ordinal = 10)]
        public static void Start()
        {
            if (!Running)
            {
                lock (locker)
                {
                    if (!Running)
                    {
                        TaskName = TaskMgr.AddTask(Schedule, 60, DateTime.Now.Precision(TimePrecision.Minute, true));
                        Running = true;
                    }
                }
            }
        }
        /// <summary>
        /// 停止任务计划
        /// </summary>
        [OnApplicationLoseMC(Ordinal = 0)]
        public static void Stop()
        {
            if (Running)
            {
                lock (locker)
                {
                    if (Running)
                    {
                        TaskMgr.RemoveTask(TaskName);
                        Running = false;
                    }
                }
            }
        }

        static Dictionary<string, IAsyncResult> jobs = new Dictionary<string, IAsyncResult>();

        static Action<PlanDescription, DateTime?> PlanAction = (s, t) =>
        {
            var success = true;
            var summary = "";
            var handler = s.Create();
            try
            {
                OwlContext.Current.UserName = AppConfig.GetSetting("Plan_Username");
                OwlContext.Current.Mandt = AppConfig.GetSetting("Plan_Mandt");
                summary = handler.Execute();
                DomainContext.Current.Commit();
            }
            catch (Exception ex)
            {
                success = false;
                summary = ex.Message;
                try
                {
                    handler.OnError(ex);
                }
                catch
                {

                }
            }
            finally
            {
                if (t.HasValue)
                    PlanStateEngine.Complete(s, success, t.Value, DateTime.Now, summary);
            }
        };
        static bool inscheduling = false;
        /// <summary>
        /// 执行计划
        /// </summary>
        /// <param name="trigger">触发时间</param>
        public static void Schedule(DateTime trigger)
        {
            if (inscheduling)
                return;
            inscheduling = true;
            try
            {
                var descriptions = GetDescriptions(trigger.Precision(TimePrecision.Minute));
                foreach (var des in descriptions)
                {

                    if (!jobs.ContainsKey(des.Name) || jobs[des.Name].IsCompleted)
                        jobs[des.Name] = TaskMgr.StartTask(PlanAction, des, trigger);
                }
            }
            catch
            {

            }
            finally
            {
                inscheduling = false;
            }
        }
        static void Schedule()
        {
            Schedule(DateTime.Now);
        }

        public static IEnumerable<PlanDescription> GetDescriptions(DateTime trigger)
        {
            var running = PlanStateEngine.GetStates();
            var result = new List<PlanDescription>();
            foreach (var des in PlanEngine.GetDescriptions())
            {
                DateTime? last = null;
                if (running.ContainsKey(des.Name))
                    last = running[des.Name].Precision(TimePrecision.Minute);
                var triggers = PlanEngine.GetTriggers(des.Name);
                if (triggers.Any(s => s.CanTrigger(trigger, last)))
                    result.Add(des);
            }
            return result;
        }

        public static void ExecutePlan(string planname)
        {
            var description = PlanEngine.GetDescription(planname);
            if (description != null)
            {
                TaskMgr.StartTask(PlanAction, description, null);
            }
        }
    }
}
