using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Reflection;
using Owl.Util;
namespace System
{
    /// <summary>
    /// 引擎提供者比较器
    /// </summary>
    internal class ProviderComparer : Comparer<Provider>
    {
        public override int Compare(Provider x, Provider y)
        {
            if (x.Priority > y.Priority)
                return -1;
            return 1;//为避免优先级一样的提供者被屏蔽掉

            //if (x.Priority == y.Priority)
            //    return 0;
            //else if (x.Priority > y.Priority)
            //    return -1;
            //return 1;
        }
    }

    /// <summary>
    /// 引擎提供者
    /// </summary>
    public abstract class Provider
    {
        /// <summary>
        /// 优先级 数值越大优先级越高
        /// </summary>
        public abstract int Priority { get; }

        /// <summary>
        /// 引擎提供者名称
        /// </summary>
        protected virtual string InnerName
        {
            get { return string.Empty; }
        }
        string m_name;
        /// <summary>
        /// 引擎提供者名称
        /// </summary>
        public string Name
        {
            get
            {
                if (string.IsNullOrEmpty(m_name))
                {
                    m_name = InnerName;
                    if (string.IsNullOrEmpty(m_name))
                        m_name = GetType().FullName.ToLower();
                }
                return m_name;
            }
        }
        /// <summary>
        /// 获取provider是否有效
        /// </summary>
        /// <value><c>true</c> if is valid; otherwise, <c>false</c>.</value>
        public virtual bool IsValid { get { return true; }}
    }

    /// <summary>
    /// 引擎模式
    /// </summary>
    public enum EngineMode
    {
        /// <summary>
        /// 多Provider
        /// </summary>
        Multiple,

        /// <summary>
        /// 单一Provider,首选匹配配置项，如果没有则选择优先级最高的Provider
        /// </summary>
        Single
    }

    /// <summary>
    /// 引擎
    /// </summary>
    public abstract class Engine : TypeLoader<Engine>
    {
        /// <summary>
        /// 引擎模式
        /// </summary>
        protected virtual EngineMode Mode
        {
            get { return EngineMode.Multiple; }
        }

        /// <summary>
        /// 是否跳过异常
        /// </summary>
        protected virtual bool SkipException
        {
            get { return true; }
        }

        protected virtual object Invoke(string method, object[] args)
        {
            return GetType().GetMethod(method, BindingFlags.Static | BindingFlags.Public).FaseInvoke(null, args);
        }

        static Dictionary<string, Engine> m_engines = new Dictionary<string, Engine>();
        public static Engine GetEngine(string name)
        {
            Engine engine = null;
            if (!m_engines.ContainsKey(name))
            {
                var type = Types.FirstOrDefault(s => s.Name == name).BaseType;
                engine = type.GetProperty("Instance", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null, null) as Engine;
                m_engines[name] = engine;
            }
            else
                engine = m_engines[name];
            return engine;
        }

        /// <summary>
        /// 根据引擎名称执行引擎的方法
        /// </summary>
        /// <param name="name">引擎的名称</param>
        /// <param name="method"> 调用的方法</param>
        /// <param name="args">参数列表</param>
        /// <returns></returns>
        public static object Invoke(string name, string method, params object[] args)
        {
            var engine = GetEngine(name);
            if (engine == null)
                throw new Exception2("引擎{0}不存在，请检查代码!", name);
            return engine.Invoke(method, args);
        }
    }

