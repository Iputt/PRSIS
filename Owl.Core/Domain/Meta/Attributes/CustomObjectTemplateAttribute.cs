using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Owl.Domain
{
    /// <summary>
    /// 自定义对象模板
    /// </summary>
    public class CustomObjectTemplateAttribute : DomainLabel
    {
        /// <summary>
        /// 序号
        /// </summary>
        public int Ordinal { get; set; }
    }
}
