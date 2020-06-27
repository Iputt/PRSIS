using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Runtime.Serialization;
using System.Collections;
using System.Collections.Concurrent;
using System.Globalization;
using Owl.Domain;
namespace System
{
    /// <summary>
    /// 忽略字段，反射时将忽略此字段
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class IgnoreFieldAttribute : Attribute
    {

    }
}
namespace Owl.Util
{
    /// <summary>
    /// 类型相关帮工具
    /// </summary>
    public static class TypeHelper
    {
        #region 类型相关
        private static readonly HashSet<Type> _numericTypes = new HashSet<Type>() {
            typeof(byte), typeof(sbyte),
            typeof(short), typeof(ushort),
            typeof(int), typeof(uint),
            typeof(long), typeof(ulong),
            typeof(float), typeof(double), typeof(decimal)
        };

        private static readonly HashSet<Type> _numberTypes = new HashSet<Type>() {
            typeof(float), typeof(double), typeof(decimal)
        };
        private static readonly HashSet<Type> _digitTypes = new HashSet<Type>() {
            typeof(byte), typeof(sbyte),
            typeof(short), typeof(ushort),
            typeof(int), typeof(uint),
            typeof(long), typeof(ulong)
        };

        /// <summary>
        /// 获取类型的实际类型，比如 bool? 返回 bool
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Type StripType(Type type)
        {
            if (type.Name == "Nullable`1")
                return Nullable.GetUnderlyingType(type);
            return type;
        }

        /// <summary>
        /// 判断类型是否为数值型
        /// </summary>
        /// <param name="type">类型</param>
        /// <param name="striped">是否已剥除空类型</param>
        /// <returns></returns>
        public static bool IsNumeric(Type type, bool striped = false)
        {
            return _numericTypes.Contains(striped ? type : StripType(type));
        }
        /// <summary>
        /// 判断值是否为数值型
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool IsNumeric(object value)
        {
            if (value == null)
                return false;
            return IsNumeric(value.GetType());
        }
        /// <summary>
        /// 判断类型是否为整型
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsDigit(Type type)
        {
            return _digitTypes.Contains(StripType(type));
        }

        /// <summary>
        /// 判断值是否为整数
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool IsDigit(object value)
        {
            if (value == null)
                return false;
            return IsDigit(value.GetType());
        }
        /// <summary>
        /// 判断类型是否为浮点数
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsFloat(Type type)
        {
            return _numberTypes.Contains(StripType(type));
        }

        /// <summary>
        /// 判断类型是否为值类型，包含string
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsValueType(Type type)
        {
            type = StripType(type);
            return type.IsValueType || type.Name == "String";
        }

        /// <summary>
        /// 判断类型是否可为空
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsNullable(Type type)
        {
            return type.Name == "Nullable`1" || !type.IsValueType;
        }

        public static bool IsArray(Type type)
        {
            type = StripType(type);

            return type.IsArray || (type.Name != "String" && type.GetInterface("IEnumerable") != null);
        }

        public static bool IsArray(object value)
        {
            if (value == null)
                return false;
            return IsArray(value.GetType());
        }

        /// <summary>
        /// 判断类型是否可以互转
        /// </summary>
        /// <param name="type1"></param>
        /// <param name="type2"></param>
        /// <returns></returns>
        static bool CanConvert(Type type1, Type type2)
        {
            if (type2 == null)
            {
                if (Nullable.GetUnderlyingType(type1) != null)
                    return true;
            }
            else
            {
                if (type1 == type2 || Nullable.GetUnderlyingType(type1) == type2)
                    return true;
                type1 = Nullable.GetUnderlyingType(type1) ?? type1;
                if ((type2 == typeof(int) && IsDigit(type1)) || (type2 == typeof(double) && IsFloat(type1)))
                {
                    return true;
                }
            }
            return false;
        }
        #endregion

