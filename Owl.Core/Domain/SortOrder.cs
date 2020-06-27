using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
namespace Owl.Domain
{
    /// <summary>
    /// 排序方式
    /// </summary>
    public enum SortOrder
    {
        [DomainLabel("不排序")]
        /// <summary>
        /// 不排序
        /// </summary>
        Unspecified = 0,
        /// <summary>
        /// 顺序排序
        /// </summary>
        [DomainLabel("顺序")]
        Ascending = 1,
        /// <summary>
        /// 逆序排序
        /// </summary>
        [DomainLabel("倒序")]
        Descending = 2
    }

    /// <summary>
    /// 排序集合 顺序优先级
    /// </summary>
    public class SortBy : Dictionary<string, SortOrder>
    {
        /// <summary>
        /// 按修改时间顺序
        /// </summary>
        public static readonly SortBy Sortby_Modified = new SortBy() { { "Modified", SortOrder.Ascending } };
        /// <summary>
        /// 按修改时间逆序
        /// </summary>
        public static readonly SortBy Sortby_Modified_Desc = new SortBy() { { "Modified", SortOrder.Descending } };

        /// <summary>
        /// 按Id顺序
        /// </summary>
        public static readonly SortBy Sortby_Id = new SortBy() { { "Id", SortOrder.Ascending } };
        /// <summary>
        /// 按Id逆序
        /// </summary>
        public static readonly SortBy Sortby_Id_Desc = new SortBy() { { "Id", SortOrder.Descending } };

        public static SortBy Create(string key, SortOrder order = SortOrder.Ascending)
        {
            return new SortBy() { { key, order } };
        }

        public override string ToString()
        {
            return Count == 0 ? "" : "order by " + string.Join(",", this.Select(s => string.Format("{0} {1} ", s.Key, s.Value == SortOrder.Ascending ? "asc" : "desc")));
        }
    }
}
