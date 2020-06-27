using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Owl;
using System.Collections;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Linq;
using Owl.Util;
namespace System
{
    /// <summary>
    /// 字段特定类型
    /// </summary>
    public class JsonFieldType
    {
        public string Name { get; private set; }

        public Type Type { get; private set; }

        public Type InnerType { get; private set; }

        public Func<Array, object> Create { get; private set; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name">字段名称</param>
        /// <param name="type">字段类型</param>
        /// <param name="inner">内部类型（用于集合）</param>
        public JsonFieldType(string name, Type type, Type inner = null, Func<Array, object> create = null)
        {
            Name = name;
            Type = type;
            InnerType = inner;
            if (InnerType == null)
            {
                if (Type.HasElementType)
                    InnerType = Type.GetElementType();
                else if (Type.IsGenericType)
                    InnerType = Type.GetGenericArguments()[0];
            }
            Create = create;
        }
    }
    /// <summary>
    /// 指定字段类型
    /// </summary>
    public class SpecTypeConverter : JsonConverter
    {
        bool firtdep = true;
        IDictionary<string, JsonFieldType> m_types;
        public SpecTypeConverter(params JsonFieldType[] fields)
        {
            m_types = fields.ToDictionary(s => s.Name);
        }

        public override bool CanConvert(Type objectType)
        {
            return firtdep;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (firtdep)
            {
                firtdep = false;
                JObject token = (JObject)serializer.Deserialize(reader);
                if (token == null)
                    throw new Exception2();
                Dictionary<string, object> adds = new Dictionary<string, object>();
                foreach (var key in m_types.Keys)
                {
                    var vs = token[key];
                    var field = m_types[key];
                    if (vs != null)
                    {
                        if (vs.Type == JTokenType.Array && vs.HasValues && vs.First.Type == JTokenType.Object)
                        {
                            ArrayList list = new ArrayList();
                            foreach (JObject v in vs)
                                list.Add(v.ToObject(field.InnerType, serializer));
                            var array = list.ToArray(field.InnerType);
                            object value = null;
                            if (field.Type.IsArray || field.Type.Name == "IEnumerable`1")
                                value = array;
                            else if (field.Create != null)
                                value = field.Create(array);
                            else
                            {
                                try
                                {
                                    dynamic d = Activator.CreateInstance(field.Type);
                                    foreach (dynamic v in array)
                                        d.Add(v);
                                    value = d;
                                }
                                catch { }
                            }
                            adds[key] = value;
                        }
                        else
                            adds[key] = vs.ToObject(field.Type, serializer);
                        token.Remove(key);
                    }
                }
                var obj = token.ToObject(objectType, serializer);
                foreach (var add in adds)
                {
                    var property = objectType.GetProperty(add.Key);
                    if (property != null)
                        property.SetValue(obj, add.Value, null);
                    else if (obj is IDictionary)
                    {
                        (obj as IDictionary<string, object>)[add.Key] = add.Value;
                    }
                    else if (obj is TransferObject)
                    {
                        (obj as TransferObject)[add.Key] = add.Value;
                    }
                }
                return obj;
            }
            else
                return serializer.Deserialize(reader, objectType);
        }
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }
    }

    public enum DictType
    {
        TransferObject,
        IDictory,
        Custom
    }

    public class AnonymousConverter<T> : JsonConverter
        where T : new()
    {
        static DictType? dicttype;
        public static DictType DictType
        {
            get
            {
                if (dicttype == null)
                {
                    var type = typeof(T);
                    if (type == typeof(TransferObject))
                        dicttype = DictType.TransferObject;
                    else if (type.GetInterfaces().Contains(typeof(IDictionary<string, object>)))
                        dicttype = DictType.IDictory;
                    else
                        dicttype = DictType.Custom;
                }
                return dicttype.Value;
            }
        }

        static MethodInfo m_MethodAdd;
        static bool loadmethodadd = false;
        protected static MethodInfo MethodAdd
        {
            get
            {
                if (!loadmethodadd)
                {
                    loadmethodadd = true;
                    m_MethodAdd = typeof(T).GetMethod("Add", new Type[] { typeof(string), typeof(object) });
                }
                return m_MethodAdd;
            }
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(object);
        }

        void Add(T obj, string key, object value)
        {
            switch (DictType)
            {
                case System.DictType.TransferObject: (obj as TransferObject).Add(key, value); break;
                case System.DictType.IDictory: (obj as IDictionary<string, object>).Add(key, value); break;
                default:
                    if (MethodAdd != null)
                        MethodAdd.FaseInvoke(obj, key, value);
                    break;
            }
        }

        T Parse(JObject obj)
        {
            T result = new T();
            foreach (var pair in obj)
            {
                if (pair.Value is JValue)
                {
                    Add(result, pair.Key, (pair.Value as JValue).Value);
                }
                else if (pair.Value is JObject)
                {
                    Add(result, pair.Key, Parse(pair.Value as JObject));
                }
            }
            return result;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var value = serializer.Deserialize(reader);
            if (value is JObject)
            {
                return Parse(value as JObject);
            }
            return value;
        }
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }
    }

    /// <summary>
    /// 自定义 json 转换器基类
    /// </summary>
    public abstract class xJsonConverter : JsonConverter
    {

    }

    public static class JsonHelper
    {
        static readonly List<xJsonConverter> converters = new List<xJsonConverter>();
        static void loadAsm(string asmname, Assembly asm)
        {
            var tmp = TypeHelper.LoadFromAsm<xJsonConverter>(asm);
            lock (converters)
                converters.AddRange(tmp);
        }
        static void unloadAsm(string asmname, Assembly asm)
        {
            lock (converters)
            {
                foreach (var converter in converters.Where(s => s.GetType().Assembly == asm).ToList())
                    converters.Remove(converter);
            }
        }
        static JsonHelper()
        {
            AsmHelper.RegisterResource(loadAsm, unloadAsm);
        }
        #region JSON序列化与反序列化
        /// <summary>
        /// 将给定的对象序列化为Json
        /// 通过为对象的字段添加System.Web.Script.Serialization.ScriptIgnoreAttribute 以避免循环嵌套
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string ToJson(this object obj)
        {
            return JsonConvert.SerializeObject(obj, converters.ToArray());
        }
        static JsonConverter[] GetConverters<T>(JsonFieldType[] fields)
            where T : new()
        {
            List<JsonConverter> tmp = new List<JsonConverter>(converters);
            if (fields.Length > 0)
            {
                fields = fields.Where(s => s != null).ToArray();
                if (fields.Length > 0)
                    tmp.Add(new SpecTypeConverter(fields));
            }

            tmp.Add(new AnonymousConverter<T>());
            return tmp.ToArray();
        }
        /// <summary>
        /// 将给定的Json字符串反序列为实体
        /// 通过为对象的字段添加System.Web.Script.Serialization.ScriptIgnoreAttribute 以避免循环嵌套
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="json"></param>
        /// <param name="fields">字段类型</param>
        /// <returns></returns>
        public static T DeJson<T>(this string json, params JsonFieldType[] fields)
        {
            return JsonConvert.DeserializeObject<T>(json, GetConverters<Dictionary<string, object>>(fields));
        }

        public static T DeJson<T, TAnnoymous>(this string json, params JsonFieldType[] fields)
            where TAnnoymous : new()
        {
            return JsonConvert.DeserializeObject<T>(json, GetConverters<TAnnoymous>(fields));
        }

        public static object DeJson(this string json, Type type, params JsonFieldType[] fields)
        {
            return JsonConvert.DeserializeObject(json, type, GetConverters<Dictionary<string, object>>(fields));
        }
        #endregion
    }
}
