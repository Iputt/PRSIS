using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
namespace Owl.Util
{
    /// <summary>
    /// 程序集内置资源
    /// </summary>
    public class AsmManifest
    {
        /// <summary>
        /// 资源名称
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// 内容
        /// </summary>
        public byte[] Buffer { get; private set; }

        public AsmManifest(string name, byte[] buffer)
        {
            Name = name;
            Buffer = buffer;
        }
        /// <summary>
        /// 获取字符串
        /// </summary>
        /// <returns></returns>
        public string AsString()
        {
            string result = "";
            using (var stream = new MemoryStream(Buffer))
            {
                using (var reader = new StreamReader(stream))
                    result = reader.ReadToEnd();
            }
            return result;
        }
        /// <summary>
        /// 获取流
        /// </summary>
        /// <returns></returns>
        public Stream AsStream()
        {
            return new MemoryStream(Buffer);
        }

    }
    /// <summary>
    /// 程序集加载
    /// </summary>
    public class AsmLoadArgs : EventArgs
    {
        /// <summary>
        /// 程序集名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 程序集
        /// </summary>
        public Assembly Assembly { get; set; }

        public AsmLoadArgs(string name, Assembly assembly)
        {
            Name = name;
            Assembly = assembly;
        }
    }
    /// <summary>
    /// 程序集管理器
    /// </summary>
    public static class AsmHelper
    {
        static Dictionary<string, Assembly> m_asseblies = new Dictionary<string, Assembly>(20);
        static Dictionary<Assembly, DateTime> m_assemblymodifies = new Dictionary<Assembly, DateTime>(20);
        static bool isModule(string path)
        {
            if (!path.EndsWith(".dll"))
                return false;
            var filename = Path.GetFileNameWithoutExtension(path);
            if (filename.StartsWith("Owl") || filename.Contains(".Module.") || filename.Contains(".Plugin."))
                return true;
            return false;
        }

        public static string GetBinPath()
        {
            var binpath = AppDomain.CurrentDomain.SetupInformation.PrivateBinPath;
            if (binpath == null)
                binpath = AppDomain.CurrentDomain.BaseDirectory;
            return binpath;
        }
        static bool isValidAsm(Assembly asm, string baseasm, Dictionary<string, Assembly> allasms)
        {
            if (asm.GetName().Name == baseasm)
                return true;
            var refasms = asm.GetReferencedAssemblies();
            if (refasms.Any(s => s.Name == baseasm))
                return true;
            foreach (var refasm in refasms)
            {
                if (allasms.ContainsKey(refasm.Name))
                    return isValidAsm(allasms[refasm.Name], baseasm, allasms);
            }
            return false;
        }
        static AsmHelper()
        {
            List<Assembly> assemblies = new List<Assembly>();
            var currentasm = Assembly.GetExecutingAssembly().GetName().Name;

            List<string> files = new List<string>();
            var binpath = GetBinPath();
            files.AddRange(Directory.EnumerateFiles(binpath).Where(s => isModule(s)));
            var moduledir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Modules");
            if (Directory.Exists(moduledir) && System.IO.File.Exists(Path.Combine(moduledir, "ok")))
                files.AddRange(Directory.EnumerateFiles(moduledir).Where(s => s.EndsWith(".dll")));

            foreach (var asm in AppConfig.Section.Assemblies)
                assemblies.Add(AppDomain.CurrentDomain.Load(asm));

            foreach (var file in files)
            {
                Assembly asm = null;
                try
                {
                    var filename = Path.GetFileNameWithoutExtension(file);
                    asm = AppDomain.CurrentDomain.Load(filename);
                }
                catch
                {
                    asm = Assembly.LoadFile(file);
                }
                m_assemblymodifies[asm] = File.GetLastWriteTime(file);
                //if (asm.GetName().Name == currentasm || asm.GetReferencedAssemblies().Any(s => s.Name == currentasm))
                //    assemblies.Add(asm);
            }
            var asmdict = m_assemblymodifies.Keys.ToDictionary(s => s.GetName().Name);
            assemblies.AddRange(m_assemblymodifies.Keys.Where(s => isValidAsm(s, currentasm, asmdict)));
            lock (m_asseblies)
            {
                foreach (var assem in assemblies)
                {
                    m_asseblies[assem.FullName] = assem;
                }
            }
        }

