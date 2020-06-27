using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Reflection;
namespace Owl.Util
{
    /// <summary>
    /// 枚举帮助类
    /// </summary>
    public static class EnumHelper
    {
        /// <summary>
        /// 分离枚举类型
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static List<string> Split(this Enum value)
        {
            var result = new List<string>();
            var type = value.GetType();
            if (Enum.IsDefined(type, value))
                result.Add(value.ToString());
            else
            {
                var s = Convert.ToInt32(value);
                foreach (var tmp in Enum.GetValues(type))
                {
                    var ts = Convert.ToInt32(tmp);
                    if (s != 0 && ts == 0)
                        continue;
                    if ((s & ts) == ts)
                        result.Add(tmp.ToString());
                }
            }
            return result;
        }
        /// <summary>
        /// 将枚举转为字符串
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ToString(Enum value)
        {
            return string.Join(",", Split(value));
        }

        static object DoParse(Type enumType, string value)
        {
            var tmp = value.TrySplit(',');
            if (tmp.Length == 1)
                return Enum.Parse(enumType, tmp[0], true);
            int result = 0;
            foreach (var v in tmp)
            {
                result = result | (int)Enum.Parse(enumType, v, true);
            }
            return result;
        }
        static MethodInfo ParseGenricMethod = typeof(EnumHelper).GetMethod("Parse", new Type[] { typeof(string) });
        static Dictionary<Type, MethodInfo> ParseMethods = new Dictionary<Type, MethodInfo>();
        static MethodInfo GetParseMethod(Type enumtype)
        {
            if (ParseMethods.ContainsKey(enumtype))
                return ParseMethods[enumtype];
            var mehtod = ParseGenricMethod.MakeGenericMethod(enumtype);
            ParseMethods[enumtype] = mehtod;
            return mehtod;
        }
        /// <summary>
        /// 解析
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static T Parse<T>(string value)
            where T : struct
        {
            T result;
            Enum.TryParse<T>(value, true, out result);
            return result;
            //var enumType = typeof(T);
            //var tmp = value.TrySplit(',');
            //if (tmp.Length == 1)
            //    return (T)Enum.Parse(enumType, tmp[0], true);
            //int result = 0;
            //foreach (var v in tmp)
            //{
            //    result = result | (int)Enum.Parse(enumType, v, true);
            //}
            //return (T)((object)result);
            //return (T)Parse(typeof(T), value);
        }

        /// <summary>
        /// 解析
        /// </summary>
        /// <param name="enumType"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static object Parse(Type enumType, string value)
        {
            return GetParseMethod(enumType).FaseInvoke(null, value);
        }

        public static bool Contain(this Enum org, Enum dest)
        {
            var iorg = Convert.ToInt32(org);
            var idest = Convert.ToInt32(dest);
            return (iorg & idest) == idest;
        }
    }
}
