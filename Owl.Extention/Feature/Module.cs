using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Owl.Domain;
using Owl.Feature.iModule;
namespace Owl.Feature
{
    /// <summary>
    /// 模块管理
    /// </summary>
    public class Module
    {
        /// <summary>
        /// 系统初始化
        /// </summary>
        /// <returns>是否重装成功</returns>
        public static bool Install()
        {
            return ModuleEngine.Install();
        }

        /// <summary>
        /// 创建数据架构
        /// </summary>
        /// <param name="assembly">程序集</param>
        public static void CreateSchema(Assembly assembly)
        {
            ModuleEngine.CreateSchema(assembly, false);
        }

        /// <summary>
        /// 重建制定名称的模块的数据库结构
        /// </summary>
        /// <param name="asmname"></param>
        public static void CreateSchema(string asmname)
        {
            ModuleEngine.CreateSchema(asmname);
        }

        /// <summary>
        /// 重建所有数据库结构
        /// </summary>
        public static void CreateSchemas()
        {
            ModuleEngine.CreateSchemas(false);
        }

        /// <summary>
        /// 更改对象名称
        /// </summary>
        /// <param name="orgmodel"></param>
        /// <param name="destmodel"></param>
        public static void ChangeName(string orgmodel, string destmodel)
        {
            ModuleEngine.ChangeName(orgmodel, destmodel);
        }

        public static void ChangeName(params ChangeName[] alters)
        {
            ModuleEngine.ChangeName(alters);
        }

        /// <summary>
        /// 变更资源的角色归属
        /// </summary>
        /// <param name="orgrole">原始名称</param>
        /// <param name="destrole">目标名称</param>
        ///<param name="namechanged">角色名称是否发生变更，如发生变更则改变用户和职位的角色名称</param>
        public static void ChangeRole(string orgrole, string destrole, bool namechanged = false)
        {
            ModuleEngine.ChangeRole(orgrole, destrole, namechanged);
        }

        public static void CreateAdmin()
        {
            ModuleEngine.CreateAdmin();
        }
        /// <summary>
        /// 获取所有模块的数据库版本
        /// </summary>
        /// <returns></returns>
        public static IDictionary<string,string> GetVersions()
        {
            return ModuleEngine.GetVersions();
        }
        /// <summary>
        /// 设置所有模块的数据库版本
        /// </summary>
        public static void SetVersions(IDictionary<string,string> versions)
        {
            ModuleEngine.SetVersions(versions);
        }
    }
}
