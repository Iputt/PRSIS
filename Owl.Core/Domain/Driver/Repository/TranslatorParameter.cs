using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Owl.Util;
namespace Owl.Domain.Driver.Repository
{
    /// <summary>
    /// 参数
    /// </summary>
    public class TranslatorParameter
    {
        /// <summary>
        /// 参数名称
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// 别名
        /// </summary>
        public string Alias { get; private set; }
        /// <summary>
        /// 值
        /// </summary>
        public object Value { get; private set; }
        /// <summary>
        /// 子参数
        /// </summary>
        public List<TranslatorParameter> Children { get; private set; }

        public TranslatorParameter(string name, object value, string alias = "")
        {
            Name = name;
            Value = value;
            Alias = alias.Coalesce(name);
            if (value is DateTime)
            {
                DateTime tmp = Util.DTHelper.ToLocalTime((DateTime)value);
                Value = tmp;
            }

            if (!(value is string) && value is IEnumerable && !(value is byte[]))
            {
                Children = new List<TranslatorParameter>();
                int i = 0;
                foreach (var v in value as IEnumerable)
                {
                    Children.Add(new TranslatorParameter(string.Format("{0}_{1}", name, i), v, alias));
                    i = i + 1;
                }
            }
        }
    }
    /// <summary>
    /// 参数集合
    /// </summary>
    public class TranslatorParameterCollection : IEnumerable<TranslatorParameter>
    {
        public TranslatorParameter Create(object value, string alias = "")
        {
            Index += 1;
            string prefix = "p";
            string name = string.Format("{0}{1}", prefix, Index - 1);
            var param = new TranslatorParameter(name, value, alias);
            m_parameters[name] = param;
            return param;
        }
        /// <summary>
        /// 当前序号
        /// </summary>
        public int Index { get; private set; }
        Dictionary<string, TranslatorParameter> m_parameters = new Dictionary<string, TranslatorParameter>();
        public TranslatorParameterCollection()
            : this(null, 0)
        {
        }
        public TranslatorParameterCollection(IEnumerable<TranslatorParameter> parameters, int index = 0)
        {
            this.Index = index;
            if (parameters == null)
                return;
            foreach (var param in parameters)
                m_parameters[param.Name] = param;

        }

        /// <summary>
        /// 根据名称获取参数
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public TranslatorParameter Get(string name)
        {
            if (m_parameters.ContainsKey(name))
                return m_parameters[name];
            return null;
        }
        public IEnumerator<TranslatorParameter> GetEnumerator()
        {
            return m_parameters.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count { get { return m_parameters.Count; } }
    }

}
