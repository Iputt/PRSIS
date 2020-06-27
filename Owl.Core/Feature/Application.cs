using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Owl.Util;

namespace Owl.Feature
{
    /// <summary>
    /// 应用程序属性
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public abstract class ApplicationAttribute : Attribute
    {
        /// <summary>
        /// 执行序号，数字越大执行越晚
        /// </summary>
        public int Ordinal { get; set; }

        /// <summary>
        /// 方法
        /// </summary>
        public MethodInfo Method { get; set; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ordinal">执行序号，数字越大执行越晚</param>
        public ApplicationAttribute()
        {
            Ordinal = 1000;
        }
    }
    /// <summary>
    /// 当本应用程序获取主控权时触发
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class OnApplicationGetMCAttribute : ApplicationAttribute
    {

    }
    /// <summary>
    /// 当应用程序丢失主控权时触发
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class OnApplicationLoseMCAttribute : ApplicationAttribute
    {

    }

    /// <summary>
    /// 应用程序进行准备工作时执行
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class OnApplicatonPrepareAttribute : ApplicationAttribute
    {
    }
    /// <summary>
    /// 应用程序启动时执行
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class OnApplicatonStartAttribute : ApplicationAttribute
    {
    }
    /// <summary>
    /// 应用程序停止时执行
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class OnApplicatonStopAttribute : ApplicationAttribute
    {
    }
    /// <summary>
    /// 应用程序，在分布式应用中标记一个进程
    /// </summary>
    public class Application
    {
        List<ApplicationAttribute> m_Behaviors = new List<ApplicationAttribute>();
        private void InvokeBehavior<TAttr>() where TAttr : ApplicationAttribute
        {
            foreach (var behavior in m_Behaviors.OfType<TAttr>().OrderBy(s => s.Ordinal))
            {
                behavior.Method.FaseInvoke(null);
            }
        }

        private Application()
        {
            _Name = Serial.GetRandom(10);// Guid.NewGuid().ToString();
            AsmHelper.RegisterResource(
                (s, asm) =>
                {
                    foreach (var type in asm.GetTypes())
                    {
                        foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public))
                        {
                            var attr = method.GetCustomAttributes(typeof(ApplicationAttribute), false).Cast<ApplicationAttribute>().FirstOrDefault();
                            if (attr == null)
                                continue;
                            attr.Method = method;
                            m_Behaviors.Add(attr);
                        }
                    }
                },
                (s, asm) =>
                {
                    m_Behaviors.RemoveAll(t => t.Method.DeclaringType.Assembly == asm);
                }
            );
        }
        /// <summary>
        /// 实例
        /// </summary>
        static readonly Application Instance = new Application();

        /// <summary>
        /// 是否为主数据
        /// </summary>
        protected bool _IsMaster { get; private set; }

        /// <summary>
        /// 应用实例名称
        /// </summary>
        protected string _Name { get; private set; }

        protected string CacheKey = "owl.feature.appinstance.cachekey";

        protected string AppsKey = "owl.feature.appinstance.appskey";

        protected string TaskId { get; private set; }

        bool SetCacheNE()
        {
            return Cache.Outer.SetNE(CacheKey, _Name, TimeSpan.FromSeconds(15));
        }

        /// <summary>
        /// 获取主控权时
        /// </summary>
        protected void OnGetMC()
        {
            InvokeBehavior<OnApplicationGetMCAttribute>();
        }
        /// <summary>
        /// 丢失主控制权时
        /// </summary>
        protected void OnLoseMC()
        {
            InvokeBehavior<OnApplicationLoseMCAttribute>();
        }

        protected void Keepalive()
        {
            if (!_IsMaster)
            {
                if (SetCacheNE())
                {
                    _IsMaster = true;
                    TaskMgr.StartTask(OnGetMC);
                }
            }
            else
            {
                if (!object.Equals(Cache.Outer.Get(CacheKey), _Name))
                {
                    _IsMaster = false;
                    TaskMgr.StartTask(OnLoseMC);
                }
                else
                    Cache.Outer.KeyExpire(CacheKey, TimeSpan.FromSeconds(15));
            }
            Cache.Outer.HashSet(AppsKey, Name, DateTime.Now);
        }
        /// <summary>
        /// 开始keepalive
        /// </summary>
        protected void StartKeepalive()
        {
            TaskId = TaskMgr.AddTask(Keepalive, 5, 0);
        }
        /// <summary>
        /// 结束keepalive
        /// </summary>
        protected void StopKeepalive()
        {
            if (!string.IsNullOrEmpty(TaskId))
                TaskMgr.RemoveTask(TaskId);
            if (_IsMaster && object.Equals(Cache.Outer.Get(CacheKey), _Name))
            {
                _IsMaster = false;
                Cache.Outer.KeyRemove(CacheKey);
                Cache.Outer.HashDelete(AppsKey, Name, true);
                OnLoseMC();
            }
        }

        /// <summary>
        /// 实例名称
        /// </summary>
        public static string Name { get { return Instance._Name; } }

        /// <summary>
        /// 获取分布式部署中所有的实例名称
        /// </summary>
        public static IEnumerable<string> AllApplications
        {
            get
            {
                List<string> result = new List<string>();
                var allapps = Cache.Outer.HashGetAll(Instance.AppsKey);
                foreach (string key in allapps.Keys)
                {
                    var dt = (DateTime)allapps[key];
                    if (dt.AddSeconds(5 * 3) > DateTime.Now)
                        result.Add(key);
                    else
                        Cache.Outer.HashDelete(Instance.AppsKey, key, true);
                }
                return result;
            }
        }

        /// <summary>
        /// 是否为主实例
        /// </summary>
        public static bool IsMaster
        {
            get { return Instance._IsMaster; }
        }

        /// <summary>
        /// 应用程序启动前准备
        /// </summary>
        public static void Prepare()
        {
            Instance.InvokeBehavior<OnApplicatonPrepareAttribute>();
        }

        /// <summary>
        /// 应用程序启动
        /// </summary>
        public static void Start()
        {
            Instance.InvokeBehavior<OnApplicatonStartAttribute>();
            Instance.StartKeepalive();
        }
        /// <summary>
        /// 应用程序停止
        /// </summary>
        public static void Stop()
        {
            Instance.InvokeBehavior<OnApplicatonStopAttribute>();
            Instance.StopKeepalive();
        }
    }
}