        static event EventHandler<AsmLoadArgs> onLoad;
        static event EventHandler<AsmLoadArgs> onUnload;

        /// <summary>
        /// 注册管理器
        /// </summary>
        /// <param name="onload">程序集加载时</param>
        /// <param name="onunload">程序集卸载时</param>
        public static void RegisterResource(Action<string, Assembly> onload, Action<string, Assembly> onunload)
        {
            if (onload == null || onunload == null)
                return;
            onLoad += new EventHandler<AsmLoadArgs>((obj, args) => onload(args.Name, args.Assembly));
            onUnload += new EventHandler<AsmLoadArgs>((obj, args) => onunload(args.Name, args.Assembly));
            Dictionary<string, Assembly> asms = new Dictionary<string, Assembly>();
            lock (m_asseblies)
            {
                foreach (var pair in m_asseblies)
                {
                    asms[pair.Key] = pair.Value;
                }
            }
            foreach (var pair in asms)
                onload(pair.Key, pair.Value);
        }
        /// <summary>
        /// 添加程序集
        /// </summary>
        /// <param name="assembly"></param>
        public static void AddAssembly(Assembly assembly)
        {
            var name = assembly.FullName;
            lock (m_asseblies)
            {
                if (m_asseblies.ContainsKey(name))
                    return;
                m_asseblies[name] = assembly;
            }
            if (onLoad != null)
                onLoad(null, new AsmLoadArgs(name, assembly));
        }
        /// <summary>
        /// 卸载程序集
        /// </summary>
        /// <param name="name"></param>
        public static void RemoveAssembly(string name)
        {
            lock (m_asseblies)
            {
                if (!m_asseblies.ContainsKey(name))
                    return;
                var assem = m_asseblies[name];
                if (onUnload != null)
                    onUnload(null, new AsmLoadArgs(name, assem));
                m_asseblies.Remove(name);
            }
        }

        /// <summary>
        /// 获取程序集
        /// </summary>
        /// <param name="name">程序集全名或部分名</param>
        /// <returns></returns>
        public static Assembly GetAssembly(string name)
        {
            if (m_asseblies.ContainsKey(name))
                return m_asseblies[name];
            return m_asseblies.Values.FirstOrDefault(s => s.GetName().Name == name);
        }
        /// <summary>
        /// 获取程序集的创建时间
        /// </summary>
        /// <param name="asm"></param>
        /// <returns></returns>
        public static DateTime? GetAssemblyModify(Assembly asm)
        {
            if (m_assemblymodifies.ContainsKey(asm))
                return m_assemblymodifies[asm].Precision(TimePrecision.Second);
            return null;
        }

        /// <summary>
        /// 获取所有程序集
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<Assembly> GetAssemblies()
        {
            return m_asseblies.Values;
        }

        /// <summary>
        /// 加载程序集的嵌入式资源
        /// </summary>
        /// <param name="assem">程序集</param>
        /// <param name="prefix">资源命名空间等前缀</param>
        /// <param name="filter">资源过滤器</param>
        /// <returns></returns>
        public static Dictionary<string, AsmManifest> LoadManifest(this Assembly assem, string prefix = null, Func<string, bool> filter = null)
        {
            var names = assem.GetManifestResourceNames().AsEnumerable();
            if (!string.IsNullOrEmpty(prefix))
            {
                if (!prefix.EndsWith("."))
                    prefix += ".";
                names = names.Where(s => s.StartsWith(prefix));
            }

            if (filter != null)
                names = names.Where(filter);
            Dictionary<string, AsmManifest> contents = new Dictionary<string, AsmManifest>();
            foreach (var name in names)
            {
                var tname = string.IsNullOrEmpty(prefix) ? name : name.Substring(prefix.Length);
                using (var stream = assem.GetManifestResourceStream(name))
                {
                    byte[] buffer = new byte[stream.Length];
                    stream.Read(buffer, 0, buffer.Length);
                    contents[tname] = new AsmManifest(tname, buffer);
                }
            }
            return contents;
        }

