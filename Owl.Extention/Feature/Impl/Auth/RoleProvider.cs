using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Domain;
using Owl.Util;
namespace Owl.Feature.iAuth
{
    public abstract class RoleProvider : Provider
    {
        /// <summary>
        /// 获取角色类型
        /// </summary>
        /// <param name="used"></param>
        /// <returns></returns>
        public abstract IEnumerable<RoleType> GetRoleTypes(string accounttype);

        /// <summary>
        /// 获取角色
        /// </summary>
        /// <returns></returns>
        public abstract IEnumerable<Role> GetRoles();

        public abstract Role GetRole(string code);

    }

    public class MetaRoleProvicer : RoleProvider
    {
        public override int Priority
        {
            get { return 1; }
        }

        #region
        static Dictionary<string, RoleType> roletypes = new Dictionary<string, RoleType>();
        static List<Role> roles = new List<Role>();
        static MetaRoleProvicer()
        {
            AsmHelper.RegisterResource(loadasm, unloadasm);
        }

        static void loadasm(string asmname, System.Reflection.Assembly asm)
        {
            foreach (RoleCategoryAttribute appattr in asm.GetCustomAttributes(typeof(RoleCategoryAttribute), false))
            {
                if (roletypes.ContainsKey(appattr.Name) && roletypes[appattr.Name].Ordinal > appattr.Ordinal)
                    continue;
                roletypes[appattr.Name] = new RoleType() { Name = appattr.Name, Display = appattr.Display, AccountType = new HashSet<string>(appattr.AccountType), Ordinal = appattr.Ordinal, NoAlloc = appattr.NoAlloc, Mode = MetaMode.Base };
            }

            foreach (RoleAttribute roleattr in asm.GetCustomAttributes(typeof(RoleAttribute), false))
            {
                var role = new Role()
                {
                    Type = roleattr.Type,
                    Name = roleattr.Name,
                    Display = roleattr.Display,
                    Description = roleattr.Description,
                    Ordinal = roleattr.Ordinal,
                    Mode = MetaMode.Base,
                    Target = roleattr.Target,
                    Children = roleattr.Children
                };
                roles.Add(role);
            }
        }
        static void unloadasm(string name, System.Reflection.Assembly asm)
        {

        }
        #endregion

        public override IEnumerable<RoleType> GetRoleTypes(string accounttype)
        {
            if (string.IsNullOrEmpty(accounttype))
                return roletypes.Values;
            return roletypes.Values.Where(s => s.AccountType.Contains(accounttype));
        }

        public override IEnumerable<Role> GetRoles()
        {
            return roles.AsReadOnly();
        }
        public override Role GetRole(string code)
        {
            return roles.FirstOrDefault(s => s.FullName == code);
        }
    }
}
