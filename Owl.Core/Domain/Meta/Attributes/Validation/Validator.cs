using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Feature;
using Owl.Util;
using Owl.Const;

namespace Owl.Domain.Validation
{
    /// <summary>
    /// 验证器
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
    public abstract class Validator : Attribute
    {
        /// <summary>
        /// 验证器
        /// </summary>
        /// <param name="type">验证器类型</param>
        /// <param name="errorformat">默认错误格式</param>
        protected Validator(string type, string errorformat, string errorresource)
        {
            Type = type;
            ErrorFormat = errorformat;
            ErrorResource = string.Format("error.owl.validation.errorformat.{0}", errorresource);
        }

        /// <summary>
        /// 验证方式
        /// </summary>
        public string Type { get; protected set; }

        IDictionary<string, object> parameters;
        /// <summary>
        /// 验证参数
        /// </summary>
        public IDictionary<string, object> Parameters
        {
            get
            {
                if (parameters == null)
                    parameters = new Dictionary<string, object>();
                return parameters;
            }
        }

        /// <summary>
        /// 错误消息
        /// </summary>
        protected string ErrorFormat { get; set; }

        List<object> m_argument;
        /// <summary>
        ///  格式化参数
        /// </summary>
        protected List<object> Argument
        {
            get
            {
                if (m_argument == null)
                    m_argument = new List<object>();
                return m_argument;
            }
        }

        /// <summary>
        /// 资源名称
        /// </summary>
        protected string ErrorResource { get; set; }

        /// <summary>
        /// 格式化错误消息
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public virtual string GetError(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "";
            var transmessage = Translation.Get(ErrorResource, ErrorFormat, true);
            var args = new List<object>();
            args.Add(name);
            args.AddRange(Argument);
            return string.Format(transmessage, args.ToArray());
        }

        protected bool IsNull(object value)
        {
            return value == null || (value is string && (string)value == "");
        }

        /// <summary>
        /// 是否有效
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public virtual bool IsValid(object value, Object2 obj)
        {
            return true;
        }
    }
}