        /// <summary>
        /// 加载调用本方法的程序集的嵌入式资源
        /// </summary>
        /// <param name="prefix">资源命名空间等前缀</param>
        /// <param name="filter">资源过滤器</param>
        /// <returns></returns>
        public static Dictionary<string, AsmManifest> LoadManifest(string prefix, Func<string, bool> filter = null)
        {
            var assem = Assembly.GetCallingAssembly();
            return assem.LoadManifest(prefix, filter);
        }
    }

    public class ObjectLoaderEventArgs : EventArgs
    {
        public object Object { get; private set; }

        public ObjectLoaderEventArgs(object obj)
        {
            Object = obj;
        }
    }

    /// <summary>
    /// 程序集中对象加载器
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    public class Loader<T>
        where T : class
    {
        static event EventHandler<ObjectLoaderEventArgs> OnLoad;
        static event EventHandler<ObjectLoaderEventArgs> OnUnLoad;
        public static void Register(Action<T> load, Action<T> unload)
        {
            if (load != null)
            {
                OnLoad += new EventHandler<ObjectLoaderEventArgs>((obj, args) => load((T)args.Object));
                foreach (var obj in LoadedObjs)
                    load(obj);
            }
            if (unload != null)
                OnUnLoad += new EventHandler<ObjectLoaderEventArgs>((obj, args) => unload((T)args.Object));
        }

        static List<T> objs = new List<T>();
        /// <summary>
        /// 已加载对象
        /// </summary>
        public static IEnumerable<T> LoadedObjs
        {
            get { return objs.AsReadOnly(); }
        }

        static Loader()
        {
            AsmHelper.RegisterResource(LoadAsm, UnloadAsm);
        }
        static void LoadAsm(string asmname, Assembly asm)
        {
            foreach (var obj in TypeHelper.LoadFromAsm<T>(asm))
            {
                objs.Add(obj);
                if (OnLoad != null)
                    OnLoad(null, new ObjectLoaderEventArgs(obj));
            }
        }
        static void UnloadAsm(string asmname, Assembly asm)
        {
            foreach (var obj in objs.ToList())
            {
                if (obj.GetType().Assembly == asm)
                {
                    objs.Remove(obj);
                    if (OnUnLoad != null)
                        OnUnLoad(null, new ObjectLoaderEventArgs(obj));
                }
            }
        }
    }

    /// <summary>
    /// 程序集中顺序对象加载器
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class SortedLoader<T>
        where T : SortedLoader<T>
    {
        /// <summary>
        /// 序号，数字越大优先级越高
        /// </summary>
        protected abstract int Priority { get; }

        static event EventHandler<ObjectLoaderEventArgs> OnLoad;
        static event EventHandler<ObjectLoaderEventArgs> OnUnLoad;
        public static void Register(Action<T> load, Action<T> unload)
        {
            if (load != null)
            {
                OnLoad += new EventHandler<ObjectLoaderEventArgs>((obj, args) => load((T)args.Object));
                foreach (var obj in LoadedObjs)
                    load(obj);
            }
            if (unload != null)
                OnUnLoad += new EventHandler<ObjectLoaderEventArgs>((obj, args) => unload((T)args.Object));
        }

        static SortedSet<T> objs = new SortedSet<T>(Comparer2<T>.Desc(s => s.Priority));
        /// <summary>
        /// 已加载对象
        /// </summary>
        public static IEnumerable<T> LoadedObjs
        {
            get { return objs.AsEnumerable(); }
        }

        static SortedLoader()
        {
            AsmHelper.RegisterResource(LoadAsm, UnloadAsm);
        }
        static void LoadAsm(string asmname, Assembly asm)
        {
            foreach (var obj in TypeHelper.LoadFromAsm<T>(asm))
            {
                objs.Add(obj);
                if (OnLoad != null)
                    OnLoad(null, new ObjectLoaderEventArgs(obj));
            }
        }
        static void UnloadAsm(string asmname, Assembly asm)
        {
            foreach (var obj in objs.ToList())
            {
                if (obj.GetType().Assembly == asm)
                {
                    objs.Remove(obj);
                    if (OnUnLoad != null)
                        OnUnLoad(null, new ObjectLoaderEventArgs(obj));
                }
            }
        }
    }

