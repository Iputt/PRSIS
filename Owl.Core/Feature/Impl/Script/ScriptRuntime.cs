using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Owl.Util;
using Owl.Domain;
using Owl.Feature;

namespace Owl.Feature.iScript
{
    /// <summary>
    /// 脚本运行时api
    /// </summary>
    public abstract class ScriptRuntimeApi : DictLoader<ScriptRuntimeApi>
    {
        /// <summary>
        /// Api名称
        /// </summary>
        public abstract string Name { get; }

        static ScriptRuntimeApi()
        {
            KeySelector = s => s.Name;
        }
    }

    /// <summary>
    /// 脚本运行时，根据对象名称 构建对象、执行静态方法等，适用所有类型的脚本
    /// </summary>
    public class ScriptRuntime
    {
        public class ScriptContext
        {
            Dictionary<string, List<string>> m_Namespaces;
            /// <summary>
            /// 命名空间集合
            /// </summary>
            public Dictionary<string, List<string>> Namespaces
            {
                get
                {
                    if (m_Namespaces == null)
                    {
                        m_Namespaces = new Dictionary<string, List<string>>();
                        m_Namespaces["System"] = new List<string>() { "", "Owl.Core" };
                        m_Namespaces["Owl.Domain"] = new List<string>() { "Owl.Core", "Owl.Extention" };
                        m_Namespaces["Owl.Util"] = new List<string>() { "Owl.Core", "Owl.Extention" };
                        m_Namespaces["Owl.PL"] = new List<string>() { "Owl.PL" };
                        m_Namespaces["Owl.PL.Web"] = new List<string>() { "Owl.PL.Web" };
                        m_Namespaces["Owl"] = new List<string>() { "Owl.Core" };
                    }
                    return m_Namespaces;
                }
            }

            protected TransferObject m_Parameters;

            /// <summary>
            /// 本次执行上下文参数表
            /// </summary>
            public TransferObject Param
            {
                get
                {
                    if (m_Parameters == null)
                        m_Parameters = new TransferObject();
                    return m_Parameters;
                }
            }
            /// <summary>
            /// 当前上下文
            /// </summary>
            public static ScriptContext Current
            {
                get { return Cache.Thread<ScriptContext>("owl.feature.scriptfactory.context.current", () => new ScriptContext()); }
                set { Cache.Thread("owl.feature.scriptfactory.context.current", value); }
            }
        }

        private ScriptRuntime()
        {

        }

        /// <summary>
        /// 脚本执行上下文
        /// </summary>
        public ScriptContext Ctx { get { return ScriptContext.Current; } }

        #region 当前执行上下文的命名空间管理

        /// <summary>
        /// 添加命名空间
        /// </summary>
        /// <param name="ns">命名空间</param>
        /// <param name="asms">包含命名空间的程序集</param>
        public void Using(string ns, params string[] asms)
        {
            var m_nsasms = Ctx.Namespaces;
            if (!m_nsasms.ContainsKey(ns))
                m_nsasms[ns] = new List<string>();
            var asmns = m_nsasms[ns];
            foreach (var asm in asms)
            {
                if (!asmns.Contains(asm))
                    asmns.Add(asm);
            }
        }
        #endregion

        #region 获取类型
        Dictionary<string, Type> m_types = new Dictionary<string, Type>();
        private Type _GetType(string fullnamewithasm)
        {
            Type type = null;
            if (!m_types.ContainsKey(fullnamewithasm))
            {
                type = Type.GetType(fullnamewithasm);
                m_types[fullnamewithasm] = type;
            }
            else
                type = m_types[fullnamewithasm];
            return type;
        }


        private Type GetType(string fullname, IEnumerable<string> asms)
        {
            Type type = null;
            foreach (var asm in asms)
            {
                if (string.IsNullOrEmpty(asm))
                {
                    type = _GetType(fullname);
                }
                else
                    type = _GetType(string.Format("{0},{1}", fullname, asm));
                if (type != null)
                    break;
            }
            return type;
        }
        protected Type GetType(string typename)
        {
            if (typename.Contains(","))
                return _GetType(typename);
            Type type = null;
            var m_nsasms = Ctx.Namespaces;
            if (typename.Contains("."))
            {
                var ns = typename.Substring(0, typename.LastIndexOf("."));
                if (m_nsasms.ContainsKey(ns))
                    type = GetType(typename, m_nsasms[ns]);
            }
            else
            {
                foreach (var ns in m_nsasms)
                {
                    type = GetType(string.Format("{0}.{1}", ns.Key, typename), ns.Value);
                    if (type != null)
                        break;
                }
            }
            return type;
        }
        #endregion

        #region 工厂方法
        /// <summary>
        /// 创建对象
        /// </summary>
        /// <param name="typename"></param>
        /// <returns></returns>
        public object New(string typename, params object[] args)
        {
            var type = GetType(typename);
            if (type == null)
                return null;
            return Activator.CreateInstance(GetType(typename), args);
        }
        /// <summary>
        /// 创建领域对象
        /// </summary>
        /// <param name="metaname"></param>
        /// <returns></returns>
        public DomainObject NewX(string metaname)
        {
            return DomainFactory.Create(metaname);
        }

