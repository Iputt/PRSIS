using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Collections;

namespace Owl.Feature.Impl.Cache
{
    /// <summary>
    /// 缓存项
    /// </summary>
    public class CacheItem
    {
        /// <summary>
        /// 值
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// 到期时间
        /// </summary>
        public DateTime? ExpireTime { get; private set; }

        ReaderWriterLockSlim m_locker;
        /// <summary>
        /// 缓存项的锁用于缓存项为集合时
        /// </summary>
        public ReaderWriterLockSlim Locker
        {
            get
            {
                if (m_locker == null)
                    m_locker = Feature.Locker.Create();
                return m_locker;
            }
        }

        /// <summary>
        ///  判断当前时间是否可访问
        /// </summary>
        /// <param name="time">访问时间</param>
        /// <returns></returns>
        public bool Valid(DateTime? time = null)
        {
            if (time == null)
                time = DateTime.Now;
            return ExpireTime == null || (ExpireTime.HasValue && ExpireTime >= time);
        }

        public void ResetExpire(TimeSpan? expire)
        {
            if (expire == null)
                ExpireTime = null;
            else
                ExpireTime = DateTime.Now.AddTicks(expire.Value.Ticks);
        }

        public CacheItem(object value, TimeSpan? expire)
        {
            Value = value;
            ResetExpire(expire);
        }
    }

