using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System
{
    /// <summary>
    /// 键值对
    /// </summary>
    public class KeyValuePair
    {
        /// <summary>
        /// 键
        /// </summary>
        public string Key { get; private set; }

        /// <summary>
        /// 值
        /// </summary>
        public object Value { get; private set; }

        public KeyValuePair(string key, object value)
        {
            Key = key;
            Value = value;
        }
    }
}