    /// <summary>
    /// 引擎基类
    /// </summary>
    /// <typeparam name="TProvider"></typeparam>
    public abstract class Engine<TProvider, TEngine> : Engine
        where TProvider : Provider
        where TEngine : Engine<TProvider, TEngine>, new()
    {
        static object locker = new object();
        static TEngine m_instance;
        /// <summary>
        /// 单例
        /// </summary>
        protected static TEngine Instance
        {
            get
            {
                if (m_instance == null)
                {
                    lock (locker)
                    {
                        if (m_instance == null)
                            m_instance = new TEngine();
                    }
                }
                return m_instance;
            }
        }

        #region providers 相关
        protected SortedSet<TProvider> m_Providers = new SortedSet<TProvider>(new ProviderComparer());

        void loadAsm(string name, Assembly assembly)
        {
            foreach (var provider in TypeHelper.LoadFromAsm<TProvider>(assembly))
            {
                if (Allows.Count == 0 || Allows.Contains(provider.Name))
                    m_Providers.Add(provider);
            }
        }
        void unloadAsm(string name, Assembly assembly)
        {
            m_Providers.RemoveWhere(s => s.GetType().Assembly.FullName == assembly.FullName);
        }

        HashSet<string> Allows;
        public Engine()
        {
            if (typeof(TEngine) != GetType())
                throw new Exception("类型不一致");
            var enginename = typeof(TEngine).Name.ToLower();
            var length = enginename.LastIndexOf("engine");
            if (length > 0)
                enginename = enginename.Substring(0, length);
            var mapto = AppConfig.Section.GetMap("engine", enginename);
            if (!string.IsNullOrEmpty(mapto))
                Allows = new HashSet<string>(mapto.Split(','));
            else
                Allows = new HashSet<string>();
            AsmHelper.RegisterResource(loadAsm, unloadAsm);
        }

        /// <summary>
        /// Providers
        /// </summary>
        public static IEnumerable<TProvider> Providers
        {
            get
            {
                var validproviders = Instance.m_Providers.Where(s => s.IsValid).ToList();
                if (Instance.Mode == EngineMode.Multiple)
                    return validproviders;
                else
                {
                    var providers = new List<TProvider>();
                    if (validproviders.Count > 0)
                        providers.Add(validproviders.FirstOrDefault());
                    return providers;
                }
            }
        }

        /// <summary>
        /// 首选Provider
        /// </summary>
        public static TProvider Provider
        {
            get { return Instance.m_Providers.FirstOrDefault(s=>s.IsValid); }
        }

        /// <summary>
        /// 获取第一个符合条件的provider
        /// </summary>
        /// <typeparam name="TP">provider类型</typeparam>
        /// <returns></returns>
        public static TProvider GetProvider<TP>()
            where TP : TProvider
        {
            return Providers.FirstOrDefault(s => s.GetType() == typeof(TP));
        }

        public static TProvider GetProvider(string name)
        {
            return Providers.FirstOrDefault(s => s.Name == name);
        }
        #endregion

        #region 执行没有返回值的操作
        /// <summary>
        /// 执行没有返回值的操作
        /// </summary>
        /// <param name="func">委托</param>
        protected static void Execute(Func<TProvider, Action> func)
        {
            foreach (var provider in Providers)
            {
                try
                {
                    func(provider)();
                }
                catch (NotImplementedException)
                {
                }
                catch (Exception ex)
                {
                    if (!Instance.SkipException)
                        throw new Exception(ex.Message, ex);
                }
            }
        }
        /// <summary>
        /// 执行没有返回值的操作
        /// </summary>
        /// <typeparam name="TArg">参数类型</typeparam>
        /// <param name="func">委托</param>
        /// <param name="arg">参数</param>
        protected static void Execute<TArg>(Func<TProvider, Action<TArg>> func, TArg arg)
        {
            foreach (var provider in Providers)
            {
                try
                {
                    func(provider)(arg);
                }
                catch (NotImplementedException)
                {
                }
                catch (Exception ex)
                {
                    if (!Instance.SkipException)
                        throw new Exception(ex.Message, ex);
                }
            }
        }
        /// <summary>
        /// 执行没有返回值的操作
        /// </summary>
        /// <typeparam name="TArg1"></typeparam>
        /// <typeparam name="TArg2"></typeparam>
        /// <param name="func"></param>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        protected static void Execute<TArg1, TArg2>(Func<TProvider, Action<TArg1, TArg2>> func, TArg1 arg1, TArg2 arg2)
        {
            foreach (var provider in Providers)
            {
                try
                {
                    func(provider)(arg1, arg2);
                }
                catch (NotImplementedException)
                {
                }
                catch (Exception ex)
                {
                    if (!Instance.SkipException)
                        throw new Exception(ex.Message, ex);
                }
            }
        }
        /// <summary>
        /// 执行没有返回值的操作
        /// </summary>
        /// <typeparam name="TArg1">参数1类型</typeparam>
        /// <typeparam name="TArg2">参数2类型</typeparam>
        /// <typeparam name="TArg3">参数3类型</typeparam>
        /// <param name="func">委托</param>
        /// <param name="arg1">参数1</param>
        /// <param name="arg2">参数2</param>
        /// <param name="arg3">参数3</param>
        protected static void Execute<TArg1, TArg2, TArg3>(Func<TProvider, Action<TArg1, TArg2, TArg3>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3)
        {
            foreach (var provider in Providers)
            {
                try
                {
                    func(provider)(arg1, arg2, arg3);
                }
                catch (NotImplementedException)
                {
                }
                catch (Exception ex)
                {
                    if (!Instance.SkipException)
                        throw new Exception(ex.Message, ex);
                }
            }
        }
        /// <summary>
        /// 执行没有返回值的操作
        /// </summary>
        /// <typeparam name="TArg1"></typeparam>
        /// <typeparam name="TArg2"></typeparam>
        /// <typeparam name="TArg3"></typeparam>
        /// <typeparam name="TArg4"></typeparam>
        /// <param name="func"></param>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        /// <param name="arg3"></param>
        /// <param name="arg4"></param>
        protected static void Execute<TArg1, TArg2, TArg3, TArg4>(Func<TProvider, Action<TArg1, TArg2, TArg3, TArg4>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4)
        {
            foreach (var provider in Providers)
            {
                try
                {
                    func(provider)(arg1, arg2, arg3, arg4);
                }
                catch (NotImplementedException)
                {
                }
                catch (Exception ex)
                {
                    if (!Instance.SkipException)
                        throw new Exception(ex.Message, ex);
                }
            }
        }
        /// <summary>
        /// 执行没有返回值的操作
        /// </summary>
        /// <typeparam name="TArg1"></typeparam>
        /// <typeparam name="TArg2"></typeparam>
        /// <typeparam name="TArg3"></typeparam>
        /// <typeparam name="TArg4"></typeparam>
        /// <typeparam name="TArg5"></typeparam>
        /// <param name="func"></param>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        /// <param name="arg3"></param>
        /// <param name="arg4"></param>
        /// <param name="arg5"></param>
        protected static void Execute<TArg1, TArg2, TArg3, TArg4, TArg5>(Func<TProvider, Action<TArg1, TArg2, TArg3, TArg4, TArg5>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5)
        {
            foreach (var provider in Providers)
            {
                try
                {
                    func(provider)(arg1, arg2, arg3, arg4, arg5);
                }
                catch (NotImplementedException)
                {
                }
                catch (Exception ex)
                {
                    if (!Instance.SkipException)
                        throw new Exception(ex.Message, ex);
                }
            }
        }
        /// <summary>
        /// 执行没有返回值的操作
        /// </summary>
        /// <typeparam name="TArg1"></typeparam>
        /// <typeparam name="TArg2"></typeparam>
        /// <typeparam name="TArg3"></typeparam>
        /// <typeparam name="TArg4"></typeparam>
        /// <typeparam name="TArg5"></typeparam>
        /// <typeparam name="TArg6"></typeparam>
        /// <param name="func"></param>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        /// <param name="arg3"></param>
        /// <param name="arg4"></param>
        /// <param name="arg5"></param>
        /// <param name="arg6"></param>
        protected static void Execute<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(Func<TProvider, Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6)
        {
            foreach (var provider in Providers)
            {
                try
                {
                    func(provider)(arg1, arg2, arg3, arg4, arg5, arg6);
                }
                catch (NotImplementedException)
                {
                }
                catch (Exception ex)
                {
                    if (!Instance.SkipException)
                        throw new Exception(ex.Message, ex);
                }
            }
        }
        #endregion

        #region 执行有非集合返回值的操作
        /// <summary>
        /// 执行有单一返回值的操作
        /// </summary>
        /// <typeparam name="TResult">返回值类型</typeparam>
        /// <param name="func">执行委托</param>
        /// <param name="breakfunc">满足本条件时即返回</param>
        /// <returns></returns>
        protected static TResult Execute2<TResult>(Func<TProvider, Func<TResult>> func, Func<TResult, bool> breakfunc = null, TResult _default = default(TResult))
        {
            TResult result = default(TResult);
            if (breakfunc == null)
                breakfunc = s => (object)s != null;
            foreach (var provider in Providers)
            {
                try
                {
                    result = func(provider)();
                    if (breakfunc(result))
                        break;
                }
                catch (NotImplementedException)
                {
                }
                catch (Exception ex)
                {
                    if (!Instance.SkipException)
                        throw new Exception(ex.Message, ex);
                }

            }
            return object.Equals(result, default(TResult)) ? _default : result;
        }
        /// <summary>
        /// 执行有单一返回值的操作
        /// </summary>
        /// <typeparam name="TArg"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="func"></param>
        /// <param name="arg"></param>
        /// <returns></returns>
        protected static TResult Execute2<TArg, TResult>(Func<TProvider, Func<TArg, TResult>> func, TArg arg, Func<TResult, bool> breakfunc = null, TResult _default = default(TResult))
        {
            TResult result = default(TResult);
            if (breakfunc == null)
                breakfunc = s => (object)s != null;
            foreach (var provider in Providers)
            {
                try
                {
                    result = func(provider)(arg);
                    if (breakfunc(result))
                        break;
                }
                catch (NotImplementedException)
                {
                }
                catch (Exception ex)
                {
                    if (!Instance.SkipException)
                        throw new Exception(ex.Message, ex);
                }
            }
            return object.Equals(result, default(TResult)) ? _default : result;
        }
        /// <summary>
        /// 执行有单一返回值的操作
        /// </summary>
        /// <typeparam name="TArg1"></typeparam>
        /// <typeparam name="TArg2"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="func"></param>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        /// <param name="breakfunc"></param>
        /// <param name="_default"></param>
        /// <returns></returns>
        protected static TResult Execute2<TArg1, TArg2, TResult>(Func<TProvider, Func<TArg1, TArg2, TResult>> func, TArg1 arg1, TArg2 arg2, Func<TResult, bool> breakfunc = null, TResult _default = default(TResult))
        {
            TResult result = default(TResult);
            if (breakfunc == null)
                breakfunc = s => (object)s != null;
            foreach (var provider in Providers)
            {

                try
                {
                    result = func(provider)(arg1, arg2);
                    if (breakfunc(result)) break;
                }
                catch (NotImplementedException)
                {
                }
                catch (Exception ex)
                {
                    if (!Instance.SkipException)
                        throw new Exception(ex.Message, ex);
                }
            }
            return object.Equals(result, default(TResult)) ? _default : result;
        }
        /// <summary>
        /// 执行有单一返回值的操作
        /// </summary>
        /// <typeparam name="TArg1"></typeparam>
        /// <typeparam name="TArg2"></typeparam>
        /// <typeparam name="TArg3"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="func"></param>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        /// <param name="arg3"></param>
        /// <param name="breakfunc"></param>
        /// <param name="_default"></param>
        /// <returns></returns>
        protected static TResult Execute2<TArg1, TArg2, TArg3, TResult>(Func<TProvider, Func<TArg1, TArg2, TArg3, TResult>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, Func<TResult, bool> breakfunc = null, TResult _default = default(TResult))
        {
            TResult result = default(TResult);
            if (breakfunc == null)
                breakfunc = s => (object)s != null;
            foreach (var provider in Providers)
            {
                try
                {
                    result = func(provider)(arg1, arg2, arg3);
                    if (breakfunc(result)) break;
                }
                catch (NotImplementedException)
                {
                }
                catch (Exception ex)
                {
                    if (!Instance.SkipException)
                        throw new Exception(ex.Message, ex);
                }
            }
            return object.Equals(result, default(TResult)) ? _default : result;
        }
        /// <summary>
        /// 执行有单一返回值的操作
        /// </summary>
        /// <typeparam name="TArg1"></typeparam>
        /// <typeparam name="TArg2"></typeparam>
        /// <typeparam name="TArg3"></typeparam>
        /// <typeparam name="TArg4"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="func"></param>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        /// <param name="arg3"></param>
        /// <param name="arg4"></param>
        /// <param name="breakfunc"></param>
        /// <param name="_default"></param>
        /// <returns></returns>
        protected static TResult Execute2<TArg1, TArg2, TArg3, TArg4, TResult>(Func<TProvider, Func<TArg1, TArg2, TArg3, TArg4, TResult>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, Func<TResult, bool> breakfunc = null, TResult _default = default(TResult))
        {
            TResult result = default(TResult);
            if (breakfunc == null)
                breakfunc = s => (object)s != null;
            foreach (var provider in Providers)
            {
                try
                {
                    result = func(provider)(arg1, arg2, arg3, arg4);
                    if (breakfunc(result)) break;
                }
                catch (NotImplementedException)
                {
                }
                catch (Exception ex)
                {
                    if (!Instance.SkipException)
                        throw new Exception(ex.Message, ex);
                }
            }
            return object.Equals(result, default(TResult)) ? _default : result;
        }
        /// <summary>
        /// 执行有单一返回值的操作
        /// </summary>
        /// <typeparam name="TArg1"></typeparam>
        /// <typeparam name="TArg2"></typeparam>
        /// <typeparam name="TArg3"></typeparam>
        /// <typeparam name="TArg4"></typeparam>
        /// <typeparam name="TArg5"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="func"></param>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        /// <param name="arg3"></param>
        /// <param name="arg4"></param>
        /// <param name="arg5"></param>
        /// <param name="breakfunc"></param>
        /// <param name="_default"></param>
        /// <returns></returns>
        protected static TResult Execute2<TArg1, TArg2, TArg3, TArg4, TArg5, TResult>(Func<TProvider, Func<TArg1, TArg2, TArg3, TArg4, TArg5, TResult>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, Func<TResult, bool> breakfunc = null, TResult _default = default(TResult))
        {
            TResult result = default(TResult);
            if (breakfunc == null)
                breakfunc = s => (object)s != null;
            foreach (var provider in Providers)
            {
                try
                {
                    result = func(provider)(arg1, arg2, arg3, arg4, arg5);
                    if (breakfunc(result)) break;
                }
                catch (NotImplementedException)
                {
                }
                catch (Exception ex)
                {
                    if (!Instance.SkipException)
                        throw new Exception(ex.Message, ex);
                }
            }
            return object.Equals(result, default(TResult)) ? _default : result;
        }
        /// <summary>
        /// 执行有单一返回值的操作
        /// </summary>
        /// <typeparam name="TArg1"></typeparam>
        /// <typeparam name="TArg2"></typeparam>
        /// <typeparam name="TArg3"></typeparam>
        /// <typeparam name="TArg4"></typeparam>
        /// <typeparam name="TArg5"></typeparam>
        /// <typeparam name="TArg6"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="func"></param>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        /// <param name="arg3"></param>
        /// <param name="arg4"></param>
        /// <param name="arg5"></param>
        /// <param name="arg6"></param>
        /// <param name="breakfunc"></param>
        /// <param name="_default"></param>
        /// <returns></returns>
        protected static TResult Execute2<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult>(Func<TProvider, Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, Func<TResult, bool> breakfunc = null, TResult _default = default(TResult))
        {
            TResult result = default(TResult);
            if (breakfunc == null)
                breakfunc = s => (object)s != null;
            foreach (var provider in Providers)
            {
                try
                {
                    result = func(provider)(arg1, arg2, arg3, arg4, arg5, arg6);
                    if (breakfunc(result)) break;
                }
                catch (NotImplementedException)
                {
                }
                catch (Exception ex)
                {
                    if (!Instance.SkipException)
                        throw new Exception(ex.Message, ex);
                }
            }
            return object.Equals(result, default(TResult)) ? _default : result;
        }
        #endregion

        #region 执行有需合并集合的操作
        /// <summary>
        /// 执行操作并将返回的结果合并
        /// </summary>
        /// <typeparam name="TResult">返回值类型</typeparam>
        /// <param name="func">调用的函数</param>
        /// <param name="filter">返回值过滤器</param>
        /// <returns></returns>
        protected static List<TResult> Execute3<TResult>(Func<TProvider, Func<IEnumerable<TResult>>> func, Func<TResult, bool> filter = null)
        {
            List<TResult> results = new List<TResult>();
            foreach (var provider in Providers)
            {
                try
                {
                    var tmp = func(provider)();
                    if (tmp != null)
                        results.AddRange(tmp.Where(s => filter == null || filter(s)));
                }
                catch (NotImplementedException)
                {
                }
                catch (Exception ex)
                {
                    if (!Instance.SkipException)
                        throw new Exception(ex.Message, ex);
                }
            }
            return results;
        }
        /// <summary>
        /// 执行操作并将返回的结果合并
        /// </summary>
        /// <typeparam name="TArg">参数类型</typeparam>
        /// <typeparam name="TResult">返回值类型</typeparam>
        /// <param name="func">调用的函数</param>
        /// <param name="arg">参数</param>
        /// <param name="filter">返回值过滤器</param>
        /// <returns></returns>
        protected static List<TResult> Execute3<TArg, TResult>(Func<TProvider, Func<TArg, IEnumerable<TResult>>> func, TArg arg, Func<TResult, bool> filter = null)
        {
            List<TResult> results = new List<TResult>();
            foreach (var provider in Providers)
            {
                try
                {
                    var tmp = func(provider)(arg);
                    if (tmp != null)
                        results.AddRange(tmp.Where(s => filter == null || filter(s)));
                }
                catch (NotImplementedException)
                {
                }
                catch (Exception ex)
                {
                    if (!Instance.SkipException)
                        throw new Exception(ex.Message, ex);
                }
            }
            return results;
        }
        /// <summary>
        /// 执行操作并将返回的结果合并
        /// </summary>
        /// <typeparam name="TArg">参数类型</typeparam>
        /// <typeparam name="TArg2">参数2类型</typeparam>
        /// <typeparam name="TResult">返回值类型</typeparam>
        /// <param name="func">调用的函数</param>
        /// <param name="arg">参数</param>
        /// <param name="arg2">参数2</param>
        /// <param name="filter">返回值过滤器</param>
        /// <returns></returns>
        protected static List<TResult> Execute3<TArg1, TArg2, TResult>(Func<TProvider, Func<TArg1, TArg2, IEnumerable<TResult>>> func, TArg1 arg1, TArg2 arg2, Func<TResult, bool> filter = null)
        {
            List<TResult> results = new List<TResult>();
            foreach (var provider in Providers)
            {
                try
                {
                    var tmp = func(provider)(arg1, arg2);
                    if (tmp != null)
                        results.AddRange(tmp.Where(s => filter == null || filter(s)));
                }
                catch (NotImplementedException)
                {
                }
                catch (Exception ex)
                {
                    if (!Instance.SkipException) throw new Exception(ex.Message, ex);
                }
            }
            return results;
        }
        /// <summary>
        /// 执行操作并将返回的结果合并
        /// </summary>
        /// <typeparam name="TArg1"></typeparam>
        /// <typeparam name="TArg2"></typeparam>
        /// <typeparam name="TArg3"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="func"></param>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        /// <param name="arg3"></param>
        /// <param name="filter">返回值过滤器</param>
        /// <returns></returns>
        protected static List<TResult> Execute3<TArg1, TArg2, TArg3, TResult>(Func<TProvider, Func<TArg1, TArg2, TArg3, IEnumerable<TResult>>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, Func<TResult, bool> filter = null)
        {
            List<TResult> results = new List<TResult>();
            foreach (var provider in Providers)
            {
                try
                {
                    var tmp = func(provider)(arg1, arg2, arg3);
                    if (tmp != null)
                        results.AddRange(tmp.Where(s => filter == null || filter(s)));
                }
                catch (NotImplementedException)
                {
                }
                catch (Exception ex)
                {
                    if (!Instance.SkipException) throw new Exception(ex.Message, ex);
                }
            }
            return results;
        }
        /// <summary>
        /// 执行操作并将返回的结果合并
        /// </summary>
        /// <typeparam name="TArg1"></typeparam>
        /// <typeparam name="TArg2"></typeparam>
        /// <typeparam name="TArg3"></typeparam>
        /// <typeparam name="TArg4"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="func"></param>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        /// <param name="arg3"></param>
        /// <param name="arg4"></param>
        /// <returns></returns>
        protected static List<TResult> Execute3<TArg1, TArg2, TArg3, TArg4, TResult>(Func<TProvider, Func<TArg1, TArg2, TArg3, TArg4, IEnumerable<TResult>>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, Func<TResult, bool> filter = null)
        {
            List<TResult> results = new List<TResult>();
            foreach (var provider in Providers)
            {
                try
                {
                    var tmp = func(provider)(arg1, arg2, arg3, arg4);
                    if (tmp != null)
                        results.AddRange(tmp.Where(s => filter == null || filter(s)));
                }
                catch (NotImplementedException)
                {
                }
                catch (Exception ex)
                {
                    if (!Instance.SkipException) throw new Exception(ex.Message, ex);
                }
            }
            return results;
        }
        /// <summary>
        /// 执行操作并将返回的结果合并
        /// </summary>
        /// <typeparam name="TArg1"></typeparam>
        /// <typeparam name="TArg2"></typeparam>
        /// <typeparam name="TArg3"></typeparam>
        /// <typeparam name="TArg4"></typeparam>
        /// <typeparam name="TArg5"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="func"></param>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        /// <param name="arg3"></param>
        /// <param name="arg4"></param>
        /// <param name="arg5"></param>
        /// <returns></returns>
        protected static List<TResult> Execute3<TArg1, TArg2, TArg3, TArg4, TArg5, TResult>(Func<TProvider, Func<TArg1, TArg2, TArg3, TArg4, TArg5, IEnumerable<TResult>>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, Func<TResult, bool> filter = null)
        {
            List<TResult> results = new List<TResult>();
            foreach (var provider in Providers)
            {
                try
                {
                    var tmp = func(provider)(arg1, arg2, arg3, arg4, arg5);
                    if (tmp != null)
                        results.AddRange(tmp.Where(s => filter == null || filter(s)));
                }
                catch (NotImplementedException)
                {
                }
                catch (Exception ex)
                {
                    if (!Instance.SkipException) throw new Exception(ex.Message, ex);
                }
            }
            return results;
        }
        /// <summary>
        /// 执行操作并将返回的结果合并
        /// </summary>
        /// <typeparam name="TArg1"></typeparam>
        /// <typeparam name="TArg2"></typeparam>
        /// <typeparam name="TArg3"></typeparam>
        /// <typeparam name="TArg4"></typeparam>
        /// <typeparam name="TArg5"></typeparam>
        /// <typeparam name="TArg6"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="func"></param>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        /// <param name="arg3"></param>
        /// <param name="arg4"></param>
        /// <param name="arg5"></param>
        /// <param name="arg6"></param>
        /// <returns></returns>
        protected static List<TResult> Execute3<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult>(Func<TProvider, Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, IEnumerable<TResult>>> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, Func<TResult, bool> filter = null)
        {
            List<TResult> results = new List<TResult>();
            foreach (var provider in Providers)
            {

                try
                {
                    var tmp = func(provider)(arg1, arg2, arg3, arg4, arg5, arg6);
                    if (tmp != null)
                        results.AddRange(tmp.Where(s => filter == null || filter(s)));
                }
                catch (NotImplementedException)
                {
                }
                catch (Exception ex)
                {
                    if (!Instance.SkipException) throw new Exception(ex.Message, ex);
                }
            }
            return results;
        }
        #endregion
    }
}
