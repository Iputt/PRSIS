using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Owl.Const
{
    /// <summary>
    /// 基本角色名称定义
    /// </summary>
    public class RoleName
    {
        /// <summary>
        /// 一般角色
        /// </summary>
        public const string General = "general";

        /// <summary>
        /// 队长角色
        /// </summary>
        public const string Leader = "leader";

        /// <summary>
        /// 经理角色
        /// </summary>
        public const string Manager = "manager";

        /// <summary>
        /// 构建完成角色名
        /// </summary>
        /// <param name="type"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string BuildFull(string type, string name)
        {
            return string.Format("{0}_{1}", type, name);
        }
    }
}
