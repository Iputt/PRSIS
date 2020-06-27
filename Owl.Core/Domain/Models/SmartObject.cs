using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Reflection;
using Owl.Util;
using System.Collections;
namespace Owl.Domain
{
    /// <summary>
    /// 非领域型对象基类
    /// </summary>
    public abstract class SmartObject : PropertyIndexer
    {
        #region 类型帮助
        Dictionary<string, PropertyInfo> m_properties;
        [IgnoreField]
        protected Dictionary<string, PropertyInfo> Properties
        {
            get
            {
                if (m_properties == null)
                    m_properties = TypeHelper.GetProperties(GetType());
                return m_properties;
            }
        }

        #endregion

        protected TransferObject InnerDict = new TransferObject();

        [IgnoreField]
        public override IEnumerable<string> Keys
        {
            get { return Properties.Keys.Union(InnerDict.Keys); }
        }

        public override bool ContainsKey(string key)
        {
            return Properties.ContainsKey(key) || InnerDict.ContainsKey(key);
        }
        /// <summary>
        /// 从dto中同步数据
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="full">字段不存在时是否写入内部字典中</param>
        public void Write(Object2 dto, bool full)
        {
            if (dto == null)
                return;
            foreach (var key in dto.Keys)
            {
                var value = dto[key];
                PropertyInfo info = Properties.ContainsKey(key) ? Properties[key] : null;
                if (info != null)
                {
                    if (info.PropertyType.IsSubclassOf(typeof(SmartObject)))
                    {
                        var obj = Activator.CreateInstance(info.PropertyType) as SmartObject;
                        obj.Write(value as Object2);
                        value = obj;
                    }
                    else
                    {
                        var elementtype = TypeHelper.GetElementType(info.PropertyType);
                        if (elementtype != null && elementtype.IsSubclassOf(typeof(SmartObject)))
                        {
                            ArrayList array = new ArrayList();
                            if (value != null)
                            {
                                foreach (Object2 v in (dynamic)value)
                                {
                                    var obj = Activator.CreateInstance(elementtype) as SmartObject;
                                    obj.Write(v);
                                    array.Add(obj);
                                }
                            }
                            if (info.CanWrite)
                            {
                                value = Convert2.ChangeType(array, info.PropertyType, elementtype);
                            }
                            else
                            {
                                dynamic mv = this[key];
                                if (mv != null)
                                {
                                    mv.Clear();
                                    foreach (var obj in array)
                                    {
                                        mv.Add(obj);
                                    }
                                }
                                continue;
                            }
                        }
                    }
                }
                if (info != null || full)
                    this[key] = value;
            }
        }
        public override void Write(Object2 dto)
        {
            Write(dto, true);
        }

        protected override TransferObject _Read(bool fordisplay)
        {
            TransferObject obj = new TransferObject();
            foreach (var info in Properties.Values)
            {
                if (info.PropertyType.IsGenericType && info.PropertyType.GetGenericArguments()[0].IsSubclassOf(typeof(SmartObject)))
                {
                    var dtos = new List<TransferObject>();
                    var objs = Instance.GetValue(this, info.Name);
                    if (objs != null)
                    {
                        foreach (SmartObject so in (IEnumerable)objs)
                            dtos.Add(so.Read(fordisplay));
                    }
                    obj[info.Name] = dtos;
                }
                else
                    obj[info.Name] = Instance.GetValue(this, info.Name);
            }
            return obj;
        }
        [IgnoreField]
        public override object this[string property]
        {
            get
            {
                if (Properties.ContainsKey(property))
                    return base[property];
                return InnerDict.ContainsKey(property) ? InnerDict[property] : null;
            }
            set
            {
                if (Properties.ContainsKey(property))
                    base[property] = value;
                else
                    InnerDict[property] = value;
            }
        }

        public T Get<T>(string property)
            where T : class
        {
            return this[property] as T;
        }

        public static T Create<T>(Object2 dto)
            where T : SmartObject, new()
        {
            T t = new T();
            t.Write(dto);
            return t;
        }
    }
}