    /// <summary>
    /// 对象加载器，结果为字典, 相同Key的，取优先级高的
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class DictLoader<T>
        where T : DictLoader<T>
    {
        /// <summary>
        /// 序号，数字越大优先级越高
        /// </summary>
        protected abstract int Priority { get; }

        protected static Func<T, string> KeySelector { get; set; }

        static Dictionary<string, T> objs = new Dictionary<string, T>();
        /// <summary>
        /// 已加载对象
        /// </summary>
        public static Dictionary<string, T> LoadedDict
        {
            get { return objs; }
        }

        static DictLoader()
        {
            AsmHelper.RegisterResource(LoadAsm, UnloadAsm);
        }
        static void LoadAsm(string asmname, Assembly asm)
        {
            foreach (var obj in TypeHelper.LoadFromAsm<T>(asm))
            {
                var key = KeySelector(obj);
                if (objs.ContainsKey(key) && objs[key].Priority > obj.Priority)
                    continue;
                objs[key] = obj;

            }
        }
        static void UnloadAsm(string asmname, Assembly asm)
        {
            foreach (var obj in objs.Values.ToList())
            {
                if (obj.GetType().Assembly == asm)
                {
                    objs.Remove(KeySelector(obj));
                }
            }
        }
    }
    public class TypeLoaderEventArgs : EventArgs
    {
        public Type Type { get; private set; }

        public TypeLoaderEventArgs(Type type)
        {
            Type = type;
        }
    }

    /// <summary>
    /// 类型加载器
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class TypeLoader<T>
        where T : class
    {
        static List<Type> types = new List<Type>();
        /// <summary>
        /// 已加载对象
        /// </summary>
        public static IEnumerable<Type> Types
        {
            get { return types.AsReadOnly(); }
        }

        static TypeLoader()
        {
            AsmHelper.RegisterResource(LoadAsm, UnloadAsm);
        }

        static void LoadAsm(string asmname, Assembly asm)
        {
            foreach (var type in TypeHelper.LoadTypeFromAsm<T>(asm))
            {
                types.Add(type);
                if (OnLoad != null)
                    OnLoad(null, new TypeLoaderEventArgs(type));
            }
        }
        static void UnloadAsm(string asmname, Assembly asm)
        {
            foreach (var type in types.ToList())
            {
                if (type.Assembly == asm)
                {
                    types.Remove(type);
                    if (OnUnLoad != null)
                        OnUnLoad(null, new TypeLoaderEventArgs(type));
                }
            }
        }

        static event EventHandler<TypeLoaderEventArgs> OnLoad;
        static event EventHandler<TypeLoaderEventArgs> OnUnLoad;
        public static void Register(Action<Type> load, Action<Type> unload)
        {
            if (load != null)
            {
                OnLoad += new EventHandler<TypeLoaderEventArgs>((obj, args) => load(args.Type));
                foreach (var type in Types)
                    load(type);
            }
            if (unload != null)
                OnUnLoad += new EventHandler<TypeLoaderEventArgs>((obj, args) => unload(args.Type));
        }
    }

    /// <summary>
    /// 程序集中指定类型加载器
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class TypeLoader<T, TLoader>
        where T : class
        where TLoader : new()
    {
        public static readonly TLoader Instance = new TLoader();

        public TypeLoader()
        {
            TypeLoader<T>.Register(OnTypeLoad, OnTypeUnLoad);
        }

        protected IEnumerable<Type> Types { get { return TypeLoader<T>.Types; } }
        protected virtual void OnTypeLoad(Type type) { }
        protected virtual void OnTypeUnLoad(Type type) { }
    }
}
