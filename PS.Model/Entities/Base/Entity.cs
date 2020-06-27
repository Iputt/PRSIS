using System;
using System.Collections.Generic;
using System.Text;

namespace PS.Model
{
    /// <summary>
    /// 实体基类
    /// </summary>
    public class Entity
    {
        /// <summary>
        /// GUID
        /// </summary>
        public virtual Guid ID { get; set; }
    }
}
