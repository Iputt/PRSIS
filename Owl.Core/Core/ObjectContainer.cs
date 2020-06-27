using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Owl.Util;
namespace Owl
{
    public class TypeConf
    {
        public string Name { get; private set; }

        public bool IsGeneric { get; private set; }

        List<TypeConf> m_genric = new List<TypeConf>();

        public IEnumerable<TypeConf> Generic { get { return m_genric; } }

        static Regex regex = new Regex(@"([\w,\.]*)(\[(.*)\])?");
        public static TypeConf Parse(string name)
        {
            var match = regex.Match(name);
            if (!match.Success)
                return null;
            TypeConf type = new TypeConf();
            type.Name = match.Groups[1].Value;
            if (!string.IsNullOrEmpty(match.Groups[2].Value))
            {
                type.IsGeneric = true;
                type.Name = string.Format("{0}`1", type.Name);
            }
            if (!string.IsNullOrEmpty(match.Groups[3].Value))
            {
                var gen = match.Groups[3].Value;
                var tmp = gen.Split(',');
                type.Name = string.Format("{0}`{1}", match.Groups[1].Value, tmp.Length);
                foreach (var n in tmp)
                {
                    var t = Parse(n);
                    if (t != null)
                        type.m_genric.Add(t);
                }
            }
            return type;
        }
    }
    /// <summary>
    /// 对象解析器
    /// </summary>
    public class ObjectContainer
    {
        Dictionary<int, Type> m_registers = new Dictionary<int, Type>(AppConfig.Section.GetMaps().Count());
        Dictionary<string, Type> types = new Dictionary<string, Type>();

        int GetHash(Type type, string name)
        {
            if (string.IsNullOrEmpty(name))
                return type.GetHashCode();
            return type.GetHashCode() + name.ToLower().GetHashCode();
        }
        Type gettype(TypeConf typeconf)
        {
            Type type = null;
            foreach (var name in AppConfig.Section.Namespaces)
            {
                var typename = string.Format("{0}.{1}", name, typeconf.Name);
                if (types.ContainsKey(typename))
                {
                    type = types[typename];
                    break;
                }
            }
            if (typeconf.IsGeneric && typeconf.Generic.Count() > 0)
            {
                var gen = typeconf.Generic.Select(t => gettype(t));
                type = type.MakeGenericType(gen.ToArray());
            }
            return type;
        }

        private ObjectContainer()
        {
            foreach (var assem in AsmHelper.GetAssemblies())
            {
                foreach (var type in assem.GetTypes())
                {
                    types[type.FullName] = type;
                }
            }
            var maps = AppConfig.Section.GetMaps();
            foreach (var map in maps)
            {
                var org = gettype(TypeConf.Parse(map.Type));
                var dest = gettype(TypeConf.Parse(map.MapTo));
                if (org != null && dest != null)
                {
                    m_registers[GetHash(org, map.Name)] = dest;
                }
            }
            types.Clear();
        }

        public static ObjectContainer Instance = new ObjectContainer();

        Dictionary<Type, Dictionary<Type, string>> m_genericmapname = new Dictionary<Type, Dictionary<Type, string>>();
        /// <summary>
        /// 为泛型类型解析注册一个名字，用于区分泛型参数的基类及派生类的类型解析
        /// </summary>
        /// <param name="org">映射的泛型类型</param>
        /// <param name="basetype">泛型参数的基类</param>
        /// <param name="name">映射的名称</param>
        public void RegisterGenericResolveMap(Type org, Type basetype, string name)
        {
            if (!org.IsGenericType)
                return;
            if (!m_genericmapname.ContainsKey(org))
                m_genericmapname[org] = new Dictionary<Type, string>();
            m_genericmapname[org][basetype] = name;
        }
        /// <summary>
        /// 从字典中获取解析名称
        /// </summary>
        /// <param name="gptype"></param>
        /// <param name="dict"></param>
        /// <returns></returns>
        string _GetGenericResolveMap(Type gptype, Dictionary<Type, string> dict)
        {
            if (gptype == null)
                return "";
            if (dict.ContainsKey(gptype))
                return dict[gptype];
            return _GetGenericResolveMap(gptype.BaseType, dict);
        }

        public string GetGenericResolveMap(Type org, Type gptype)
        {
            if (m_genericmapname.ContainsKey(org))
            {
                return _GetGenericResolveMap(gptype, m_genericmapname[org]);
            }
            return "";
        }
        public void Register(Type org, Type dest, string name = "")
        {
            m_registers[GetHash(org, name)] = dest;
        }
        /// <summary>
        /// 解析类型
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <param name="throwisnull"></param>
        /// <returns></returns>
        public Type ResolveType<T>(string name = "", bool throwisnull = true)
        {
            return ResoleType(typeof(T), name, throwisnull);
        }
        /// <summary>
        /// 解析类型
        /// </summary>
        /// <param name="type"></param>
        /// <param name="name"></param>
        /// <param name="throwisnull"></param>
        /// <returns></returns>
        public Type ResoleType(Type type, string name = "", bool throwisnull = true)
        {
            Type target = null;
            var thash = GetHash(type, name);
            if (m_registers.ContainsKey(thash))
                target = m_registers[thash];
            if (target == null && type.IsGenericType)
            {
                if (string.IsNullOrEmpty(name))
                    name = GetGenericResolveMap(type.GetGenericTypeDefinition(), type.GetGenericArguments()[0]);
                var tmp = GetHash(type.GetGenericTypeDefinition(), name);
                if (m_registers.ContainsKey(tmp))
                    target = m_registers[tmp];
            }
            if (target == null)
            {
                if (throwisnull)
                    throw new Exception2("对象{0}解析失败,请检查配置文件", type.FullName);
                return null;
            }
            if (target.IsGenericTypeDefinition && type.GetGenericArguments().Length > 0)
            {
                target = target.MakeGenericType(type.GetGenericArguments());
                m_registers[thash] = target;
            }
            return target;
        }

        /// <summary>
        /// 创建类型映射的对象
        /// </summary>
        /// <typeparam name="T">基准类型</typeparam>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public T Resolve<T>(string name = "", bool throwisnull = true)
        {
            return (T)Resolve(typeof(T), name, throwisnull);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public object Resolve(Type type, string name = "", bool throwisnull = true)
        {
            Type target = ResoleType(type, name, throwisnull);
            if (target == null)
                return null;
            return Activator.CreateInstance(target);
        }
    }
}
