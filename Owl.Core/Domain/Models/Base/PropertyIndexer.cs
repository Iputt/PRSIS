using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using Owl.Util;

namespace Owl.Domain
{
    

    /// <summary>
    /// 包含属性的可索引对象基类
    /// </summary>
    public abstract class PropertyIndexer : Object2
    {
        FastInvoker instance;
        [IgnoreField]
        internal FastInvoker Instance
        {
            get
            {
                if (instance == null)
                    instance = FastInvoker.GetInstance(GetType());
                return instance;
            }
        }
        internal void SetInvoker(FastInvoker invoker)
        {
            instance = invoker;
        }

        /// <summary>
        /// 设置字段的值
        /// </summary>
        /// <param name="property">字段名称</param>
        /// <param name="value">值</param>
        protected virtual void SetValue(string property, object value)
        {
            Instance.SetValue(this, property, value);
        }

        /// <summary>
        /// 获取字段的值
        /// </summary>
        /// <param name="property">字段名称</param>
        /// <returns></returns>
        protected virtual object GetValue(string property)
        {
            return Instance.GetValue(this, property);
        }

        /// <summary>
        /// 索引
        /// </summary>
        /// <param name="property">字段名称</param>
        /// <returns></returns>
        [IgnoreField]
        public override object this[string property]
        {
            get { return GetValue(property); }
            set
            {
                if (!string.IsNullOrEmpty(property))
                    SetValue(property, value);
            }
        }
    }
    public static class xObjectHelper
    {

        static object toGuid(object value)
        {
            if (value is Guid)
                return value;
            if (value is string && (string)value != "")
            {
                Guid v;
                Guid.TryParse((string)value, out v);
                if (v != Guid.Empty)
                    return v;
            }
            return Guid.NewGuid();
        }
        public static object FitType(object value, Type desttype)
        {
            if (desttype.Name == "Guid")
                return toGuid(value);
            return Convert2.ChangeType(value, desttype);
        }
    }
}
