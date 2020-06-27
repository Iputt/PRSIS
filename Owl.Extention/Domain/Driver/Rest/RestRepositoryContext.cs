using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Owl.Domain.Driver.Repository
{
    public class RestRepositoryContext : RepositoryContext
    {
        public override void Commit()
        {
            RestProxy.Instance.Save(ForAdd.Values, ForUpdate.Values, ForRemove.Values);
        }

        public override void RollBack()
        {
            
        }
    }
}
