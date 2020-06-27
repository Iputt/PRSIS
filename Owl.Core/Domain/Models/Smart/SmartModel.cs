using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Owl.Domain
{

    public class SmartModel : TransferObject
    {

        [IgnoreField]
        public Guid Id
        {
            get
            {
                if (ContainsKey("Id"))
                    return Util.Convert2.ChangeType<Guid>(this["Id"]);
                return Guid.NewGuid();
            }
        }
        public static SmartModel FromDomain(DomainObject obj)
        {
            var model = new SmartModel();
            model.Write(obj.Read());
            model.__ModelName__ = obj.Metadata.Name;
            return model;
        }

        public DomainObject ToDomain()
        {
            var domain = DomainFactory.Create(__ModelName__);
            domain.Write(this);
            return domain;
        }
    }
}
