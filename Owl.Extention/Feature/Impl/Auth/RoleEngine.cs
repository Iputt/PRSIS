using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Domain;

namespace Owl.Feature.iAuth
{
    public class RoleEngine : Engine<RoleProvider, RoleEngine>
    {
        /// <summary>
        /// 获取所有角色类型
        /// </summary>
        /// <param name="accounttype">账户类型</param>
        public static Dictionary<string, RoleType> GetRoleTypes(string accounttype = "")
        {
            Dictionary<string, RoleType> types = new Dictionary<string, RoleType>();
            foreach (var provider in Providers)
            {
                var tmp = provider.GetRoleTypes(accounttype);
                if (tmp == null)
                    continue;
                foreach (var type in tmp)
                {
                    if (!types.ContainsKey(type.Name))
                        types[type.Name] = type;
                }
            }
            return types;
        }

        public static IEnumerable<RoleType> GetRoleTypeForSelect(string accounttype = "")
        {
            var roletypes = GetRoleTypes(accounttype).Values;
            return roletypes.Where(s => !s.NoAlloc);
        }

        public static Role GetRole(string code)
        {
            return Execute2<string, Role>(s => s.GetRole, code);
        }

        /// <summary>
        /// 获取指定客户端指定公司的所有角色
        /// </summary>
        public static IEnumerable<Role> GetRoles()
        {
            Dictionary<string, Role> roles = new Dictionary<string, Role>();
            foreach (var provider in Providers)
            {
                var tmp = provider.GetRoles();
                if (tmp == null)
                    continue;
                foreach (var type in tmp)
                {
                    if (!roles.ContainsKey(type.FullName))
                        roles[type.FullName] = type;
                }
            }
            return roles.Values;
        }
        ///// <summary>
        ///// 获取当前登录账号的公司所有账号
        ///// </summary>
        ///// <returns></returns>
        //public static IEnumerable<Role> GetRoles()
        //{
        //    var member = Member.Current;
        //    string mandt = "";
        //    string bukrs = "";
        //    if (member != null)
        //    {
        //        mandt = member.MANDT;
        //        bukrs = member.BUKRS;
        //    }
        //    return GetRoles(mandt, bukrs);
        //}
    }
}
