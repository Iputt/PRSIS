using Owl.Const;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Owl.Feature.Impl.Select
{

    public class TemplateSelectProvider : SelectProvicer
    {
        public override int Priority => 100;
        protected override void Init()
        {
            Register(CoreConst.TemplateList, top =>
            {
                return new Domain.ListOptionCollection(Template.GetProviderNames().Select(s => new Domain.ListItem(s, s)));
            });
        }
    }
}
