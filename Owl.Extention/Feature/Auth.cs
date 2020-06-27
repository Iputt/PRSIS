using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Domain;
using Owl.Feature.iAuth;

namespace Owl.Feature
{
    /// <summary>
    /// 指定范围
    /// </summary>
    public class SpecifyScope
    {
        /// <summary>
        /// 公司
        /// </summary>
        public string Bukrs { get; private set; }
        /// <summary>
        /// 部门
        /// </summary>
        public string Department { get; private set; }
        /// <summary>
        /// 员工
        /// </summary>
        public string Staff { get; private set; }

        public SpecifyScope(string bukrs, string department, string staff)
        {
            Bukrs = bukrs;
            Department = department;
            Staff = staff;
        }

        Specification specification;
        /// <summary>
        /// 表达式
        /// </summary>
        public Specification Specification
        {
            get
            {
                if (specification == null)
                {
                    List<Specification> specifications = new List<Specification>();
                    specifications.Add(Specification.Create("BUKRS", CmpCode.EQ, Bukrs));
                    if (!string.IsNullOrEmpty(Department))
                    {
                        specifications.Add(Specification.Create("DepartmentCode", CmpCode.EQ, Department));
                    }
                    if (!string.IsNullOrEmpty(Staff))
                        specifications.Add(Specification.Create("Submitor", CmpCode.EQ, Staff));
                    specification = Specification.And(specifications.ToArray());
                }
                return specification;
            }
        }

    }

    public class RoleScope
    {
        public OpScope Scope { get; set; }

        public List<SpecifyScope> Specifies { get; private set; }

        public List<Specification> Conditions { get; private set; }

        public RoleScope()
        {
            Scope = OpScope.MySelf;
            Specifies = new List<SpecifyScope>();
            Conditions = new List<Specification>();
        }
        public void AddSpecify(string bukrs, string department, string staff)
        {
            var bukrspedifies = Specifies.Where(s => s.Bukrs == bukrs).ToList();

            if (bukrspedifies.Count > 0 && string.IsNullOrEmpty(bukrspedifies[0].Department))
                return;

            if (string.IsNullOrEmpty(department))
            {
                foreach (var spec in bukrspedifies)
                {
                    Specifies.Remove(spec);
                }
                Specifies.Add(new SpecifyScope(bukrs, "", ""));
                return;
            }
            var departspedifies = bukrspedifies.Where(s => s.Department == department).ToList();
            if (departspedifies.Count > 0 && string.IsNullOrEmpty(departspedifies[0].Staff))
                return;
            if (string.IsNullOrEmpty(staff))
            {
                foreach (var spec in departspedifies)
                {
                    Specifies.Remove(spec);
                }
                Specifies.Add(new SpecifyScope(bukrs, department, ""));
                return;
            }
            if (departspedifies.Any(s => s.Staff == staff))
                return;
            Specifies.Add(new SpecifyScope(bukrs, department, staff));
        }

        public void ClearSpecify()
        {
            Specifies.Clear();
        }

        public Dictionary<string, Dictionary<string, string[]>> GetAll(Member member)
        {
            if (Scope != OpScope.All)
            {
                var nrscope = new RoleScope();
                foreach (var spec in Specifies)
                {
                    nrscope.AddSpecify(spec.Bukrs, spec.Department, spec.Staff);
                }
                if (Scope == OpScope.Bukrs)
                {
                    foreach (var bukrs in member.AllBUKRS)
                        nrscope.AddSpecify(bukrs, "", "");
                }
                else if (Scope == OpScope.Department)
                {
                    foreach (var ship in member.Ships)
                    {
                        foreach (var department in ship.Departments)
                        {
                            nrscope.AddSpecify(ship.BUKRS, department, "");
                        }
                        foreach (var child in ship.Children)
                            nrscope.AddSpecify(ship.BUKRS, child, "");
                    }
                }
                //else if (Scope == OpScope.MySelf)
                //{
                //    foreach (var ship in member.Ships)
                //    {
                //        foreach (var department in ship.Departments)
                //        {
                //            nrscope.AddSpecify(ship.BUKRS, department, member.Login);
                //        }
                //        //foreach (var child in ship.Children)
                //        //    nrscope.AddSpecify(ship.BUKRS, child, "");
                //    }
                //}
                return nrscope.Specifies
                    .GroupBy(s => s.Bukrs)
                    .ToDictionary(s => s.Key, s => s.GroupBy(t => t.Department).Where(t => !string.IsNullOrEmpty(t.Key)).ToDictionary(t => t.Key, t => t.Select(k => k.Staff).Where(k => !string.IsNullOrEmpty(k)).ToArray()));
            }
            return null;
        }
    }

