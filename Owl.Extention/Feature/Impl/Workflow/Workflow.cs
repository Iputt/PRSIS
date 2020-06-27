using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Util;
using Owl.Domain.Driver;
using Owl.Domain;
namespace Owl.Feature.Workflows
{
    public enum ActivityKind
    {
        [DomainLabel("状态")]
        Status,
        /// <summary>
        /// 执行功能
        /// </summary>
        [DomainLabel("功能")]
        Function,
        /// <summary>
        /// 孤立的动作，不会发生状态迁移
        /// </summary>
        [DomainLabel("孤立")]
        Dummy,
        /// <summary>
        /// 子流程
        /// </summary>
        [DomainLabel("子流程")]
        Subflow,
        /// <summary>
        /// 终止
        /// </summary>
        [DomainLabel("终止所有")]
        Stopall
    }

    public enum ActivityCondition
    {
        [DomainLabel("无条件")]
        None,
        [DomainLabel("与")]
        And,
        [DomainLabel("或")]
        Or,
        [DomainLabel("异或")]
        Xor
    }

    /// <summary>
    /// 迁移
    /// </summary>
    public class Transition : SmartObject
    {
        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 源活动
        /// </summary>
        [IgnoreField]
        public Activity Source { get; set; }
        /// <summary>
        /// 目标活动
        /// </summary>
        [IgnoreField]
        public Activity Destination { get; set; }
        /// <summary>
        /// 信号,为空表示可自动触发的信号
        /// </summary>
        public string Signal { get; set; }

        string m_condition;
        /// <summary>
        /// 迁移条件
        /// </summary>
        public string Condition
        {
            get { return m_condition; }
            set
            {
                m_condition = value;
                m_specific = null;
            }
        }

        Specification m_specific;
        public Specification Specific
        {
            get
            {
                if (m_specific == null && (Source.Condition != null || !string.IsNullOrEmpty(Condition)))
                {
                    if (!string.IsNullOrEmpty(Condition))
                        m_specific = Specification.Create(Condition);
                    if (Source.Condition != null)
                        m_specific = Specification.And(Source.Condition, m_specific);
                }
                return m_specific;
            }
        }

        /// <summary>
        /// 角色
        /// </summary>
        public string[] Roles { get; set; }
    }

    /// <summary>
    /// 活动
    /// </summary>
    public class Activity : SmartObject
    {
        /// <summary>
        /// 活动Id
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 动作类型
        /// </summary>
        public ActivityKind Kind { get; set; }

        /// <summary>
        /// 子流程
        /// </summary>
        public Guid? SubflowId { get; set; }

        /// <summary>
        /// 子流程成功时的回调信号
        /// </summary>
        public string SuccessSignal { get; set; }

        /// <summary>
        /// 子流程失败时的回调信号
        /// </summary>
        public string FailSignal { get; set; }
        /// <summary>
        /// 加入条件与OutCondition组合可构成并行流程
        /// </summary>
        public ActivityCondition InCondition { get; set; }

        /// <summary>
        /// 分离条件与InCondition组合可构成并行流程
        /// </summary>
        public ActivityCondition OutCondition { get; set; }

        string m_vewmodel;
        /// <summary>
        /// 视图对象
        /// </summary>
        protected string ViewModel
        {
            get { return m_vewmodel; }
            set
            {
                m_vewmodel = value;
                m_viewmeta = null;
            }
        }

        ModelMetadata m_viewmeta;
        /// <summary>
        /// 视图对象元数据
        /// </summary>
        public ModelMetadata ViewMeta
        {
            get
            {
                if (m_viewmeta == null && !string.IsNullOrEmpty(ViewModel))
                {
                    m_viewmeta = ModelMetadataEngine.GetModel(ViewModel);
                }
                return m_viewmeta;
            }
        }
        /// <summary>
        /// python代码
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// 邦定状态
        /// </summary>
        public string Status { get; set; }

        string str_context;
        /// <summary>
        /// 状态上下文,json格式
        /// </summary>
        public string Context
        {
            get { return str_context; }
            set
            {
                str_context = value;
                m_context = null;
                _condition = null;
            }
        }