        #region  反射相关
        private static readonly ConcurrentDictionary<Type, Dictionary<string, PropertyInfo>> m_propterties = new ConcurrentDictionary<Type, Dictionary<string, PropertyInfo>>();
        private static readonly ConcurrentDictionary<Type, Dictionary<string, List<MethodInfo>>> m_methods = new ConcurrentDictionary<Type, Dictionary<string, List<MethodInfo>>>();
        private static readonly ConcurrentDictionary<Type, Dictionary<string, List<MemberInfo>>> m_static_members = new ConcurrentDictionary<Type, Dictionary<string, List<MemberInfo>>>();
        /// <summary>
        /// 获取指定类型的有效字段
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Dictionary<string, PropertyInfo> GetProperties(Type type)
        {
            if (type == null)
                return null;
            return m_propterties.GetOrAdd(type, t =>
            {
                var pinfos = t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                        .Where(s => s.GetCustomAttributes(typeof(IgnoreFieldAttribute), true).Length == 0).ToList();
                var infos = new Dictionary<string, PropertyInfo>(pinfos.Count);
                foreach (var info in pinfos)
                {
                    infos[info.Name] = info;
                }
                return infos;
            });
        }

        /// <summary>
        /// 获取指定类型的实例方法
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Dictionary<string, List<MethodInfo>> GetMethods(Type type)
        {
            return m_methods.GetOrAdd(type, t =>
            {
                return t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GroupBy(s => s.Name).ToDictionary(s => s.Key, s => s.ToList());
            });
        }
        /// <summary>
        /// 获取指定类型的方法
        /// </summary>
        /// <param name="type"></param>
        /// <param name="methodname"></param>
        /// <param name="argtypes"></param>
        /// <returns></returns>
        public static MethodInfo GetMethod(Type type, string methodname, params Type[] argtypes)
        {
            var methods = GetMethods(type);
            if (methods.ContainsKey(methodname))
            {
                foreach (var method in methods[methodname])
                {
                    var parameters = method.GetParameters();
                    var ismatch = true;
                    if (parameters.Length == argtypes.Length)
                    {
                        for (var i = 0; i < parameters.Length; i++)
                        {
                            if (parameters[i].ParameterType != argtypes[i])
                            {
                                ismatch = false;
                                continue;
                            }
                        }
                    }
                    if (ismatch)
                        return method;
                }
            }
            return null;
        }

        /// <summary>
        /// 获取类型的静态成员
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Dictionary<string, List<MemberInfo>> GetStaticMembers(Type type)
        {
            if (type == null)
                return null;
            return m_static_members.GetOrAdd(type, t =>
            {
                return t.GetMembers(BindingFlags.Static | BindingFlags.Public).GroupBy(s => s.Name).ToDictionary(s => s.Key, s => s.ToList());
            });
        }


        /// <summary>
        /// 获取对象的静态字段或属性
        /// </summary>
        /// <param name="type">类型</param>
        /// <param name="member">成员名称</param>
        /// <returns></returns>
        public static MemberInfo GetStaticMember(Type type, string member)
        {
            var members = GetStaticMembers(type);
            if (members != null && members.ContainsKey(member))
                return members[member].FirstOrDefault();
            return null;
        }
        /// <summary>
        /// 获取类型的静态方法
        /// </summary>
        /// <param name="type"></param>
        /// <param name="methodname"></param>
        /// <returns></returns>
        public static IEnumerable<MethodInfo> GetStaticMethods(Type type, string methodname)
        {
            var members = GetStaticMembers(type);
            if (members != null && members.ContainsKey(methodname))
                return members[methodname].OfType<MethodInfo>();
            return null;
        }
        /// <summary>
        /// 获取构造函数
        /// </summary>
        /// <param name="type"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static ConstructorInfo GetConstructor(Type type, Type[] parameters)
        {
            ConstructorInfo constructor = null;
            foreach (var cons in type.GetConstructors())
            {
                var paras = cons.GetParameters();
                if (paras.Length != parameters.Length)
                    continue;
                for (int i = 0; i < paras.Length; i++)
                {
                    if (!CanConvert(paras[i].ParameterType, parameters[i]))
                        break;
                }
                constructor = cons;
                break;
            }
            return constructor;
        }

