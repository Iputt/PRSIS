using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Util;

namespace Owl.Feature.Impl.Config
{
    public class SectionEngine : Engine<SectioinProvider, SectionEngine>
    {
        public static TSection GetConfig<TSection>()
            where TSection : Section, new()
        {
            return Execute2<TSection>(s => s.GetSection<TSection>);
        }
    }
}
