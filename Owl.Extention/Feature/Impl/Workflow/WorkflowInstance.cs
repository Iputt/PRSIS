using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Util;
using Owl.Domain.Driver;
using Owl.Domain;

namespace Owl.Feature.Workflows
{
    /// <summary>
    /// 实例状态
    /// </summary>
    public enum InstanceStatus
    {
        Draft,
        Running,
        Complete,
        Stop
    }

    /// <summary>
    /// 流程实例
    /// </summary>
    public class WorkflowInstance : SmartObject
    {
        /// <summary>
        /// 本实例的聚合根引用
        /// </summary>
        public AggRoot Root { get; private set; }

        /// <summary>
        /// 实例状态
        /// </summary>
        public InstanceStatus Status { get; set; }
        /// <summary>
        /// 当前活动
        /// </summary>
        public Activity Current { get; set; }

        /// <summary>
        /// 已完成信号
        /// </summary>
        public string Complete { get; set; }

        Workflow _workflow;
        public Workflow Workflow
        {
            get
            {
                if (_workflow == null)
                    _workflow = WorkflowEngine.GetWorkflow(Root.Metadata.Name);
                return _workflow;
            }
        }

        private WorkflowInstance()
        {
        }
        public static WorkflowInstance Create(Activity current, InstanceStatus status, string complete)
        {
            var instance = new WorkflowInstance()
            {
                Current = current,
                Status = status,
                Complete = complete
            };
            return instance;
        }
        /// <summary>
        /// 加载聚合根的流程实例，若该聚合根没有实例，则创建并启动实例
        /// </summary>
        /// <param name="root"></param>
        /// <returns></returns>
        public static WorkflowInstance Load(AggRoot root, bool start = true)
        {
            if (root == null)
                throw new ArgumentNullException("root");
            var modelname = root.Metadata.Name;
            var instance = WorkflowEngine.LoadInstance(modelname, root.Id);
            if (instance == null)
            {
                instance = new WorkflowInstance() { Status = InstanceStatus.Draft };
            }
            instance.Root = root;
            if (instance.Status == InstanceStatus.Draft && start)
            {
                instance.ReStart();
            }
            return instance;
        }
        object _executeactivity(Activity next, TransferObject dto)
        {
            if (next.Behaviors != null)
                MsgContext.Current.Behaviors.AddRange(next.Behaviors);
            var result = next.Execute(Root, dto);
            if (next.Kind == ActivityKind.Dummy)
                return result;
            Current = next;
            if (next.End)
                Status = InstanceStatus.Complete;
            else
            {
                Status = InstanceStatus.Running;
                foreach (var tran in next.Transitions.Where(s => s.Signal == null || s.Signal == ""))
                {
                    if (tran.Specific == null || tran.Specific.IsValid(Root))
                    {
                        _executeactivity(tran.Destination, null);
                        break;
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// 执行下一步活动
        /// </summary>
        /// <param name="next">下一步活动</param>
        /// <param name="dto">传输对象</param>
        /// <returns></returns>
        object ExecuteActivity(Activity next, TransferObject dto)
        {
            var result = _executeactivity(next, dto);
            Save();
            return result;
        }
        /// <summary>
        /// 启动本实例
        /// </summary>
        public void ReStart()
        {
            var workflow = Workflow;
            var next = workflow.Activities.FirstOrDefault(s => s.Start);
            _executeactivity(next, new TransferObject());
            Save();
            //            ExecuteActivity(next, new TransferObject());
        }

        /// <summary>
        /// 恢复工作流执行
        /// </summary>
        /// <param name="signal">信号</param>
        /// <param name="dto">数据传输对象</param>
        /// <param name="asadmin"></param>
        public object Resume(string signal, TransferObject dto, bool asadmin = false)
        {
            if (string.IsNullOrEmpty(signal))
                throw new ArgumentNullException("signal");
            if (Current.Transitions.Count == 0)
                throw new Exception2("流程设计有误：本活动缺少迁移!");

            Transition transit = Current.Transitions.FirstOrDefault(s => s.Signal == signal);
            if (transit == null)
                throw new Exception(string.Format("信号{0}无效", signal));
            if (transit.Specific != null && !transit.Specific.IsValid(Root))
                throw new Exception2(string.Format("当前对象不满足执行信号{0}的条件", signal));

            return ExecuteActivity(transit.Destination, dto);
        }

        public object ResumeSubflow(bool success, string message = "")
        {
            if (Current.Kind != ActivityKind.Subflow)
            {
                throw new Exception2("当前活动不是可调用的子流程！");
            }
            return Resume(success ? Current.SuccessSignal : Current.FailSignal, null);
        }

        /// <summary>
        /// 将流程切换到某个特定的节点
        /// </summary>
        /// <param name="status"></param>
        public void Forward(string status)
        {
            if (Current.Status == status)
                return;
            var actiity = Workflow.Activities.FirstOrDefault(s => s.Kind == ActivityKind.Status && s.Status == status);
            if (actiity != null)
            {
                actiity.Execute(Root, null);
                Current = actiity;
                Save();
            }
        }

        public void Back()
        {
            var activities = Workflow.Activities.OrderBy(s => s.Sequence).ToList();
            var cindex = activities.IndexOf(Current);
            Activity prev = null;
            for (var i = cindex - 1; i < cindex; i--)
            {
                prev = activities[i];
                if (prev.Kind == ActivityKind.Status)
                    break;
            }
            if (prev != null)
            {
                prev.Execute(Root, null);
                Current = prev;
                Save();
            }
        }
        public void Stop()
        {
            Status = InstanceStatus.Stop;
            Save();
        }
        /// <summary>
        /// 保存本实例
        /// </summary>
        void Save()
        {
            WorkflowEngine.SaveInstance(this);
        }
    }
}