        static readonly Dictionary<Type, MethodInfo> m_parsemethods = new Dictionary<Type, MethodInfo>(15);
        /// <summary>
        /// 获取值类型的字符串解析函数
        /// </summary>
        /// <param name="type">值类型</param>
        /// <returns></returns>
        public static MethodInfo GetParseMethod(Type valuetype)
        {
            if (valuetype == null || (!valuetype.IsValueType && !Parser.ContainsKey(valuetype)))
                return null;
            valuetype = StripType(valuetype);

            MethodInfo result = null;
            if (!m_parsemethods.ContainsKey(valuetype))
            {
                if (IsNumeric(valuetype))
                    result = valuetype.GetMethod("Parse", new Type[] { typeof(string), typeof(NumberStyles) });
                else if (valuetype.IsEnum)
                    result = typeof(EnumHelper).GetMethod("Parse", new Type[] { typeof(Type), typeof(string) });
                else
                {
                    result = valuetype.GetMethod("Parse", new Type[] { typeof(string) });
                    if (result == null)
                        result = typeof(TypeHelper).GetMethod("Parse");
                }
                    
                m_parsemethods[valuetype] = result;
                return result;
            }
            return m_parsemethods[valuetype];
        }
        static Dictionary<Type, Func<string, object>> Parser = new Dictionary<Type, Func<string, object>>()
        {
            {
                typeof(Guid),
                s=>Guid.Parse(s)
            },
            {
                typeof(short),
                s=>short.Parse(s, NumberStyles.Any)
            },
            {
                typeof(ushort),
                s=>ushort.Parse(s, NumberStyles.Any)
            },
            {
                typeof(int),
                s=>int.Parse(s, NumberStyles.Any)
            },
            {
                typeof(uint),
                s=>uint.Parse(s, NumberStyles.Any)
            },
            {
                typeof(long),
                s=>long.Parse(s, NumberStyles.Any)
            },
            {
                typeof(ulong),
                s=>ulong.Parse(s, NumberStyles.Any)
            },
            {
                typeof(float),
                s=>float.Parse(s, NumberStyles.Any)
            },
            {
                typeof(double),
                s=>double.Parse(s, NumberStyles.Any)
            },
            {
                typeof(decimal),
                s=>decimal.Parse(s, NumberStyles.Any)
            },
            {
                typeof(DateTime),
                s=>
                {
                    var value = s;
                    if (value.Contains("("))
                    {
                        value = value.Substring(value.IndexOf("(")+1, 13);
                        return new DateTime(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks + long.Parse(value) * 10000).ToLocalTime();
                    }
                    else
                        return DateTime.Parse(value);
                }
            },
            {
                typeof(DateMonth),
                s=>DateMonth.Parse(s)
            },
            {
                typeof(byte[]),
                s=>
                {
                    return Convert.FromBase64String(s);
                }
            }
        };
        /// <summary>
        /// 解析字符串
        /// </summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static object Parse(Type type, string value)
        {
            if (type.IsEnum)
                return EnumHelper.Parse(type, value);
            if (Parser.ContainsKey(type))
                return Parser[type](value);
            return Convert.ChangeType(value, type);
        }

        #endregion

        #region 加载数据与类型
        static bool isSubclassOf(Type sub, Type basecls)
        {
            if (basecls.IsInterface)
                return sub.GetInterfaces().Any(s => s == basecls);
            return sub.IsSubclassOf(basecls);
        }