    /// <summary>
    /// 验证授权
    /// </summary>
    public static class Auth
    {
        public static void Login(string login, string pwd)
        {

        }

        /// <summary>
        /// 获取所有角色类型
        /// </summary>
        /// <param name="accounttype"></param>
        public static Dictionary<string, RoleType> GetRoleTypes(string accounttype)
        {
            return RoleEngine.GetRoleTypes(accounttype);
        }
        /// <summary>
        /// 获取所有角色
        /// </summary>
        public static IEnumerable<Role> GetRoles()
        {
            return RoleEngine.GetRoles();
        }

        public static Role GetRole(string code)
        {
            return RoleEngine.GetRole(code);
        }
        /// <summary>
        /// 从指定角色中获取账户类型相关的可分配的角色
        /// </summary>
        /// <param name="roles"></param>
        /// <param name="accounttypes"></param>
        /// <returns></returns>
        public static IEnumerable<Role> FilterRoles(IEnumerable<Role> roles, params string[] accounttypes)
        {
            Dictionary<string, RoleType> roletypes = new Dictionary<string, RoleType>();
            foreach (var accounttype in accounttypes)
            {
                foreach (var roletype in GetRoleTypes(accounttype))
                {
                    if (!roletype.Value.NoAlloc)
                        roletypes[roletype.Key] = roletype.Value;
                }
            }
            var result = new List<Role>();
            foreach (var role in roles)
            {
                if (roletypes.ContainsKey(role.Type))
                    result.Add(role);
            }
            return result;
        }
        public static IEnumerable<Role> GetRoles(string accounttype)
        {
            var roletypes = GetRoleTypes(accounttype);
            var roles = GetRoles();
            var result = new List<Role>();
            foreach (var role in roles)
            {
                if (roletypes.ContainsKey(role.Type))
                    result.Add(role);
            }
            return result;
        }

        public static Permission GetAuth(Guid? actionid, string model, string user, IEnumerable<string> roles)
        {
            return AuthEngine.GetAuth(actionid, model, user, roles);
        }
        public static bool HasPower(Guid? actionid, string name, string model, AuthTarget target, string user, IEnumerable<string> roles)
        {
            return AuthEngine.HasPower(actionid, name, model, target, user, roles);
        }

        /// <summary>
        /// 对象过滤
        /// </summary>
        /// <param name="model">对象名称</param>
        /// <param name="tag">过滤标签</param>
        /// <param name="groups">用户组</param>
        /// <returns></returns>
        public static Specification Filter(Guid? partnerid, string model, string tag, IEnumerable<string> groups)
        {
            return FilterEngine.Filter(partnerid, model, tag, groups);
        }

        public static Specification Filter(Guid? actionid, string widget, string model, string mandt, IEnumerable<string> roles)
        {
            return FilterEngine.Filter(actionid, widget, model, mandt, roles);
        }

        /// <summary>
        /// 账户数据过滤
        /// </summary>
        /// <param name="model"></param>
        /// <param name="accounttype"></param>
        /// <returns></returns>
        public static Specification FilterAccount(string model, string accounttype)
        {
            return FilterEngine.FilterAccount(model, accounttype);
        }
        /// <summary>
        /// 获取当前用户的操作范围
        /// </summary>
        /// <param name="model">对象名称</param>
        /// <param name="tag">过滤组标记</param>
        /// <returns></returns>
        //public static OpScope GetScope(string model, string tag)
        //{
        //    return FilterEngine.GetScope(model,tag, Member.Current.Groups);
        //}
       
        public static Dictionary<string, OpScope> GetScope(string model, string tag)
        {
            var scope = FilterEngine.GetScope(null, model, tag, Member.Current.Groups);
            if (Member.Current.Account.Id == null)
            {
                if (scope.ContainsKey("empty") && Member.Current.Account.Login == Member.Current.Login)
                    scope["empty"] = OpScope.All;
            }
            return scope;
        }

        public static RoleScope GetScope(Guid actionid, string widget)
        {
            return FilterEngine.GetScope(Member.Current.MANDT, Member.Current.Roles, actionid, widget);
        }

        /// <summary>
        /// 获取动作的验证
        /// </summary>
        /// <param name="actionid">动作</param>
        /// <param name="widget">部件</param>
        /// <returns></returns>
        public static IEnumerable<Tuple<Specification, string>> GetValidators(Guid actionid, string widget = "main")
        {
            var member = Member.Current;
            return FilterEngine.GetValidators(actionid, widget, member.MANDT, member.Roles);
        }
    }
}
