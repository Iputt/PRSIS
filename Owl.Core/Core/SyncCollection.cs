using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Threading;
using System.Collections.ObjectModel;
using Owl.Util;
using Owl.Feature;
namespace System
{
    #region SyncDictionary
    /// <summary>
    /// 同步字典
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    [Serializable]
    public class SyncDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        public SyncDictionary() : this(100) { }

        public SyncDictionary(int capacity)
        {
            dict = new Dictionary<TKey, TValue>(capacity);
        }

        Dictionary<TKey, TValue> dict;

        ReaderWriterLockSlim synclock = Locker.Create();

        /// <summary>
        /// 锁定写入并返回解锁对象
        /// </summary>
        /// <returns></returns>
        public IDisposable Lock()
        {
            return Locker.LockWrite(synclock);
        }

        public void Add(TKey key, TValue value)
        {
            using (Locker.LockWrite(synclock))
            {
                dict.Add(key, value);
            }
        }

        public bool ContainsKey(TKey key)
        {
            if (key == null)
                return false;
            using (Locker.LockRead(synclock))
            {
                return dict.ContainsKey(key);
            }
        }

        public ICollection<TKey> Keys
        {
            get
            {
                using (Locker.LockRead(synclock))
                {
                    return dict.Keys;
                }
            }
        }

        public bool Remove(TKey key)
        {
            using (Locker.LockWrite(synclock))
            {
                return dict.Remove(key);
            }
        }
        /// <summary>
        /// 获取字典的值，如果值不存在，default(TValue)
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public TValue Get(TKey key)
        {
            using (Locker.LockRead(synclock))
            {
                if (dict.ContainsKey(key))
                    return dict[key];
            }
            return default(TValue);
        }
        /// <summary>
        /// 安全设置字典的值
        /// </summary>
        /// <param name="key"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public TValue Set(TKey key, Func<TValue> func)
        {
            using (Locker.LockWrite(synclock))
            {
                if (!dict.ContainsKey(key))
                {
                    var value = func();
                    dict[key] = value;
                    return value;
                }
                return dict[key];
            }
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            using (Locker.LockRead(synclock))
            {
                return dict.TryGetValue(key, out value);
            }
        }

        public ICollection<TValue> Values
        {
            get
            {
                using (Locker.LockRead(synclock))
                {
                    return dict.Values;
                }
            }
        }

        public TValue this[TKey key]
        {
            get
            {
                using (Locker.LockRead(synclock))
                {
                    return dict[key];
                }
            }
            set
            {
                using (Locker.LockWrite(synclock))
                {
                    dict[key] = value;
                }
            }
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        public void Clear()
        {
            using (Locker.LockWrite(synclock))
            {
                dict.Clear();
            }
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return ContainsKey(item.Key);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            using (Locker.LockRead(synclock))
            {
                ((ICollection<KeyValuePair<TKey, TValue>>)dict).CopyTo(array, arrayIndex);
            }
        }

        public int Count
        {
            get
            {
                using (Locker.LockRead(synclock))
                {
                    return dict.Count;
                }
            }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return Remove(item.Key);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            using (Locker.LockRead(synclock))
            {
                return dict.GetEnumerator();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
    #endregion

    #region SyncList
    /// <summary>
    /// 同步List
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SyncList<T> : IList<T>
    {

        List<T> value = new List<T>();

        ReaderWriterLockSlim synclock = Locker.Create();

        /// <summary>
        /// 锁定写入并返回解锁对象
        /// </summary>
        /// <returns></returns>
        public IDisposable Lock()
        {
            return Locker.LockWrite(synclock);
        }

        public int IndexOf(T item)
        {
            using (Locker.LockRead(synclock))
            {
                return value.IndexOf(item);
            }
        }

        public void Insert(int index, T item)
        {
            using (Locker.LockWrite(synclock))
            {
                value.Insert(index, item);
            }
        }

        public void RemoveAt(int index)
        {
            using (Locker.LockWrite(synclock))
            {
                value.RemoveAt(index);
            }
        }

        public T this[int index]
        {
            get
            {
                using (Locker.LockRead(synclock))
                {
                    return value[index];
                }
            }
            set
            {
                using (Locker.LockWrite(synclock))
                {
                    this.value[index] = value;
                }
            }
        }

        public void Add(T item)
        {
            using (Locker.LockWrite(synclock))
            {
                value.Add(item);
            }
        }

        public void Clear()
        {
            using (Locker.LockWrite(synclock))
                value.Clear();
        }

        public bool Contains(T item)
        {
            using (Locker.LockRead(synclock))
                return value.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            using (Locker.LockRead(synclock))
                value.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get
            {
                using (Locker.LockRead(synclock))
                    return value.Count;
            }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(T item)
        {
            using (Locker.LockWrite(synclock))
                return value.Remove(item);
        }

        public IEnumerator<T> GetEnumerator()
        {
            using (Locker.LockRead(synclock))
                return value.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
    #endregion
}
