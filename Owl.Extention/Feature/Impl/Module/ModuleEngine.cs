using System;
using System.Reflection;
using Owl.Domain;
using Owl.Util;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Collections;
namespace Owl.Feature.iModule
{
    /// <summary>
    /// 模块引擎
    /// </summary>
    public class ModuleEngine : Engine<ModuleProvider, ModuleEngine>
    {
        protected override bool SkipException
        {
            get
            {
                return false;
            }
        }
        /// <summary>
        /// 系统初始化
        /// </summary>
        /// <returns>是否重装成功</returns>
        public static bool Install()
        {
            var ok = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "install.ok");
            if (File.Exists(ok))
                return false;
            CreateSchemas(false);
            Execute(s => s.Install);
            FileHelper.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "install.ok"), "1");
            return true;
        }

        /// <summary>
        /// 创建数据架构
        /// </summary>
        /// <param name="assembly">程序集</param>
        /// <param name="force">是否强制重建</param>
        internal static void CreateSchema(Assembly assembly, bool force = false)
        {
            using (UnitofworkScope scop = new UnitofworkScope())
            {
                foreach (var type in TypeHelper.LoadTypeFromAsm<AggRoot>(assembly))
                {
                    Repository.CreateSchema(ModelMetadataEngine.GetModel(type), force);
                }
                scop.Complete();
            }
        }

        /// <summary>
        /// 重建制定名称的模块的数据库结构
        /// </summary>
        /// <param name="asmname"></param>
        /// <param name="force"></param>
        internal static void CreateSchema(string asmname, bool force = false)
        {
            var asm = AsmHelper.GetAssembly(asmname);
            if (asm != null)
                CreateSchema(asm, force);
        }

        /// <summary>
        /// 重建所有数据库结构
        /// </summary>
        internal static void CreateSchemas(bool force = false)
        {
            foreach (var asm in AsmHelper.GetAssemblies())
                CreateSchema(asm, force);
        }

        /// <summary>
        /// 更改对象名称
        /// </summary>
        /// <param name="orgmodel"></param>
        /// <param name="destmodel"></param>
        public static void ChangeName(string orgmodel, string destmodel)
        {
            if (string.IsNullOrEmpty(orgmodel) || string.IsNullOrEmpty(destmodel))
                return;
            Execute(s => s.ChangeName, new List<ChangeName>() { new ChangeName(orgmodel, destmodel) });
        }

        public static void ChangeName(params ChangeName[] alters)
        {
            Execute(s => s.ChangeName, alters);
        }

        /// <summary>
        /// 变更角色名称
        /// </summary>
        /// <param name="orgrole">原始名称</param>
        /// <param name="destrole">目标名称</param>
        public static void ChangeRole(string orgrole, string destrole, bool namechanged)
        {
            if (string.IsNullOrEmpty(orgrole) || string.IsNullOrEmpty(destrole))
                return;
            Execute(s => s.ChangeRole, orgrole, destrole, namechanged);
        }

        public static void CreateAdmin()
        {
            Execute(s => s.CreageAdmin, "admin", "admin");
        }

        public static IDictionary<string, string> GetVersions()
        {
            return Execute2<IDictionary<string, string>>(s => s.GetVersions);
        }

        public static void SetVersions(IDictionary<string, string> versions)
        {
            Execute(s => s.SetVersions, versions);
        }
    }
}

