using System;
using Owl.Util;
using System.Collections.Generic;
using Owl.Domain;
namespace Owl.Feature
{
    public class ChangeName
    {
        public string OrgName { get; private set; }

        public string DestName { get; private set; }

        public ChangeName(string orgname, string destname)
        {
            if (string.IsNullOrEmpty(orgname))
                throw new ArgumentNullException("orgname");
            if (string.IsNullOrEmpty(destname))
                throw new ArgumentNullException("destname");
            OrgName = orgname.ToLower();
            DestName = destname.ToLower();
        }
    }
}

namespace Owl.Feature.iModule
{


    public abstract class ModuleProvider : Provider
    {
        /// <summary>
        /// 安装模块
        /// </summary>
        public abstract void Install();

        /// <summary>
        /// 改变对象名称
        /// </summary>
        /// <param name="alters"></param>
        public abstract void ChangeName(IEnumerable<ChangeName> alters);

        /// <summary>
        /// 改变角色名称
        /// </summary>
        /// <param name="orgrole"></param>
        /// <param name="destrole"></param>
        /// <param name="namechanged"></param>
        public abstract void ChangeRole(string orgrole, string destrole, bool namechanged);

        /// <summary>
        /// 创建管理员账号
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        public abstract void CreageAdmin(string username, string password);

        /// <summary>
        /// 获取模块数据版本号
        /// </summary>
        /// <returns></returns>
        public abstract IDictionary<string, string> GetVersions();

        /// <summary>
        /// 设置模块的数据版本号
        /// </summary>
        /// <param name="versions"></param>
        public abstract void SetVersions(IDictionary<string, string> versions);
    }
}

