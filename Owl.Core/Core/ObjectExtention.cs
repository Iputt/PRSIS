using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace System
{
    public static class ObjectExt
    {
        static readonly Dictionary<string, MethodInfo> methods = new Dictionary<string, MethodInfo>(10);
        static ObjectExt()
        {
            foreach (var method in typeof(ObjectExt).GetMethods())
            {
                if (method.Name == "In" && method.GetParameters()[1].ParameterType.IsArray)
                    continue;
                if (!method.IsStatic)
                    continue;
                methods[method.Name] = method;
            }
        }
        public static MethodInfo GetMethod(string methodname)
        {
            if (methods.ContainsKey(methodname))
                return methods[methodname];
            return null;
        }
        public static bool In<TProperty>(this TProperty obj, IEnumerable<TProperty> objs)
        {
            return In(obj, objs.ToArray());
        }

        public static bool In<TProperty>(this TProperty obj, params TProperty[] objs)
        {
            foreach (var o in objs)
            {
                if (Object.Equals(o, obj))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 将源字符串作为以 , 号分割的数组，判断目标字符串是否被包含在次数组中
        /// </summary>
        /// <param name="org">来源字符串</param>
        /// <param name="dest"></param>
        /// <returns></returns>
        public static bool ArrayContains(this string org, string dest)
        {
            if (string.IsNullOrEmpty(org) || string.IsNullOrEmpty(dest))
                return false;
            return org.Split(',').Contains(dest);
        }

        /// <summary>
        /// 字符串比较 a>b
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool GreaterThan(this string a, string b)
        {
            return string.Compare(a, b, true) > 0;
        }
        /// <summary>
        /// 字符串比较 a>=b
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool GreaterThanOrEqual(this string a, string b)
        {
            return string.Compare(a, b, true) >= 0;
        }
        /// <summary>
        /// 字符串比较 a<b
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool LessThan(this string a, string b)
        {
            return string.Compare(a, b) < 0;
        }
        /// <summary>
        /// 字符串比较 a<=b
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool LessThanOrEqual(this string a, string b)
        {
            return string.Compare(a, b) <= 0;
        }
        /// <summary>
        /// 转为字典，若key重复则用最后匹配的值
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="source"></param>
        /// <param name="keySelector"></param>
        /// <returns></returns>
        public static Dictionary<TKey, TSource> xToDictionary<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            Dictionary<TKey, TSource> dict = new Dictionary<TKey, TSource>();
            foreach (var value in source)
            {
                var key = keySelector(value);
                if (key !=null && !dict.ContainsKey(key))
                    dict[key] = value;
            }
            return dict;
        }
        /// <summary>
        /// 转为字典，若key重复则用最后匹配的值
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TElement"></typeparam>
        /// <param name="source"></param>
        /// <param name="keySelector"></param>
        /// <param name="elementSelector"></param>
        /// <returns></returns>
        public static Dictionary<TKey, TElement> xToDictionary<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector)
        {
            Dictionary<TKey, TElement> dict = new Dictionary<TKey, TElement>();
            foreach (var value in source)
            {
                var key = keySelector(value);
                if (!dict.ContainsKey(key))
                    dict[key] = elementSelector(value);
            }
            return dict;
        }

        public static int Compare(object obj1, object obj2)
        {
            if (obj1 == null && obj2 == null)
                return 0;
            if (obj1 == null)
                return -1;
            if (obj2 == null)
                return 1;
            if (obj1 is string)
                return string.Compare(obj1 as string, obj2.ToString());

            if (obj1 is bool && obj2 is bool)
                return (bool)obj1 == (bool)obj2 ? 0 : -1;
            if (obj1 is Guid && obj2 is Guid)
                return (Guid)obj1 == (Guid)obj2 ? 0 : -1;

            var type1 = obj1.GetType();
            var type2 = obj2.GetType();
            if (type1 != type2)
                obj2 = Owl.Util.Convert2.ChangeType(obj2, type1);
            if (object.Equals(obj1, obj2))
                return 0;
            if (obj1 is bool)
                return (bool)obj1 == (bool)obj2 ? 0 : -1;
            if (obj1 is Guid)
                return (Guid)obj1 == (Guid)obj2 ? 0 : -1;

            return (dynamic)obj1 > (dynamic)obj2 ? 1 : -1;
        }
        /// <summary>
        /// 切片
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="source"></param>
        /// <param name="step"></param>
        /// <returns></returns>
        public static IEnumerable<TSource>[] Slice<TSource>(this IEnumerable<TSource> source, int offset, int step)
        {
            var tmp = source.Skip(offset).ToList();
            List<List<TSource>> result = new List<List<TSource>>();
            List<TSource> current = null;
            for (int i = 0; i < tmp.Count; i++)
            {
                if (i % step == 0)
                {
                    current = new List<TSource>();
                    result.Add(current);
                }
                current.Add(tmp[i]);
            }
            return result.ToArray();
        }
    }
}

