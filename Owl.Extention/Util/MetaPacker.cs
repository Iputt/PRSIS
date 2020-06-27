using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Domain;
using System.Reflection;
using System.IO;
namespace Owl.Util
{
    public class PackData<T>
    {
        public DateTime Version { get; set; }

        public IEnumerable<T> Datas { get; set; }
    }
    /// <summary>
    /// 元数据打包器
    /// </summary>
    public abstract class MetaPacker : Loader<MetaPacker>
    {
        /// <summary>
        /// 元数据的名称
        /// </summary>
        protected abstract string MetaName { get; }

        /// <summary>
        /// 忽略的字段
        /// </summary>
        protected virtual string[] IgnoreFileds { get { return new string[0]; } }

        protected virtual IEnumerable<AggRoot> GetMetas(Assembly asm)
        {
            var models = MetaEngine.GetModels().Where(s => s.State != MetaMode.Custom && s.ModelType.Assembly == asm).Select(s => s.Name);
            return GetMetas(models, asm == null);
        }
        /// <summary>
        /// 获取指定对象的元数据
        /// </summary>
        /// <param name="models"></param>
        /// <param name="custom">是否自定义模块</param>
        /// <returns></returns>
        protected abstract IEnumerable<AggRoot> GetMetas(IEnumerable<string> models, bool custom);

        /// <summary>
        /// 将指定的对象恢复到系统中
        /// </summary>
        /// <param name="dtos"></param>
        protected virtual void Restore(Assembly asm, IEnumerable<TransferObject> dtos, DateTime version)
        {
            var roots = GetMetas(asm).ToDictionary(s => s.Id);
            var dict = dtos.xToDictionary(s => s.GetRealValue<Guid>("Id"));
            var fixids = dict.Keys.Where(s => !roots.ContainsKey(s)).ToArray();
            if (fixids.Length > 0)
            {
                foreach (var root in Repository.FindAll(MetaName, Specification.Create("Id", CmpCode.IN, fixids).ToString()))
                {
                    roots[root.Id] = root;
                }
            }

            foreach (var pair in dict)
            {
                AggRoot root = null;
                var id = pair.Key;
                var dto = pair.Value;
                if (roots.ContainsKey(id))
                {
                    root = roots[id];
                    roots.Remove(id);
                }
                else
                {
                    root = DomainFactory.Create<AggRoot>(MetaName);
                    root.Id = id;
                }
                root.Write(dto);
                root.Push();
            }
            foreach (var root in roots.Values)
            {
                if (root.Created < version)
                    root.Remove();
            }
        }

        /// <summary>
        /// 将指定程序集的元数据打包
        /// </summary>
        /// <param name="asm">程序集</param>
        /// <returns>数据打包后的路径</returns>
        public static string Pack(Assembly asm)
        {
            if (asm == null)
                throw new ArgumentNullException("asm");
            var filepath = FileHelper.GetFilePath(string.Format("{0}.meta.json", asm.GetName().Name.ToLower()));
            var packdata = new PackData<TransferObject>();
            packdata.Version = DateTime.Now;

            //Datas = new LoadedObjs.SelectMany(s => s.GetMetas(asm))
            List<TransferObject> datas = new List<TransferObject>();
            packdata.Datas = datas;
            foreach (var loadobj in LoadedObjs)
            {
                var objs = loadobj.GetMetas(asm).Select(s => s.Read());
                foreach (var ifiled in loadobj.IgnoreFileds)
                {
                    foreach (var obj in objs)
                    {
                        obj.Remove(ifiled);
                    }
                }
                datas.AddRange(objs);
            }
            //var tmp = LoadedObjs.SelectMany(s => s.GetMetas(asm)).ToJson();
            File.WriteAllText(filepath, packdata.ToJson(), Encoding.UTF8);
            return filepath;
        }

        /// <summary>
        /// 从程序集中恢复元数据
        /// </summary>
        /// <param name="asm">程序集</param>
        public static void UnPack(Assembly assembly, string outfile)
        {
            string text = "";
            if (!string.IsNullOrEmpty(outfile) && File.Exists(outfile))
                text = FileHelper.ReadAllText(outfile, Encoding.UTF8);
            else
            {
                var filepath = FileHelper.GetMetaPath(string.Format("{0}.meta.json", assembly.GetName().Name.ToLower()));
                if (File.Exists(filepath))
                {
                    text = FileHelper.ReadAllText(filepath, Encoding.UTF8);
                }
                else if (assembly != null)
                {
                    foreach (var pair in assembly.LoadManifest(filter: s => s.EndsWith(".meta.json")))
                    {
                        text = pair.Value.AsString();
                    }
                }
            }
            var packdata = text.DeJson<PackData<TransferObject>>();
            var dtos = packdata.Datas.GroupBy(s => s.__ModelName__).ToDictionary(s => s.Key, s => s.ToList());
            foreach (var parser in LoadedObjs)
            {
                if (dtos.ContainsKey(parser.MetaName))
                    parser.Restore(assembly, dtos[parser.MetaName], packdata.Version);
            }
        }
    }
}
