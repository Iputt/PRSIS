using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Owl.Feature.Impl.Cache
{
    internal class GCacheEngine : Engine<CacheProvider, GCacheEngine>
    {
        static CacheProvider m_inproc;
        protected static CacheProvider Inproc
        {
            get
            {
                if (m_inproc == null)
                    m_inproc = Providers.FirstOrDefault(s => s.Inproc);
                return m_inproc;
            }
        }
        static CacheProvider m_outproc;
        protected static CacheProvider Outproc
        {
            get
            {
                if (m_outproc == null)
                    m_outproc = Providers.FirstOrDefault(s => !s.Inproc);
                return m_outproc;
            }
        }
        private static CacheProvider GetProvider(bool inproc)
        {
            return inproc ? Inproc : Outproc;
            //var provider = Providers.FirstOrDefault(s => s.Mode == mode);
            //if (provider == null)
            //    provider = Providers.FirstOrDefault(s => s.Mode == CacheMode.Memory);
            //return provider;
        }

        /// <summary>
        /// 用指定优先级将对象放入全局缓存
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="autoremove">是否可被系统自动回收</param>
        /// <param name="mode">缓存模式</param>
        public static void Add(string key, object value, bool autoremove, bool inproc)
        {
            GetProvider(inproc).Add(key, value, autoremove);
        }

        /// <summary>
        /// 用指定优先级将对象放入全局缓存
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="expire">绝对到期时间</param>
        /// <param name="autoremove">是否可被系统自动回收</param>
        /// <param name="mode">缓存模式</param>
        public static void Add(string key, object value, DateTime expire, bool autoremove, bool inproc)
        {
            GetProvider(inproc).Add(key, value, expire, autoremove);
        }
        /// <summary>
        /// 用指定优先级将对象放入全局缓存
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="expire">滑动到期时间</param>
        /// <param name="autoremove">是否可被系统自动回收</param>
        /// <param name="mode">缓存模式</param>
        public static void Add(string key, object value, TimeSpan expire, bool autoremove, bool inproc)
        {
            GetProvider(inproc).Add(key, value, expire, autoremove);
        }

        /// <summary>
        /// 将文件映射对象放入全局缓存中
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="fielname">文件名称</param>
        /// <param name="resolver">文件解析</param>
        /// <param name="expire">绝对到期时间</param>
        /// <param name="autoremove">是否可被系统自动回收</param>
        /// <param name="mode">缓存模式</param>
        public static void Add(string key, string fielname, Func<byte[], object> resolver, DateTime expire, bool autoremove, bool inproc)
        {
            GetProvider(inproc).Add(key, fielname, resolver, expire, autoremove);
        }

        /// <summary>
        /// 将文件映射对象放入全局缓存中
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="fielname">文件名称</param>
        /// <param name="resolver">文件解析</param>
        /// <param name="expire">滑动到期时间</param>
        /// <param name="autoremove">是否可被系统自动回收</param>
        /// <param name="mode">缓存模式</param>
        public static void Add(string key, string fielname, Func<byte[], object> resolver, TimeSpan expire, bool autoremove, bool inproc)
        {
            GetProvider(inproc).Add(key, fielname, resolver, expire, autoremove);
        }

        /// <summary>
        /// 获取全局缓存
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static object Get(string key, bool inproc)
        {
            return GetProvider(inproc).Get(key);
        }
        /// <summary>
        /// 获取全局缓存
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public static T Get<T>(string key, bool inproc)
        {
            var value = Get(key, inproc);
            if (value == null)
                return default(T);
            return (T)value;
        }

        /// <summary>
        /// 删除全局缓存
        /// </summary>
        /// <param name="key"></param>
        public static void Remove(string key, bool inproc)
        {
            GetProvider(inproc).Remove(key);
        }
    }
}
