using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Domain;
using Owl.Util;

namespace System
{
    /// <summary>
    /// 框架核心基础对象
    /// </summary>
    public abstract class Object2
    {

        /// <summary>
        /// 索引
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        [IgnoreField]
        public abstract object this[string key] { get; set; }

        /// <summary>
        /// 获取指定key的值
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public object Val(string key)
        {
            return this[key];
        }
        /// <summary>
        /// 设置指定字段的值
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Val(string key, object value)
        {
            this[key] = value;
        }

        /// <summary>
        /// 所有键的集合
        /// </summary>
        [IgnoreField]
        public abstract IEnumerable<string> Keys { get; }

        /// <summary>
        /// 是否包含键
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public abstract bool ContainsKey(string key);

        /// <summary>
        /// 根据传入字典更新对象
        /// </summary>
        /// <param name="dto"></param>
        public virtual void Write(IDictionary<string, object> dto)
        {
            Object2 obj = new TransferObject(dto);
            Write(obj);
        }

        public virtual void Write(TransferObject dto)
        {
            Write((Object2)dto);
        }

        /// <summary>
        /// 根据传入的Object2 更新对象
        /// </summary>
        /// <param name="dto"></param>
        public abstract void Write(Object2 dto);
        /// <summary>
        /// 读取对象并用字典返回
        /// </summary>
        /// <param name="fordisplay">为页面展现</param>
        /// <returns></returns>
        public TransferObject Read(bool fordisplay = false)
        {
            return _Read(fordisplay);
        }

        protected abstract TransferObject _Read(bool fordisplay);

        /// <summary>
        /// 深入获取字段值
        /// </summary>
        /// <param name="property">字段名称，自对象的用 . 分隔</param>
        /// <returns></returns>
        public object GetDepthValue(string property)
        {
            var keys = property.Split(new char[] { '.' }, 2);
            var obj = this[keys[0]];
            if (keys.Length == 2)
            {
                var tmp = obj as Object2;
                if (tmp != null)
                    return tmp.GetDepthValue(keys[1]);
                throw new AlertException("error.system.object2.getdepthvalue.fieldinvalid", "{0}中包含的字段{1}不是有效的可导航对象！", property, keys[1]);
            }
            return obj;
        }

        /// <summary>
        /// 获取键的显示示文本
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string GetDisplay(string key)
        {
            return _GetDisplay(key);
        }
        /// <summary>
        /// 获取键的真实值(不包含显示文本部分)
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public object GetRealValue(string key)
        {
            return _GetRealValue(key);
        }

        /// <summary>
        /// 获取键的真实值(不包含显示文本部分)
        /// </summary>
        /// <typeparam name="T">值的类型</typeparam>
        /// <param name="key">键的名称</param>
        /// <param name="_default">缺省值</param>
        /// <returns></returns>
        public T GetRealValue<T>(string key, T _default = default(T))
        {
            var value = _GetRealValue(key);
            if (value == null)
                return _default;
            if (value is T)
                return (T)value;
            return Convert2.ChangeType<T>(value);
        }


        protected virtual string _GetDisplay(string key)
        {
            return string.Format("{0}", this[key]);
        }
        protected virtual object _GetRealValue(string key)
        {
            return this[key];
        }
    }
}