        TransferObject m_context;
        /// <summary>
        /// 获取状态上下文
        /// </summary>
        /// <returns></returns>
        protected TransferObject _Context
        {
            get
            {
                if (m_context == null && !string.IsNullOrEmpty(Context))
                {
                    m_context = Context.DeJson<TransferObject>();
                }
                return m_context;
            }
        }
        Specification _condition;
        /// <summary>
        /// 关联迁移过滤条件
        /// </summary>
        public Specification Condition
        {
            get
            {
                if (_condition == null)
                {
                    if (Kind == ActivityKind.Status && !string.IsNullOrEmpty(Status))
                        _condition = Specification.Create("Status", CmpCode.EQ, Status);
                    if (_Context != null && _Context.Count > 0)
                    {
                        foreach (var pair in _Context)
                        {
                            var spec = Specification.Create(pair.Key, CmpCode.EQ, pair.Value);
                            if (_condition == null)
                                _condition = spec;
                            else
                                _condition = Specification.And(_condition, spec);
                        }
                    }
                }
                return _condition;
            }
        }
        /// <summary>
        /// 是否是开始工作流
        /// </summary>
        public bool Start { get; set; }
        /// <summary>
        /// 是否是终止工作流
        /// </summary>
        public bool End { get; set; }

        /// <summary>
        /// 主线序号，空表示分支
        /// </summary>
        public int? Sequence { get; set; }
        List<Transition> m_transitions;
        /// <summary>
        /// 迁移
        /// </summary>
        [IgnoreField]
        public List<Transition> Transitions
        {
            get
            {
                if (m_transitions == null)
                    m_transitions = new List<Transition>();
                return m_transitions;
            }
            set { m_transitions = value; }
        }

        /// <summary>
        /// 活动执行之后
        /// </summary>
        public IEnumerable<IMessageBehavior> Behaviors { get; set; }

        /// <summary>
        /// 同步状态
        /// </summary>
        /// <param name="root"></param>
        public void SyncContext(AggRoot root)
        {
            bool canpush = false;
            if (!string.IsNullOrEmpty(Status) && Kind == ActivityKind.Status)
            {
                root["Status"] = Status;
                canpush = true;
            }
            if (_Context != null && _Context.Count > 0)
            {
                root.Write(_Context);
                canpush = true;
            }
            if (canpush)
                root.Push();
        }

        public object Execute(AggRoot root, TransferObject dto)
        {
            object result = null;
            FormObject obj = null;
            if (MsgContext.Current.Handler != null)
                obj = MsgContext.Current.Handler.FormObj;
            if (obj == null && ViewMeta != null && ViewMeta.ObjType == DomainType.Form && dto != null)
            {
                obj = DomainFactory.Create<FormObject>(ViewMeta);
                obj.Write(dto);
            }
            if (!string.IsNullOrEmpty(Code) && Code.Split('\n').Any(s => !s.Trim().StartsWith("#")))
            {
                Dictionary<string, object> dict = new Dictionary<string, object>();
                dict["self"] = root;
                dict["dto"] = obj;
                dict["form"] = obj;
                result = Script.Execute(Name, Code, dict);
                root.Push();
            }
            SyncContext(root);
            return result;
        }
    }


    public class Workflow : SmartObject
    {
        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 对象名称
        /// </summary>
        public string ModelName { get; set; }

        /// <summary>
        /// 对象创建时是否同时创建工作流
        /// </summary>
        public bool Create { get; set; }

        List<Activity> m_activities;
        /// <summary>
        /// 活动
        /// </summary>
        [IgnoreField]
        public List<Activity> Activities
        {
            get
            {
                if (m_activities == null)
                    m_activities = new List<Activity>();
                return m_activities;
            }
            set { m_activities = value; }
        }

        List<Transition> m_transitions;
        /// <summary>
        /// 迁移
        /// </summary>
        [IgnoreField]
        public List<Transition> Transitions
        {
            get
            {
                if (m_transitions == null)
                    m_transitions = new List<Transition>();
                return m_transitions;
            }
            set
            {
                m_transitions = value;
            }
        }

        public Transition GetTransition(string signal)
        {
            return Transitions.FirstOrDefault(s => s.Signal == signal);
        }
    }
}
