using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Util;
namespace Owl.Domain
{
    /// <summary>
    /// 授权
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class Authorize : Attribute
    {
        /// <summary>
        /// 角色列表
        /// </summary>
        public string[] Roles { get; set; }

        /// <summary>
        /// 授权的权限
        /// </summary>
        public Permission Permission { get; set; }

        public Authorize()
        {
        }

        /// <summary>
        /// 授权访问
        /// </summary>
        /// <param name="permission">允许的权限</param>
        /// <param name="roles">授权的角色列表</param>
        public Authorize(Permission permission, params string[] roles)
        {
            Roles = roles;
            Permission = permission;
        }

        public AuthObject ToAuthObj()
        {
            return new AuthObject(Permission, Roles.Length == 0, Roles);
        }
    }

    /// <summary>
    /// 允许的角色，与Deny组和使用，有序号决定优先级
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Class)]
    public sealed class Allow : Attribute
    {
        /// <summary>
        /// 关连的角色
        /// </summary>
        public string[] Roles { get; private set; }

        /// <summary>
        /// 允许所有人访问
        /// </summary>
        public bool AllowAll { get; private set; }

        /// <summary>
        /// 允许的角色
        /// </summary>
        /// <param name="roles">角色列表</param>
        public Allow(params string[] roles)
        {
            Roles = roles;
            AllowAll = false;
        }

        /// <summary>
        /// 允许的角色
        /// </summary>
        /// <param name="allowall">允许所有</param>
        public Allow(bool allowall)
        {
            AllowAll = allowall;
        }


        public AuthObject ToAuthObj(string name, AuthTarget target)
        {
            return new AuthObject(name, target, AllowAll, Roles);
        }
    }

    /// <summary>
    /// 授权对象
    /// </summary>
    public class AuthObject
    {
        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; private set; }
        /// <summary>
        /// 类型
        /// </summary>
        public AuthTarget Target { get; private set; }

        /// <summary>
        /// 对象相关权限
        /// </summary>
        public Permission Permission { get; private set; }

        /// <summary>
        /// 是否允许所有人访问
        /// </summary>
        public bool AllowAll { get; set; }

        /// <summary>
        /// 授权的用户
        /// </summary>
        public HashSet<string> Users { get; private set; }

        /// <summary>
        /// 授权的用户组
        /// </summary>
        //public HashSet<string> Groups { get; private set; }

        /// <summary>
        /// 授权的角色
        /// </summary>
        public HashSet<string> Roles { get; private set; }

        /// <summary>
        /// 验证对象是否有效
        /// </summary>
        public bool IsValid
        {
            get
            {
                return AllowAll || (!AllowAll && (Users.Count > 0 || Roles.Count > 0));
            }
        }

        AuthObject()
        {
            Users = new HashSet<string>();
            Roles = new HashSet<string>();
        }
        /// <summary>
        /// 授权对象
        /// </summary>
        /// <param name="name">权限名称</param>
        /// <param name="target">权限类型</param>
        public AuthObject(string name, AuthTarget target, bool allowall, params string[] roles)
            : this()
        {
            Name = name;
            Target = target;
            if (Target == AuthTarget.Model)
                Permission = Util.Convert2.ChangeType<Permission>(name);
            AllowAll = allowall;
            if (!AllowAll)
            {
                foreach (var role in roles)
                {
                    Roles.Add(role);
                }
            }
        }

        public AuthObject(string name, AuthTarget target, params string[] roles)
            : this(name, target, false, roles)
        {

        }

        /// <summary>
        /// 对象权限授权
        /// </summary>
        /// <param name="permission"></param>
        public AuthObject(Permission permission, bool allowall, params string[] roles)
            : this()
        {
            Permission = permission;
            Target = AuthTarget.Model;
            Name = permission.ToString();
            AllowAll = allowall;
            if (!AllowAll)
            {
                foreach (var role in roles)
                    Roles.Add(role);
            }
        }
        /// <summary>
        /// 授权对象是否允许访问
        /// </summary>
        /// <param name="user">用户</param>
        /// <param name="groups">用户组集合</param>
        /// <param name="roles">角色集合</param>
        /// <returns></returns>
        public bool Allow(string user, IEnumerable<string> roles)
        {
            return AllowAll ||
                   (Users.Count > 0 && Users.Contains(user)) ||
                   (Roles.Count > 0 && roles.Any(s => Roles.Contains(s)));
        }
    }

    public class AuthModel
    {
        public string ModelName { get; private set; }
        /// <summary>
        /// 对象的增删改权限
        /// </summary>
        public Dictionary<string, AuthObject> General { get; private set; }
        /// <summary>
        /// 消息权限
        /// </summary>
        public Dictionary<string, AuthObject> Messages { get; private set; }
        /// <summary>
        /// 字段权限
        /// </summary>
        public Dictionary<string, AuthObject> Fields { get; private set; }


        /// <summary>
        /// 从对象权限中删除用户
        /// </summary>
        /// <param name="user"></param>
        public void RemoveUser(string user)
        {
            foreach (var obj in General.Values)
            {
                if (!string.IsNullOrEmpty(user) && obj.Users.Contains(user))
                    obj.Users.Remove(user);
            }
            foreach (var obj in Messages.Values)
            {
                if (!string.IsNullOrEmpty(user) && obj.Users.Contains(user))
                    obj.Users.Remove(user);
            }
            foreach (var obj in Fields.Values)
            {
                if (!string.IsNullOrEmpty(user) && obj.Users.Contains(user))
                    obj.Users.Remove(user);
            }
        }

        /// <summary>
        /// 从对象权限中删除角色
        /// </summary>
        /// <param name="role"></param>
        public void RemoveRole(string role)
        {
            foreach (var obj in General.Values)
            {
                if (!string.IsNullOrEmpty(role) && obj.Roles.Contains(role))
                    obj.Roles.Remove(role);
            }
            foreach (var obj in Messages.Values)
            {
                if (!string.IsNullOrEmpty(role) && obj.Roles.Contains(role))
                    obj.Roles.Remove(role);
            }
            foreach (var obj in Fields.Values)
            {
                if (!string.IsNullOrEmpty(role) && obj.Roles.Contains(role))
                    obj.Roles.Remove(role);
            }
        }
        /// <summary>
        /// 删除权限
        /// </summary>
        /// <param name="permission"></param>
        /// <param name="target"></param>
        public void RemovePermission(string permission,AuthTarget target)
        {
            if (target == AuthTarget.Model && General.ContainsKey(permission))
                General.Remove(permission);
            if (target == AuthTarget.Message && Messages.ContainsKey(permission))
                Messages.Remove(permission);
            if (target == AuthTarget.Field && Fields.ContainsKey(permission))
                Fields.Remove(permission);

        }
        public AuthModel(string modelname)
        {
            ModelName = modelname;
            General = new Dictionary<string, AuthObject>();
            Messages = new Dictionary<string, AuthObject>();
            Fields = new Dictionary<string, AuthObject>();
        }

        AuthObject GetAuthObj(string name, AuthTarget target)
        {
            switch (target)
            {
                case AuthTarget.Model:
                    if (!General.ContainsKey(name))
                        General[name] = new AuthObject(name, target);
                    return General[name];
                case AuthTarget.Message:
                    if (!Messages.ContainsKey(name))
                        Messages[name] = new AuthObject(name, target);
                    return Messages[name];
                case AuthTarget.Field:
                    if (!Fields.ContainsKey(name))
                        Fields[name] = new AuthObject(name, target);
                    return Fields[name];
            }
            return null;
        }

        public void Push(string name, AuthTarget target, string role = null, string user = null)
        {
            var obj = GetAuthObj(name, target);
            if (!string.IsNullOrEmpty(role) && !obj.Roles.Contains(role))
                obj.Roles.Add(role);
            if (!string.IsNullOrEmpty(user) && !obj.Users.Contains(user))
                obj.Users.Add(user);
        }

        public void Push(AuthObject obj)
        {
            switch (obj.Target)
            {
                case AuthTarget.Field: Fields[obj.Name] = obj; break;
                case AuthTarget.Message: Messages[obj.Name] = obj; break;
                case AuthTarget.Model: General[obj.Name] = obj; break;
            }
        }

        static Dictionary<string, AuthModel> m_authmodels;
        public static Dictionary<string, AuthModel> AuthModels
        {
            get
            {
                if (m_authmodels == null)
                    m_authmodels = new Dictionary<string, AuthModel>();
                return m_authmodels;
            }
        }
    }
}