        /// <summary>
        /// 从指定程序集中加载
        /// </summary>
        /// <typeparam name="T">基类类型</typeparam>
        /// <param name="assembly">程序集</param>
        /// <returns></returns>
        public static IEnumerable<T> LoadFromAsm<T>(Assembly assembly)
            where T : class
        {
            List<T> providers = new List<T>();

            foreach (var type in assembly.GetTypes())
            {
                if (isSubclassOf(type, typeof(T)) && !type.IsAbstract && !type.IsGenericType)
                {
                    try
                    {
                        providers.Add(Activator.CreateInstance(type) as T);
                    }
                    catch
                    {

                    }
                }
            }

            return providers;
        }
        /// <summary>
        /// 从程序集中加载相关类型
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="assembly">程序集</param>
        /// <returns></returns>
        public static IEnumerable<Type> LoadTypeFromAsm<T>(Assembly assembly)
        {
            List<Type> types = new List<Type>();

            foreach (var type in assembly.GetTypes())
            {
                if (isSubclassOf(type, typeof(T)) && !type.IsAbstract)
                {
                    types.Add(type);
                }
            }

            return types;
        }

        #endregion

        /// <summary>
        /// 获取元数据名称
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string MetaName(this Type type)
        {
            //var ObjType = DomainHelper.GetDomainType(type);
            //if (ObjType == DomainType.Handler)
            //{
            //    var attr = Attrs.OfType<MsgRegisterAttribute>().FirstOrDefault();
            //    if (attr != null)
            //    {
            //        var gtype = TypeHelper.GetBaseGenericType(type, typeof(MsgHandler));
            //        if (gtype != null)
            //            return string.Format("{0}.{1}", gtype.MetaName(), attr.Name);
            //    }
            //}
            return type.FullName.Replace("+", ".").ToLower();
        }

        public static string GetTypeName(this Type type)
        {
            var fullname = type.FullName;
            if (type.IsGenericType)
            {
                fullname = fullname.Split('`')[0];
                fullname = string.Format("{0}[{1}]", fullname, string.Join(",", type.GetGenericArguments().Select(s => s.Name)));
            }
            return fullname;
        }

        /// <summary>
        /// 获取基类的聚合根类型(AggRoot)的范型参数
        /// </summary>
        /// <param name="type">当前类型</param>
        /// <param name="orgtype">原始基类</param>
        /// <returns></returns>
        public static Type GetBaseGenericType(Type type, Type orgtype)
        {
            var basetype = type.BaseType;
            if (basetype.IsSubclassOf(orgtype))
            {
                if (basetype.IsGenericType)
                {
                    var gtype = basetype.GetGenericArguments()[0];
                    if (gtype.IsSubclassOf(typeof(AggRoot)))
                        return gtype;
                    else
                        return GetBaseGenericType(basetype, orgtype);
                }
                else
                    return GetBaseGenericType(basetype, orgtype);
            }
            return null;
        }

        /// <summary>
        /// 获取类型的缺省值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T Default<T>()
        {
            return default(T);
        }
        static Dictionary<Type, object> _defaultvalues = new Dictionary<Type, object>(30);
        /// <summary>
        /// 获取类型的缺省值
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static object Default(Type type)
        {
            if (!type.IsValueType || Nullable.GetUnderlyingType(type) != null)
                return null;
            object value = null;
            if (!_defaultvalues.ContainsKey(type))
            {
                value = Activator.CreateInstance(type);
                _defaultvalues[type] = value;
            }
            else
                value = _defaultvalues[type];
            return value;
        }
        /// <summary>
        /// 获取集合的元素类型
        /// </summary>
        /// <param name="collection">集合类型</param>
        /// <returns></returns>
        public static Type GetElementType(Type collection)
        {
            if (collection != null)
            {
                if (collection.IsGenericType)
                {
                    var arg = collection.GetGenericArguments();
                    if (arg.Length == 1)
                        return arg[0];
                }
                else if (collection.IsArray)
                    return collection.GetElementType();
            }
            return null;
        }

        public static Type GetBaseTypeUntil(Type type, string basename)
        {
            if (type.BaseType == null)
                return null;
            if (type.BaseType.Name == basename)
                return type;
            return GetBaseTypeUntil(type.BaseType, basename);
        }
    }
}
