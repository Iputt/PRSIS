using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Domain;
using Owl.Feature.iAuth;
using Owl.Util;
namespace Owl.Feature.Impl.Select
{
    internal class AuthSelectProvider : SelectProvicer
    {
        protected override void Init()
        {
            Register(Const.SelectConst.RoleType, s =>
            {

                var items = new ListOptionCollection();
                foreach (var type in RoleEngine.GetRoleTypeForSelect().OrderBy(t => t.Ordinal))
                {
                    items.AddOption(new ListItem(type.Name, type.Display));
                }
                return items;
            });

            Register("roles", s =>
            {
                var items = new ListOptionCollection();
                var roles = RoleEngine.GetRoles().GroupBy(t => t.Type)
                    .ToDictionary(t => t.Key, t => t.OrderByDescending(n => n.Ordinal));
                foreach (var rtype in RoleEngine.GetRoleTypeForSelect().OrderBy(t => t.Ordinal))
                {
                    if (!roles.ContainsKey(rtype.Name))
                        continue;
                    var group = new List<ListOption>();
                    Dictionary<string, ListItem> tmp = new Dictionary<string, ListItem>();
                    foreach (var role in roles[rtype.Name])
                    {
                        tmp[role.Name] = new ListItem(role.FullName, role.Display);
                    }
                    items.AddOption(new ListItemGroup(rtype.Display, tmp.Values));
                }
                return items;
            });
            Register(Const.SelectConst.RoleListForType, top =>
            {
                var items = new ListOptionCollection();
                // s = s.Coalesce(Owl.Const.SelectConst.AccountType_Tenant);
                var roles = RoleEngine.GetRoles().Where(s => s.Type == top);
                foreach (var role in roles.OrderBy(s => s.Ordinal))
                {
                    items.AddOption(new ListItem(role.Name, role.Display));
                }
                return items;
            });
        }

        public override int Priority
        {
            get { return 1; }
        }
    }
}
