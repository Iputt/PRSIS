using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Domain;
using Owl.Util;

namespace Owl.Feature.iAuth
{
    public class AuthEngine : Engine<AuthProvider, AuthEngine>
    {
        public static Permission Default = EnumHelper.Parse<Permission>(AppConfig.GetSetting("owl_auth_permission_default"));
        /// <summary>
        /// 获取对象授权
        /// </summary>
        /// <param name="model"></param>
        /// <param name="user"></param>
        /// <param name="groups"></param>
        /// <param name="roles"></param>
        /// <returns></returns>
        public static Permission GetAuth(Guid? actionid, string model, string user, IEnumerable<string> roles)
        {
            var auths = Execute3<Guid?, string, AuthObject>(s => s.GetAuth, actionid, model, s => s.IsValid);
            bool isempty = true;
            var permission = Permission.None;
            foreach (var auth in auths)
            {
                if ((permission & auth.Permission) == auth.Permission)
                    continue;
                isempty = false;
                if (auth.Allow(user, roles))
                    permission = permission | auth.Permission;
            }
            return isempty ? Default : permission;
        }
        /// <summary>
        /// 获取字段或消息授权
        /// </summary>
        /// <param name="name"></param>
        /// <param name="model"></param>
        /// <param name="target"></param>
        /// <param name="login"></param>
        /// <param name="jobs"></param>
        /// <param name="roles"></param>
        /// <returns></returns>
        public static bool HasPower(Guid? actionid, string name, string model, AuthTarget target, string login, IEnumerable<string> roles)
        {
            if (target == AuthTarget.Model)
                return false;
            var auths = Execute3<Guid?, string, string, AuthTarget, AuthObject>(s => s.GetAuth, actionid, name, model, target, s => s != null && s.IsValid);
            if (auths.Count == 0)
            {
                return target == AuthTarget.Field;
            }
            foreach (var auth in auths)
            {
                if (auth.Allow(login, roles))
                    return true;
            }
            return false;
        }
    }
}
