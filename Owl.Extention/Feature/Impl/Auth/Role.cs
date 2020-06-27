using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Domain;
using Owl.Const;

namespace Owl.Feature.iAuth
{
    /// <summary>
    /// 角色组，定义一组功能相近的角色
    /// </summary>
    public class RoleType : SmartObject
    {
        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }


        /// <summary>
        /// 账户类型
        /// </summary>
        public HashSet<string> AccountType { get; set; }

        /// <summary>
        /// 显示名称
        /// </summary>
        public string Display { get; set; }
        /// <summary>
        /// 序号
        /// </summary>
        public int Ordinal { get; set; }

        /// <summary>
        /// 不可被分配
        /// </summary>
        public bool NoAlloc { get; set; }

        /// <summary>
        /// 来源
        /// </summary>
        public MetaMode Mode { get; set; }
    }

    /// <summary>
    /// 角色
    /// </summary>
    public class Role : SmartObject
    {
        string m_name;
        /// <summary>
        /// 名称
        /// </summary>
        public string Name
        {
            get { return m_name; }
            set { m_name = value; fullname = null; }
        }

        /// <summary>
        /// 角色目标
        /// </summary>
        public RoleTarget Target { get; set; }

        string m_type;
        /// <summary>
        /// 类型
        /// </summary>
        public string Type
        {
            get { return m_type; }
            set { m_type = value; fullname = null; }
        }

        string fullname;
        /// <summary>
        /// 角色全名
        /// </summary>
        public string FullName
        {
            get
            {
                if (fullname == null)
                    fullname = RoleName.BuildFull(Type, Name);
                return fullname;
            }
        }

        /// <summary>
        /// 显示名称
        /// </summary>
        public string Display { get; set; }
        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// 序号
        /// </summary>
        public int Ordinal { get; set; }

        /// <summary>
        /// 来源
        /// </summary>
        public MetaMode Mode { get; set; }

        /// <summary>
        /// 子角色
        /// </summary>
        public string[] Children { get; set; }

        /// <summary>
        /// 构建完整角色名称
        /// </summary>
        /// <param name="type"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string BuildFullName(string type, string name)
        {
            return string.Format("{0}_{1}", type, name);
        }
    }
}
