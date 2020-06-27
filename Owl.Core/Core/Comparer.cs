using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Util;
namespace System.Collections.Generic
{
    /// <summary>
    /// Owl通用比较器
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Comparer2<T> : Comparer<T>
    {
        protected Func<T, T, int> m_func;
        private Comparer2(Func<T, T, int> func)
        {
            m_func = func;
        }
        /// <summary>
        /// 顺序排序比较器
        /// </summary>
        /// <typeparam name="TProperty"></typeparam>
        /// <param name="property"></param>
        /// <returns></returns>
        public static Comparer<T> Asc<TProperty>(Func<T, TProperty> property)
        {
            return new Comparer2<T>((x, y) =>
            {
                var p1 = property(x);
                var p2 = property(y);
                return ObjectExt.Compare(p1, p2);
            });
        }

        /// <summary>
        /// 逆序排序比较器
        /// </summary>
        /// <typeparam name="TProperty"></typeparam>
        /// <param name="property"></param>
        /// <returns></returns>
        public static Comparer<T> Desc<TProperty>(Func<T, TProperty> property)
        {
            return new Comparer2<T>((x, y) =>
            {
                var p1 = property(x);
                var p2 = property(y);
                return -ObjectExt.Compare(p1, p2);
            });
        }

        /// <summary>
        /// 创建比较器
        /// </summary>
        /// <param name="comparer"></param>
        /// <returns></returns>
        public static Comparer<T> Create(Func<T, T, int> comparer)
        {
            return new Comparer2<T>(comparer);
        }

        public override int Compare(T x, T y)
        {
            return m_func(x, y);
        }
    }
}
