using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Domain;
using Owl.Util;
using System.IO;
namespace Owl.Domain
{
    /// <summary>
    /// 不进行备份恢复
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class IgnoreBakAttribute : Attribute
    {

    }
    /// <summary>
    /// 进行系统配置的对象
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class SysConfigAttribute : Attribute
    {

    }
}
namespace Owl.Feature
{
    /// <summary>
    /// 数据维护
    /// </summary>
    public static class Maintenance
    {
        static IEnumerable<DomainModel> GetMetas(string[] models)
        {
            List<DomainModel> metas = new List<DomainModel>();
            if (models.Length == 0)
            {
                metas.AddRange(MetaEngine.GetModels().Where(s =>
                    s.ObjType == DomainType.AggRoot &&
                    !s.ModelType.IsAbstract &&
                    (s.Attrs != null && s.Attrs.OfType<IgnoreBakAttribute>().Count() == 0) &&
                    s.Name != typeof(SmartRoot).MetaName())
                );
            }
            else
            {
                foreach (var model in models)
                {
                    var meta = MetaEngine.GetModel(model);
                    if (meta != null)
                        metas.Add(meta);
                }
            }
            return metas;
        }

        static IEnumerable<DomainModel> GetConfigMetas()
        {
            List<DomainModel> metas = new List<DomainModel>();

            metas.AddRange(MetaEngine.GetModels().Where(s =>
                s.ObjType == DomainType.AggRoot &&
                !s.ModelType.IsAbstract &&
                (s.Attrs != null && s.Attrs.OfType<SysConfigAttribute>().Count() == 1) &&
                s.Name != typeof(SmartRoot).MetaName())
            );

            return metas;
        }

        static string Backup(DateTime time, IEnumerable<DomainModel> metas)
        {
            var date = time.Date;
            var version = time.ToString("HHmmss");

            foreach (var meta in metas.OrderBy(s => s.Name))
            {
                var start = DateTime.Now;
                string status = "";
                try
                {
                    Repository.Backup(new ModelMetadata(meta), date, version);
                    status = "成功";
                }
                catch (Exception ex)
                {
                    status = string.Format("失败,原因 {0}", ex.Message);
                }
                MsgContext.Current.AppendInfo("", "{0}:备份 {1} {2}\r\n", start, meta.Name, status);
            }
            MsgContext.Current.AppendInfo("", "{0}:备份结束\r\n", DateTime.Now);
            return version;
        }

        /// <summary>
        /// 备份数据
        /// </summary>
        /// <param name="date">备份时间</param>
        /// <param name="models">备份对象</param>
        /// <returns>备份的版本号</returns>
        public static string Backup(DateTime time, params string[] models)
        {
            models = models.Where(s => s != null).ToArray();
            var metas = GetMetas(models);
            return Backup(time, metas);
        }

        public static string BakConfig(DateTime time)
        {
            return Backup(time, GetConfigMetas());
        }
        /// <summary>
        /// 还原数据
        /// </summary>
        /// <param name="date">备份日期</param>
        /// <param name="version">版本号</param>
        /// <param name="models">还原对象</param>
        public static void Restore(DateTime date, string version, params string[] models)
        {
            models = models.Where(s => s != null).ToArray();
            var metas = GetMetas(models);
            List<string> result = new List<string>();
            foreach (var meta in metas.OrderBy(s => s.Name))
            {
                var start = DateTime.Now;
                var status = "";
                try
                {
                    Repository.Restore(new ModelMetadata(meta), date, version);
                    status = "成功";
                }
                catch (Exception ex)
                {
                    status = string.Format("失败,原因 {0}", ex.Message);
                }
                MsgContext.Current.AppendInfo("", "{0}:恢复 {1} {2}\r\n", start, meta.Name, status);
            }
            MsgContext.Current.AppendInfo("", "{0}:恢复完成\r\n", DateTime.Now);
        }
        /// <summary>
        /// 恢复最近一次备份
        /// </summary>
        /// <param name="models"></param>
        /// <returns></returns>
        public static void RestoreLatest(params string[] models)
        {
            var backpath = Path.Combine(AppConfig.Section.ResPath, "Bak");
            if (!Directory.Exists(backpath))
                return;
            var data = "";
            var version = "";
            data = Directory.EnumerateDirectories(backpath).OrderByDescending(s => s).FirstOrDefault();

            if (!string.IsNullOrEmpty(data))
                version = Directory.EnumerateDirectories(data).OrderByDescending(s => s).FirstOrDefault();
            if (!string.IsNullOrEmpty(data) && !string.IsNullOrEmpty(version))
                Restore(DateTime.Parse(data.Substring(backpath.Length + 1)), version.Substring(data.Length + 1), models);
        }
    }
}
