using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Util;
using Owl.Const;
namespace Owl.Domain
{
    public enum RoleTarget
    {
        [DomainLabel("消息")]
        Message,
        [DomainLabel("字段")]
        Field
    }
    /// <summary>
    /// 模块，角色类别
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class RoleCategoryAttribute : Attribute
    {
        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// 显示名称
        /// </summary>
        public string Display { get; private set; }

        /// <summary>
        /// 账户类型
        /// </summary>
        public string[] AccountType { get; private set; }

        /// <summary>
        /// 序号
        /// </summary>
        public int Ordinal { get; private set; }

        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 不显示在select中
        /// </summary>
        public bool NoAlloc { get; set; }

        /// <summary>
        /// 模块、角色类别
        /// </summary>
        /// <param name="name">名称</param>
        /// <param name="display">显示名称</param>
        /// <param name="ordinal">序号</param>
        /// <param name="accounttype">Owl.Const.SelectConst 中取值， 默认为客户端（租户系统）</param>
        public RoleCategoryAttribute(string name, string display, int ordinal, params string[] accounttype)
        {
            Name = name;
            Display = display;
            Ordinal = ordinal;
            AccountType = (accounttype == null || accounttype.Length == 0) ? new string[] { SelectConst.AccountType_Tenant } : accounttype;
        }
    }
    /// <summary>
    /// 角色
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class RoleAttribute : Attribute
    {
        /// <summary>
        /// 角色类型
        /// </summary>
        public string Type { get; private set; }

        /// <summary>
        /// 角色名称
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// 显示名称
        /// </summary>
        public string Display { get; private set; }

        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// 序号
        /// </summary>
        public int Ordinal { get; private set; }

        /// <summary>
        /// 包含的角色
        /// </summary>
        public string[] Children { get; private set; }

        /// <summary>
        /// 角色作用范围
        /// </summary>
        public RoleTarget Target { get; set; }
        /// <summary>
        /// 角色
        /// </summary>
        /// <param name="type">应用名称</param>
        /// <param name="name">角色名称</param>
        /// <param name="display">显示名称</param>
        /// <param name="ordinal">序号</param>
        /// <param name="description">描述</param>
        /// <param name="children">包含的角色</param>
        public RoleAttribute(string type, string name, string display, byte ordinal, string description, params string[] children)
        {

            Name = name;
            Display = display;
            Type = type;
            Description = description;
            Ordinal = ordinal;
            Children = children;
            Target = RoleTarget.Message;
        }
    }
}
