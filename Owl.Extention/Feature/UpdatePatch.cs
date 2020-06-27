using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Util;
using System.IO;
using Owl.Domain;
namespace Owl.Feature
{
    public abstract class UpdatePatch : Loader<UpdatePatch>
    {
        static UpdatePatch()
        {
            Register(s =>
            {
                s.AddPatch();

            }, null);
        }

        Dictionary<string, Action<string>> Patchs = new Dictionary<string, Action<string>>();
        protected void RegisterPatch(string version, Action<string> patch)
        {
            Patchs[version] = patch;
        }
        protected abstract void AddPatch();
        static bool Updating;
        static IDictionary<string, string> m_asmversions;
        /// <summary>
        /// 程序集的数据库版本号
        /// </summary>
        protected static IDictionary<string, string> AsmVersions
        {
            get
            {
                if (m_asmversions == null)
                {
                    m_asmversions = new Dictionary<string, string>();
                    var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "asmversion");
                    if (File.Exists(path))
                    {
                        foreach (var asmversion in File.ReadAllLines(path))
                        {
                            if (string.IsNullOrEmpty(asmversion))
                                continue;
                            var tmp = asmversion.Split(' ');
                            m_asmversions[tmp[0]] = tmp[1];
                        }
                        File.Move(path, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "asmversion_old"));
                    }
                    var todayversion = DateTime.Today.ToString("yyyy.MM.dd");
                    foreach (var pair in Module.GetVersions())
                    {
                        if (m_asmversions.ContainsKey(pair.Key) && m_asmversions[pair.Key].GreaterThan(pair.Value))
                        {
                            continue;
                        }
                        m_asmversions[pair.Key] = pair.Value.Coalesce(todayversion);
                    }
                }
                return m_asmversions;
            }
        }

        public static string GetVersion(string asm)
        {
            asm = asm.ToLower();
            if (AsmVersions.ContainsKey(asm))
                return AsmVersions[asm].Split('|')[0];
            return DateTime.Today.ToString("yyyy.MM.dd");
        }

        [OnApplicationGetMC(Ordinal = 0)]
        public static void Update()
        {
            if (Updating)
                return;
            Updating = true;
            var asmversions = AsmVersions;
            foreach (var obj in LoadedObjs)
            {
                var assembly = obj.GetType().Assembly;
                var asmname = assembly.GetName().Name.ToLower();
                if (!asmversions.ContainsKey(asmname))
                    asmversions[asmname] = DateTime.Today.ToString("yyyy.MM.dd");

                var versions = asmversions[asmname].Split('|');
                var lversion = versions.Length > 1 ? versions[1] : versions[0];//上次执行中断版本号
                foreach (var patch in obj.Patchs.Where(s => s.Key.GreaterThan(lversion)).OrderBy(s => s.Key))
                {
                    try
                    {
                        using (var trans = DomainContext.StartTransaction())
                        {
                            DomainContext.Current.Host(() => patch.Value(versions[0]));
                            trans.Commit();
                        }
                        lversion = patch.Key;
                        asmversions[asmname] = patch.Key;
                    }
                    catch (Exception ex)
                    {
                        asmversions[asmname] = string.Format("{0}|{1}", versions[0], lversion);
                        break;
                    }
                }
            }
            Module.SetVersions(asmversions);

        }
    }
}
