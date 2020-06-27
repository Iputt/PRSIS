using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Owl.Domain
{
    /// <summary>
    /// 智能根
    /// </summary>
    [DomainModel(NoTable = true)]
    [CustomObjectTemplate(Label = "普通根对象",  Ordinal = 100)]
    public sealed class SmartRoot : AggRoot
    {
        public SmartRoot() { }

        public SmartRoot(ModelMetadata meta)
        {
            Metadata = meta;
        }
    }
}
