using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Domain;
namespace Owl.Feature.Impl.Config
{
    /// <summary>
    /// 部分，部件
    /// </summary>
    public abstract class Section : SmartObject
    {
        static Dictionary<Type, Section> _Default = new Dictionary<Type, Section>();
        /// <summary>
        /// 注册缺省值
        /// </summary>
        /// <typeparam name="TSection">配置类型</typeparam>
        /// <param name="value">值</param>
        protected static void RegisterDefault<TSection>(TSection value)
            where TSection : Section
        {
            _Default[typeof(TSection)] = value;
        }

        public static TSection Default<TSection>()
            where TSection : Section, new()
        {
            var type = typeof(TSection);
            if (_Default.ContainsKey(type))
                return _Default[type] as TSection;
            return new TSection();
        }
        string _prefix;
        /// <summary>
        /// 前缀
        /// </summary>
        public virtual string Prefix
        {
            get
            {
                if (string.IsNullOrEmpty(_prefix))
                    _prefix = GetType().Name.Replace("Section", "");
                return _prefix;
            }
        }
    }
}
