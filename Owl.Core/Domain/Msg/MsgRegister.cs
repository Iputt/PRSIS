using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Owl.Domain
{
    /// <summary>
    /// 消息的数据限制
    /// </summary>
    public enum RootRestrict
    {
        /// <summary>
        /// 不限制，数据可有可无
        /// </summary>
        None,
        /// <summary>
        /// 单条数据
        /// </summary>
        Single,
        /// <summary>
        /// 特定的数据
        /// </summary>
        Special,
        /// <summary>
        /// 可以是特定的数据 也可以是符合条件的所有数据
        /// </summary>
        All
    }

    /// <summary>
    /// 注册消息处理器，可以用于实例类和方法上
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public sealed class MsgRegisterAttribute : DomainLabel
    {
        /// <summary>
        /// 消息名称
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// 消息权限名称，当两个消息 Lable 相同时，用于在权限配置中区分
        /// </summary>
        public string Alias { get; set; }
        /// <summary>
        /// 作用对象名称 * 表示所有对象，表示该处理器为通用处理器
        /// </summary>
        public string Model { get; private set; }

        /// <summary>
        /// 消息类型
        /// </summary>
        public MsgType Type { get; set; }

        bool? needlog;
        /// <summary>
        /// 是否记录本消息的操作记录,缺省为true
        /// </summary>
        public bool NeedLog
        {
            get
            {
                if (needlog == null)
                {
                    needlog = Restrict == RootRestrict.Special || Restrict == RootRestrict.Single;
                }
                return needlog.Value;
            }
            set { needlog = value; }
        }

        /// <summary>
        /// 执行条件
        /// </summary>
        public string Condition { get; set; }

        /// <summary>
        /// 确认消息
        /// </summary>
        public string Confirm { get; set; }
        Specification m_specific;
        /// <summary>
        /// 执行条件
        /// </summary>
        public Specification Specific
        {
            get
            {
                if (m_specific == null && !string.IsNullOrEmpty(Condition))
                    m_specific = Specification.Create(Condition);
                return m_specific;
            }
        }
        /// <summary>
        /// 数据限定 缺省为 特定的
        /// </summary>
        public RootRestrict Restrict { get; set; }

        /// <summary>
        /// 加载对象同时加载关系，多字段用‘;’分割
        /// </summary>
        public string Relations { get; set; }

        /// <summary>
        /// 是否自动展示在工具栏中
        /// </summary>
        public bool AutoShow { get; set; }

        /// <summary>
        /// 显示序号
        /// </summary>
        public int Ordinal { get; set; }

        /// <summary>
        /// 是否可进行权限配置
        /// </summary>
        public bool PermissionConfig { get; set; }

        /// <summary>
        /// 单例模式
        /// </summary>
        public bool Singleton { get; set; }

        /// <summary>
        /// 单例模式超时时间(秒)
        /// </summary>
        public int? SingleTimeout { get; set; }

        /// <summary>
        /// 单例模式执行中提示
        /// </summary>
        public string SingleNotify { get; set; }

        /// <summary>
        /// 单例模式执行中提示资源名称
        /// </summary>
        public string SingleNotifyResource { get; set; }
        /// <summary>
        /// 消息注册器
        /// </summary>
        /// <param name="name">消息名称</param>
        /// <param name="model"> 作用对象名称 *表示所有对象</param>
        /// <param name="label">展示名称</param>
        public MsgRegisterAttribute(string name, string model, string label)
            : base(string.IsNullOrEmpty(label) ? name : label)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("name");
            Name = name.ToLower();
            Model = model.ToLower();
            Type = MsgType.General;
            Restrict = RootRestrict.Special;
            //Resource = string.Format("msglabel.{0}.{1}", Model, Name);
        }
        /// <summary>
        /// 通用消息注册器，适用所有对象
        /// </summary>
        /// <param name="name">消息名称</param>
        /// <param name="label">展示名称</param>
        /// <param name="needlog">是否需要记录日志</param>
        public MsgRegisterAttribute(string name, string label)
            : this(name, "*", label)
        {
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name">消息名称</param>
        /// <param name="label">展示名称</param>
        /// <param name="autoshow">自动显示</param>
        /// <param name="ordinal">序号</param>
        public MsgRegisterAttribute(string name, string label, bool autoshow, int ordinal)
            : this(name, label)
        {
            AutoShow = autoshow;
            Ordinal = ordinal;
        }
    }
}
