using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Domain.Driver;
using System.Reflection;
using Owl.Util;
namespace Owl.Domain
{
    public class ReflectMetaProvider : MetaProvider
    {
        void loadfromasm(string name, Assembly asm)
        {
            var types = TypeHelper.LoadTypeFromAsm<DomainObject>(asm);
            foreach (var type in types)
            {
                var metadata = DomainModel.FromType(type);
            }
        }

        void unloadasm(string name, Assembly asm)
        {
            var types = TypeHelper.LoadTypeFromAsm<DomainObject>(asm);
            foreach (var type in types)
            {
                RemoveMeta(type.MetaName());
            }
        }

        public override void Init()
        {
            Util.AsmHelper.RegisterResource(loadfromasm, unloadasm);
        }
        public override int Priority
        {
            get { return 1000; }
        }
    }
}
