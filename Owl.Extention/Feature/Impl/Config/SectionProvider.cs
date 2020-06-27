using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Util;

namespace Owl.Feature.Impl.Config
{
    public abstract class SectioinProvider : Provider
    {
        public abstract TSection GetSection<TSection>()
            where TSection : Section, new();
    }
}
