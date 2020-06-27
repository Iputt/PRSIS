using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Owl.Common
{
    /// <summary>
    /// 有序的字典，用于解决原始的Dictionary在元素删除后，新添加的元素顺序混乱的问题
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public class OrderlyDict<TKey, TValue> : IDictionary<TKey, TValue>
    {
        protected Dictionary<TKey, TValue> innerdict;
        protected List<TKey> keys = new List<TKey>();

        public OrderlyDict()
        {
            innerdict = new Dictionary<TKey, TValue>();
        }
        public OrderlyDict(int capacity)
        {
            innerdict = new Dictionary<TKey, TValue>(capacity);
        }

        public TValue this[TKey key]
        {
            get => innerdict[key];
            set => Add(key, value);
        }

        public ICollection<TKey> Keys => keys;

        public ICollection<TValue> Values => keys.Select(s => innerdict[s]).ToArray();

        public int Count => keys.Count;

        public bool IsReadOnly => false;

        public void Add(TKey key, TValue value)
        {
            if (!innerdict.ContainsKey(key))
                keys.Add(key);
            innerdict[key] = value;
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        public void Clear()
        {
            keys.Clear();
            innerdict.Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return innerdict.Contains(item);
        }

        public bool ContainsKey(TKey key)
        {
            return innerdict.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            for (var i = arrayIndex; i < keys.Count; i++)
            {
                var key = keys[i];
                array[i - arrayIndex] = new KeyValuePair<TKey, TValue>(key, innerdict[key]);
            }
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            foreach (var key in keys)
            {
                yield return new KeyValuePair<TKey, TValue>(key, innerdict[key]);
            }
        }

        public bool Remove(TKey key)
        {
            if (innerdict.ContainsKey(key))
            {
                innerdict.Remove(key);
                keys.Remove(key);
                return true;
            }
            return false;
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            if (innerdict.Contains(item))
            {
                innerdict.Remove(item.Key);
                keys.Remove(item.Key);
                return true;
            }
            return false;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return innerdict.TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
