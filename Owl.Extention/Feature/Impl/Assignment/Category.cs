using System;
using Owl.Domain;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using Owl.Util;

namespace Owl.Feature.Assignments
{
    public class Issueflow : SmartObject
    {
        /// <summary>
        /// 节点key
        /// </summary>
        protected Guid Id { get; private set; }

        /// <summary>
        /// 节点名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 责任人
        /// </summary>
        protected string AssignTo { get; private set; }

        /// <summary>
        /// 责任人 python
        /// </summary>
        protected string AssignTo2 { get; private set; }

        Specification m_specification;
        string m_condition;
        /// <summary>
        /// Gets the condition.
        /// </summary>
        /// <value>The condition.</value>
        public string Condition
        {
            get { return m_condition; }
            private set
            {
                m_condition = value;
                m_specification = null;
                if (!string.IsNullOrEmpty(value))
                    m_specification = Specification.Create(value);
            }
        }

        /// <summary>
        /// Gets the sequence.
        /// </summary>
        /// <value>The sequence.</value>
        public int Sequence { get; private set; }

        /// <summary>
        /// 当前节点所占进度
        /// </summary>
        public int Progress { get; private set; }

        public bool IsValid(AggRoot root)
        {
            if (m_specification == null)
                return true;
            return m_specification.IsValid(root);
        }

        public string GetAssignTo(AggRoot root)
        {
            var assign = "";
            if (!string.IsNullOrEmpty(AssignTo))
                assign = AssignTo;
            else if (!string.IsNullOrEmpty(AssignTo2))
            {
                var funcname = string.Format("getassign_{0}", Id.ToString().Replace("-", ""));
                var param = new Dictionary<string, object>();
                param["self"] = root;
                Script.ScriptType = ScriptType.Python;
                assign = (string)Script.Execute(funcname, AssignTo2, param);
            }
            return assign == "" ? null : assign;
        }
    }

    /// <summary>
    /// 任务节点
    /// </summary>
    public class IssueNode
    {
        /// <summary>
        /// 节点名称
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// 分配给
        /// </summary>
        public string AssignTo { get; private set; }

        /// <summary>
        /// 下个节点序号
        /// </summary>
        public int Next { get; private set; }

        public int Progress { get; private set; }

        public IssueNode(string name, string assignto, int next, int progress)
        {
            Name = name;
            AssignTo = assignto;
            Next = next;
            Progress = progress;
        }
    }

    /// <summary>
    /// 任务类别
    /// </summary>
    public class Category : SmartObject
    {

        #region the private method

        object executePython(string name, string code, AggRoot root, AggRoot issue = null)
        {
            if (root == null)
                return null;
            var funcname = string.Format("{0}_{1}", name, Id.ToString().Replace("-", ""));
            var param = new Dictionary<string, object>();
            param["self"] = root;
            if (issue != null)
                param["issue"] = issue;
            Script.ScriptType = ScriptType.Python;
            return Script.Execute(funcname, code, param);
        }

        #endregion

        /// <summary>
        /// Id
        /// </summary>
        public object Id { get; set; }

        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// 对象名称
        /// </summary>
        public string Model { get; private set; }

        /// <summary>
        /// 限时
        /// </summary>
        public int Interval { get; private set; }

        /// <summary>
        /// the issue importance
        /// </summary>
        /// <value>The rank.</value>
        public Scene Rank { get; private set; }
        /// <summary>
        /// is the issue can auto create and done
        /// </summary>
        public bool IsAuto { get; private set; }



        SortedSet<Issueflow> m_flow;
        /// <summary>
        /// get the flows
        /// </summary>
        /// <value>The flow.</value>
        public SortedSet<Issueflow> Flows
        {
            get
            {
                if (m_flow == null)
                    m_flow = new SortedSet<Issueflow>(Comparer2<Issueflow>.Asc(s => s.Sequence));
                return m_flow;
            }
        }

        public IssueNode GetAssignTo(AggRoot root, int? current = null)
        {
            var cvalue = current ?? 0;
            Issueflow next = null;
            if (Flows.Count == 1)
                next = cvalue == 0 ? Flows.ElementAt(0) : null;
            else
            {
                if (current == 0 && string.IsNullOrEmpty(Flows.FirstOrDefault().Condition))
                    cvalue = cvalue + 1;
                var length = Flows.Count;

                for (var i = 0; i < length; i++)
                {
                    var flow = Flows.ElementAt(cvalue);
                    if (flow.IsValid(root))
                    {
                        next = flow;
                        break;
                    }
                    cvalue = cvalue + 1;
                    if (cvalue >= length)
                        cvalue = cvalue - length;
                }
            }
            if (next == null)
                return null;

            if (current == null)
                next = Flows.ElementAt(0);

            return new IssueNode(next.Name, next.GetAssignTo(root), cvalue, next.Progress);
        }

