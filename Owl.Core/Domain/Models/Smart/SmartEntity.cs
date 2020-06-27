using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Owl.Domain
{
    /// <summary>
    /// 智能实体
    /// </summary>
    [CustomObjectTemplate(Label = "行项目对象",  Ordinal = 900)]
    public sealed class SmartEntity : Entity
    {
        public SmartEntity()
        {
        }
        public SmartEntity(ModelMetadata meta)
        {
            Metadata = meta;
        }
    }
}