    /// <summary>
    /// 进程内缓存
    /// </summary>
    public class InnerCache : ICache
    {
        Dictionary<string, CacheItem> m_items = new Dictionary<string, CacheItem>();
        Timer m_timer;
        ReaderWriterLockSlim m_locker;
        public InnerCache()
        {
            m_locker = Feature.Locker.Create();
            m_timer = new Timer(Collect, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
        }
        ~InnerCache()
        {
            m_timer.Dispose();
            m_locker.Dispose();
        }
        /// <summary>
        /// 整理缓存项
        /// </summary>
        /// <param name="state"></param>
        void Collect(object state)
        {
            Collect(DateTime.Now);
        }
        /// <summary>
        /// 清除过期的缓存项
        /// </summary>
        /// <param name="time"></param>
        void Collect(DateTime time)
        {
            var items = m_items.ToList();
            if (items.Count == 0)
                return;
            using (Feature.Locker.LockWrite(m_locker))
            {
                foreach (var pair in items)
                {
                    if (!pair.Value.Valid(time))
                        m_items.Remove(pair.Key);
                }
            }
        }

        /// <summary>
        /// 更新缓存的过期时间
        /// </summary>
        /// <param name="key"></param>
        /// <param name="expire"></param>
        public void KeyExpire(string key, TimeSpan? expire)
        {
            using (Feature.Locker.LockRead(m_locker))
            {
                if (m_items.ContainsKey(key) && m_items[key].Valid())
                {
                    m_items[key].ResetExpire(expire);
                }
            }
        }
        /// <summary>
        /// 从缓存中删除Key
        /// </summary>
        /// <param name="key"></param>
        public void KeyRemove(string key)
        {
            using (Feature.Locker.LockWrite(m_locker))
            {
                if (m_items.ContainsKey(key))
                    m_items.Remove(key);
            }
        }

        public bool KeyExists(string key)
        {
            using (Feature.Locker.LockRead(m_locker))
            {
                return m_items.ContainsKey(key) ? m_items[key].Valid() : false;
            }
        }

        #region 单个对象缓存
        public void Set(string key, object value, TimeSpan? expire = null)
        {
            using (Feature.Locker.LockWrite(m_locker))
            {
                var now = DateTime.Now;
                if (m_items.ContainsKey(key))
                {
                    var item = m_items[key];
                    item.Value = value;
                    item.ResetExpire(expire);
                }
                else
                    m_items[key] = new CacheItem(value, expire);
            }
        }

        public object Get(string key)
        {
            using (Feature.Locker.LockRead(m_locker))
            {
                if (m_items.ContainsKey(key))
                {
                    var item = m_items[key];
                    if (item.Valid())
                        return item.Value;
                }
                return null;
            }
        }
        public bool SetNE(string key, object value, TimeSpan? expire = null)
        {
            using (Feature.Locker.LockWrite(m_locker))
            {
                CacheItem item = null;
                if (m_items.ContainsKey(key))
                {
                    item = m_items[key];
                    if (item.Valid(DateTime.Now))
                        return false;
                }
                if (item == null)
                {
                    item = new CacheItem(value, expire);
                    m_items[key] = item;
                }
                else
                {
                    item.Value = value;
                    item.ResetExpire(expire);
                }
                return true;
            }
        }

        public object GetSet(string key, object value)
        {
            using (Feature.Locker.LockWrite(m_locker))
            {
                CacheItem item = null;
                if (m_items.ContainsKey(key))
                {
                    item = m_items[key];
                    if (!item.Valid())
                        item = null;
                }
                object old = null;
                if (item == null)
                    item = new CacheItem(value, null);
                else
                {
                    old = item.Value;
                    item.Value = value;
                }
                return old;
            }
        }
        #endregion

        Tuple<T, ReaderWriterLockSlim> GetCollect<T>(string key)
            where T : class, new()
        {
            using (Feature.Locker.LockWrite(m_locker))
            {
                CacheItem item = null;
                if (m_items.ContainsKey(key))
                {
                    item = m_items[key];
                    if (!item.Valid())
                        item = null;
                }
                if (item == null)
                {
                    item = new CacheItem(new T(), null);

                    m_items[key] = item;
                }
                return new Tuple<T, ReaderWriterLockSlim>((T)item.Value, item.Locker); ;
            }
        }

        #region hash对象缓存

        /// <summary>
        /// hash表设置
        /// </summary>
        /// <param name="key"></param>
        /// <param name="field"></param>
        /// <param name="value"></param>
        public void HashSet(string key, string field, object value)
        {
            var hs = GetCollect<Hashtable>(key);
            using (Feature.Locker.LockWrite(hs.Item2))
            {
                hs.Item1[field] = value;
            }
        }
        public object HashGet(string key, string field)
        {
            var hs = GetCollect<Hashtable>(key);
            using (Feature.Locker.LockRead(hs.Item2))
            {
                return hs.Item1[field];
            }
        }

        public T HashGet<T>(string key, string field)
        {
            var hs = GetCollect<Hashtable>(key);
            using (Feature.Locker.LockRead(hs.Item2))
            {
                var value = hs.Item1[field];
                if (value != null)
                    return (T)value;
                return default(T);
            }
        }

        public void HashDelete(string key, string field, bool async)
        {
            var hs = GetCollect<Hashtable>(key);
            using (Feature.Locker.LockRead(hs.Item2))
            {
                hs.Item1.Remove(field);
            }
        }

        public Hashtable HashGetAll(string key)
        {
            return GetCollect<Hashtable>(key).Item1;
        }

        #endregion

        #region 增量缓存
        public long Increment(string key, long value = 1)
        {
            using (Feature.Locker.LockWrite(m_locker))
            {
                var now = DateTime.Now;
                CacheItem item = null;
                if (m_items.ContainsKey(key))
                {
                    item = m_items[key];
                    if (item.Valid(now))
                    {
                        item.Value = (long)item.Value + value;
                    }
                    else
                        item = null;
                }
                if (item == null)
                {
                    item = new CacheItem(value, null);
                    m_items[key] = item;
                }
                return (long)item.Value;
            }
        }
        #endregion

        #region list
        public void ListLeftPush(string key, params object[] value)
        {
            var array = GetCollect<ArrayList>(key);
            using (Feature.Locker.LockWrite(array.Item2))
            {
                array.Item1.InsertRange(0, value.Reverse().ToArray());
            }
        }

        public object ListLeftPop(string key)
        {
            var item = GetCollect<ArrayList>(key);
            var array = item.Item1;
            using (Feature.Locker.LockWrite(item.Item2))
            {
                if (array.Count > 0)
                {
                    var obj = array[0];
                    array.RemoveAt(0);
                    return obj;
                }
                return null;
            }
        }

        public void ListRightPush(string key, params object[] value)
        {
            var array = GetCollect<ArrayList>(key);
            using (Feature.Locker.LockWrite(array.Item2))
            {
                array.Item1.AddRange(value);
            }
        }

        public object ListRightPop(string key)
        {
            var item = GetCollect<ArrayList>(key);
            var array = item.Item1;
            using (Feature.Locker.LockWrite(item.Item2))
            {
                if (array.Count > 0)
                {
                    var obj = array[array.Count - 1];
                    array.RemoveAt(array.Count - 1);
                    return obj;
                }
                return null;
            }
        }

        public IEnumerable<object> ListRange(string key, int start = 0, int end = -1)
        {
            var item = GetCollect<ArrayList>(key);
            var array = item.Item1;
            if (end < 0 || end > array.Count - 1)
                end = array.Count - 1;
            var count = end - start + 1;
            object[] result = new object[count];
            array.CopyTo(0, result, start, count);
            return result;
        }
        #endregion
    }
}