        /// <summary>
        /// determines whether the root can create an issue
        /// </summary>
        /// <param name="root"></param>
        /// <returns></returns>
        public bool AutoCreate(AggRoot root)
        {
            var flow = Flows.FirstOrDefault();
            if (!IsAuto || string.IsNullOrEmpty(flow.Condition))
                return false;
            return flow.IsValid(root);
        }

        #region partner

        /// <summary>
        /// 关联客户
        /// </summary>
        /// <value>The partner.</value>
        protected string Partner { get; private set; }

        /// <summary>
        /// Gets the partner.
        /// </summary>
        /// <returns>The partner.</returns>
        /// <param name="root">Root.</param>
        public Guid GetPartner(AggRoot root)
        {
            Guid result = Guid.Empty;
            if (!string.IsNullOrEmpty(Partner))
            {
                var tmp = executePython("getpartner", Partner, root);
                if (tmp != null)
                    result = (Guid)tmp;
            }
            return result;
        }

        #endregion

        #region approver

        protected string Approver { get; private set; }

        protected string Approver2 { get; private set; }

        /// <summary>
        /// Gets the approver.
        /// </summary>
        /// <returns>The approver.</returns>
        /// <param name="root">Root.</param>
        public string GetApprover(AggRoot root)
        {
            var approver = "";
            if (!string.IsNullOrEmpty(Approver))
                approver = Approver;
            else if (!string.IsNullOrEmpty(Approver2))
                approver = (string)executePython("getassign", Approver2, root);

            return approver == "" ? null : approver;
        }

        #endregion

        #region 任务触发条件

        string m_trigger;
        Specification m_spectrig;
        protected string Trigger
        {
            get { return m_trigger; }
            set
            {
                m_trigger = value;
                m_spectrig = null;
            }
        }
        /// <summary>
        /// 是否可触发本任务
        /// </summary>
        /// <param name="root"></param>
        /// <returns></returns>
        public bool CanTrigger(AggRoot root)
        {
            if (string.IsNullOrEmpty(Trigger))
                return true;
            if (m_spectrig == null)
                m_spectrig = Specification.Create(Trigger);
            return m_spectrig.IsValid(root);
        }

        #endregion

        #region condition for complete

        string m_conddone;
        Specification m_specdone;

        protected string CondDone
        {
            get { return m_conddone; }
            set
            {
                m_conddone = value;
                m_specdone = null;
            }
        }

        /// <summary>
        /// Determines whether this instance candone  root.
        /// </summary>
        /// <returns><c>true</c> if this instance candone root; otherwise, <c>false</c>.</returns>
        /// <param name="root">Root.</param>
        public bool Candone(AggRoot root, bool mustauto = false)
        {
            if (mustauto && string.IsNullOrEmpty(CondDone))
                return false;
            if (string.IsNullOrEmpty(CondDone))
                return true;

            if (m_specdone == null)
            {
                m_specdone = Specification.Create(CondDone);
            }
            return m_specdone.IsValid(root);
        }

        #endregion

        #region tag

        /// <summary>
        /// 标签 python
        /// </summary>
        protected string Tag { get; private set; }

        /// <summary>
        /// Gets the tag.
        /// </summary>
        /// <returns>The tag.</returns>
        /// <param name="root">Root.</param>
        public string GetTag(AggRoot root)
        {
            if (string.IsNullOrEmpty(Tag))
                return "";
            return (string)executePython("gettag", Tag, root);
        }

        #endregion

        #region event

        protected string CodeGet { get; set; }

        /// <summary>
        /// Raises the get event.
        /// </summary>
        /// <param name="root">Root.</param>
        /// <param name="issue">Issue.</param>
        public void OnGet(AggRoot root, AggRoot issue)
        {
            if (string.IsNullOrEmpty(CodeGet))
                return;
            executePython("onget", CodeGet, root, issue);
            root.Push();
        }

        protected string CodeDone { get; set; }

        /// <summary>
        /// Raises the done event.
        /// </summary>
        /// <param name="root">Root.</param>
        /// <param name="issue">Issue.</param>
        public void OnDone(AggRoot root, AggRoot issue)
        {
            if (string.IsNullOrEmpty(CodeDone))
                return;
            executePython("ondone", CodeDone, root, issue);
            root.Push();
        }

        #endregion

        #region the summary

        /// <summary>
        /// 摘要
        /// </summary>
        protected string Summary { get; private set; }

        /// <summary>
        /// Gets the summary.
        /// </summary>
        /// <returns>The summary.</returns>
        /// <param name="root">Root.</param>
        public string GetSummary(AggRoot root)
        {
            if (string.IsNullOrEmpty(Summary))
                return "";
            return (string)executePython("getsummary", Summary, root);
        }

        #endregion

    }
}