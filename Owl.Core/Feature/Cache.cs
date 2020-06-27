using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Runtime.Remoting.Messaging;
using System.Xml;
using System.IO;
using Owl.Feature.Impl.Cache;
using System.Collections;
using Owl.Util;
using System.Threading;

namespace Owl.Feature
{
    /// <summary>
    /// 应用程序上下文
    /// </summary>
    public class Cache
    {
        #region 线程缓存
        static AsyncLocal<Dictionary<string, object>> m_ThreadDict = new AsyncLocal<Dictionary<string, object>>();
        protected static IDictionary<string, object> ThreadDict
        {
            get
            {
                if (m_ThreadDict.Value == null)
                {
                    m_ThreadDict.Value = new Dictionary<string, object>();
                }
                return m_ThreadDict.Value;
            }
        }
        /// <summary>
        /// 将对象缓存在线程上下文中
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        public static void Thread(string key, object value)
        {
            ThreadDict[key] = value;
            //CallContext.SetData(key, value);
        }

        /// <summary>
        /// 从线程上下文中获取对象
        /// </summary>
        /// <param name="key">键</param>
        /// <returns></returns>
        public static object Thread(string key)
        {
            return ThreadDict.TryGetValue(key, out object value) ? value : null;
            //return CallContext.GetData(key);
        }

        /// <summary>
        /// 从线程上下文中获取对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="padnew">为true时：如果当前缓存不存在，则用新对象填充</param>
        /// <returns></returns>
        public static T Thread<T>(string key, Func<T> padobj = null)
        {
            T result;
            var value = Thread(key);
            if (value != null)
                result = (T)value;
            else if (value == null && padobj != null)
            {
                result = padobj();
                Thread(key, result);
            }
            else
                result = default(T);
            return result;
        }

        #endregion

        #region Session管理

        static readonly string m_sessionkey = "owl.feature.cache.sessionkey";


        /// <summary>
        /// 当前session的id
        /// </summary>
        public static string SessionId
        {
            get
            {
                var sessionid = Client.SessionId;
                return sessionid.Coalesce(Thread<string>(m_sessionkey, () => Guid.NewGuid().ToString()));
            }
            set
            {
                Thread(m_sessionkey, value);
            }
        }
        /// <summary>
        /// 判断当前环境Session是否有效
        /// </summary>
        public static bool SessionValid
        {
            get { return !string.IsNullOrEmpty(SessionId); }
        }

        /// <summary>
        /// 将对象缓存到当前Session中
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void Session(string key, object value, bool inproc = true)
        {
            var sessionid = SessionId;
            if (!string.IsNullOrEmpty(sessionid))
            {
                var cache = GlobalCache(inproc);
                cache.HashSet(sessionid, key, value);
                cache.KeyExpire(sessionid, TimeSpan.FromMinutes(30));
            }
            else
                Thread(key, value);
        }
        /// <summary>
        /// 从当前连接上下文中获取对象
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static object Session(string key, bool inproc = true)
        {
            var sessionid = SessionId;
            if (!string.IsNullOrEmpty(sessionid))
            {
                var cache = GlobalCache(inproc);
                cache.KeyExpire(sessionid, TimeSpan.FromMinutes(30));
                return cache.HashGet(sessionid, key);
            }
            else
                return Thread(key);
        }

        /// <summary>
        /// 从session中获取指定缓存
        /// </summary>
        /// <typeparam name="T">缓存项的类型</typeparam>
        /// <param name="key">key</param>
        /// <returns>是否为进程内Session</returns>
        public static T Session<T>(string key, bool inproc = true)
        {
            T result = default(T);
            var value = Session(key, inproc);
            if (value != null)
                result = (T)value;
            return result;
        }
        /// <summary>
        /// 从当前连接上下文中获取对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public static T Session<T>(string key, Func<T> padobj, bool inproc = true)
        {
            T result;
            var value = Session(key, inproc);
            if (value != null)
                result = (T)value;
            else if (value == null && padobj != null)
            {
                result = padobj();
                Session(key, result, inproc);
            }
            else
                result = default(T);
            return result;
        }

        /// <summary>
        /// 清理缓存
        /// </summary>
        public static void ClearSession()
        {
            var sessionid = SessionId;
            if (!string.IsNullOrEmpty(sessionid))
            {
                Inner.KeyRemove(sessionid);
                Outer.KeyRemove(sessionid);
                //Client.ResetSession();
            }
        }