        /// <summary>
        /// 获取指定类型的静态字段的值
        /// </summary>
        /// <param name="memberwithtype">成员名称，格式为 类型名称.成员名称</param>
        /// <returns></returns>
        public object Val(string memberwithtype)
        {
            if (string.IsNullOrEmpty(memberwithtype))
                throw new ArgumentNullException(nameof(memberwithtype));
            var index = memberwithtype.LastIndexOf('.');
            if (index == -1)
                throw new ArgumentException("成员名称必须为 类型名称.成员名称");
            var type = memberwithtype.Substring(0, index);
            var member = memberwithtype.Substring(index + 1);
            var info = TypeHelper.GetStaticMember(GetType(type), member);
            if (info != null)
            {
                if (info.MemberType == MemberTypes.Property)
                {
                    var getmethod = (info as PropertyInfo).GetGetMethod();
                    if (getmethod != null)
                        return getmethod.FaseInvoke(null);
                }
                else if (info.MemberType == MemberTypes.Field)
                {
                    return (info as FieldInfo).GetValue(null);
                }
            }
            return null;
        }
        /// <summary>
        /// 设置置顶类型的静态字段的值
        /// </summary>
        /// <param name="memberwithtype">成员名称，格式为 类型名称.成员名称</param>
        /// <param name="value"></param>
        public void Val(string memberwithtype, object value)
        {
            if (string.IsNullOrEmpty(memberwithtype))
                throw new ArgumentNullException(nameof(memberwithtype));
            var index = memberwithtype.LastIndexOf('.');
            if (index == -1)
                throw new ArgumentException("成员名称必须为 类型名称.成员名称");
            var type = memberwithtype.Substring(0, index);
            var member = memberwithtype.Substring(index + 1);
            var info = TypeHelper.GetStaticMember(GetType(type), member);
            if (info != null)
            {
                if (info.MemberType == MemberTypes.Property)
                {
                    var setmethod = (info as PropertyInfo).GetSetMethod();
                    if (setmethod != null)
                        setmethod.FaseInvoke(null, value);
                }
                else if (info.MemberType == MemberTypes.Field)
                {
                    (info as FieldInfo).SetValue(null, value);
                }
            }
        }
        /// <summary>
        /// 执行指定类型的静态方法
        /// </summary>
        /// <param name="type"></param>
        /// <param name="method"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public object Static(string memberwithtype, params object[] args)
        {
            if (string.IsNullOrEmpty(memberwithtype))
                throw new ArgumentNullException(nameof(memberwithtype));
            var index = memberwithtype.LastIndexOf('.');
            if (index == -1)
                throw new ArgumentException("成员名称必须为 类型名称.成员名称");
            var type = memberwithtype.Substring(0, index);
            var method = memberwithtype.Substring(index + 1);

            var methods = TypeHelper.GetStaticMethods(GetType(type), method);
            if (methods != null)
            {
                foreach (var info in methods)
                {
                    var parameters = info.GetParameters();
                    if (parameters.Length == args.Length)
                    {
                        try
                        {
                            for (var i = 0; i < parameters.Length; i++)
                            {
                                args[i] = Convert2.ChangeType(args[i], parameters[i].ParameterType);
                            }
                            return info.FaseInvoke(null, args);
                        }
                        catch (Exception ex)
                        {

                        }
                    }
                }
            }
            return null;
        }

        public object Static(string memberwithtype)
        {
            return Static(memberwithtype, new object[0]);
        }

        /// <summary>
        /// 类型转换
        /// </summary>
        /// <param name="typefullname"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public object Convert(string typefullname, object value)
        {
            return Convert2.ChangeType(value, GetType(typefullname));
        }

        /// <summary>
        /// 对象转为字符串
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        public string ToString(object obj, string format)
        {
            if (string.IsNullOrEmpty(format))
                format = "{0}";
            else
                format = "{0:" + format + "}";
            if (obj is DateTime)
            {
                obj = ((DateTime)obj).ToLocalTime();
            }
            return string.Format(format, obj);
        }

        /// <summary>
        /// 将指定字符串中的格式项替换为指定数组中相应对象的字符串表示形式。
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public string Format(string format, object[] args)
        {
            for (var i = 0; i < args.Length; i++)
            {
                var obj = args[i];
                if (obj is DateTime)
                    args[i] = ((DateTime)obj).ToLocalTime();
            }
            return string.Format(format, args);
        }

        public void Print(string format)
        {
            Console.WriteLine(format);
        }

        /// <summary>
        /// 用于调试
        /// </summary>
        /// <param name="obj"></param>
        public void Log(object obj)
        {

        }

        #endregion

        /// <summary>
        /// 等待一段时间
        /// </summary>
        /// <param name="millisecondsTimeout"></param>
        public void Sleep(int millisecondsTimeout)
        {
            System.Threading.Thread.Sleep(millisecondsTimeout);
        }

        /// <summary>
        /// 获取指定功能
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public ScriptRuntimeApi Api(string name)
        {
            if (ScriptRuntimeApi.LoadedDict.ContainsKey(name))
                return ScriptRuntimeApi.LoadedDict[name];
            return null;
        }

        Dictionary<string, object> m_ext;
        public Dictionary<string, object> Ext
        {
            get
            {
                if (m_ext == null)
                    m_ext = new Dictionary<string, object>();
                return m_ext;
            }
        }

        static object locker = new object();
        static ScriptRuntime m_instance;
        /// <summary>
        /// 当前工厂
        /// </summary>
        public static ScriptRuntime Current
        {
            get
            {
                if (m_instance == null)
                {
                    lock (locker)
                    {
                        if (m_instance == null)
                        {
                            m_instance = new ScriptRuntime();
                        }
                    }
                }
                return m_instance;
            }
        }
    }
}
