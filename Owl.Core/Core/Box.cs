using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System
{
    /// <summary>
    /// 值包装
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Box<T>
    {
        /// <summary>
        /// 包装的值
        /// </summary>
        public T Value { get; set; }

        public Box() { }

        public Box(T value)
        {
            Value = value;
        }
    }
}