        #endregion

        #region 全局缓存
        /// <summary>
        /// 全局缓存
        /// </summary>
        /// <param name="inproc">是否进程内</param>
        /// <returns></returns>
        public static ICache GlobalCache(bool inproc = true)
        {
            return inproc ? Inner : Outer;
        }
        /// <summary>
        /// 进程内全局缓存
        /// </summary>
        public static readonly ICache Inner = new InnerCache();

        /// <summary>
        /// 进程外全局缓存
        /// </summary>
        public static readonly ICache Outer = OuterCache.Provider ?? (ICache)new InnerCache();

        static string m_key_expire = "owl.feature.cache.keyexpire";

        static void ResetExpire(string key, bool inner, TimeSpan? expire = null)
        {
            var cache = GlobalCache(inner);
            if (expire.HasValue)
                cache.HashSet(m_key_expire, key, expire.Value);
            else
            {
                expire = (TimeSpan?)cache.HashGet(m_key_expire, key);
                if (expire.HasValue)
                    cache.KeyExpire(key, expire);
            }
        }

        /// <summary>
        /// 用指定优先级将对象放入全局缓存
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="inproc">是否进程内缓存</param>
        public static void Global(string key, object value, bool inproc = true)
        {
            GlobalCache(inproc).Set(key, value);
            ResetExpire(key, inproc);
        }

        /// <summary>
        /// 将对象放入全局缓存
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="expire">相对到期时间</param>
        /// <param name="inproc">是否进程内缓存</param>
        public static void Global(string key, object value, TimeSpan expire, bool inproc = true)
        {
            GlobalCache(inproc).Set(key, value, expire);
            ResetExpire(key, inproc, expire);
        }

        /// <summary>
        /// 获取全局缓存
        /// </summary>
        /// <param name="key"></param>
        /// <param name="inproc">是否进程内缓存</param>
        /// <returns></returns>
        public static object Global(string key, bool inproc = true)
        {
            ResetExpire(key, inproc);
            return GlobalCache(inproc).Get(key);
        }
        /// <summary>
        /// 获取全局缓存
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="inproc">是否进程内缓存</param>
        /// <returns></returns>
        public static T Global<T>(string key, bool inproc = true)
        {
            var value = Global(key, inproc);
            if (value == null)
                return default(T);
            return (T)value;
        }

        /// <summary>
        /// 删除全局缓存
        /// </summary>
        /// <param name="key"></param>
        /// <param name="inproc">是否进程内缓存</param>
        public static void RemoveGlobal(string key, bool inproc = true)
        {
            GlobalCache(inproc).KeyRemove(key);
            GlobalCache(inproc).HashSet(m_key_expire, key, null);
        }
        #endregion

        static Dictionary<string, object> filelockers = new Dictionary<string, object>();
        static object GetFileLocker(string filepath)
        {
            lock (filelockers)
            {
                if (!filelockers.ContainsKey(filepath))
                {
                    filelockers[filepath] = new object();
                }
                return filelockers[filepath];
            }
        }
        /// <summary>
        /// 文本文件缓存获取
        /// </summary>
        /// <param name="filepath"></param>
        /// <returns></returns>
        public static string File(string filepath)
        {
            filepath = filepath.ToLower();
            var locker = GetFileLocker(filepath);
            lock(locker)
            {
                if (!Inner.KeyExists(filepath))
                {
                    if (!System.IO.File.Exists(filepath))
                        return null;
                    Inner.Set(filepath, FileHelper.ReadAllText(filepath, Encoding.UTF8));
                    var directory = Path.GetDirectoryName(filepath);
                    var filename = Path.GetFileName(filepath);
                    FileSystemWatcher watcher = new FileSystemWatcher(directory, filename);
                    watcher.NotifyFilter = NotifyFilters.LastWrite;
                    watcher.Changed += Watcher_Changed;
                }
                return Inner.Get(filepath).ToString();
            }
        }

        private static void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            var filepath = e.FullPath.ToLower();
            var locker = GetFileLocker(filepath);
            lock(locker)
            {
                Inner.Set(filepath, FileHelper.ReadAllText(filepath, Encoding.UTF8));
            }
            
        }
    }
}
