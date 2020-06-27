using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Owl.Domain
{
    /// <summary>
    /// 索引
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
    public class IndexAttribute : Attribute
    {
        /// <summary>
        /// 索引名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 是否聚集索引
        /// </summary>
        public bool Cluster { get; set; }

        /// <summary>
        /// 是否唯一
        /// </summary>
        public bool Unique { get; set; }

        /// <summary>
        /// 排序方式
        /// </summary>
        public SortOrder Sort { get; set; }

        /// <summary>
        /// 在索引中的位置
        /// </summary>
        public int Order { get; set; }
    }
}
