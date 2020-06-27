using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Domain;
using Owl.Util;
using System.Reflection;
namespace Owl.Feature.iAuth
{
    /// <summary>
    /// 账户过滤对象
    /// </summary>
    public class AccountFilterObj : SmartObject
    {
        /// <summary>
        /// 对象名
        /// </summary>
        public string Model { get; set; }

        /// <summary>
        /// 表达式
        /// </summary>
        public Specification Expr { get; set; }

        /// <summary>
        /// 是否不附加上级对象表达式
        /// </summary>
        public bool Override { get; set; }
    }

    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class AccountFilterAttribute : Attribute
    {
        /// <summary>
        /// 账户类型
        /// </summary>
        public string AccountType { get; private set; }

        /// <summary>
        /// 对象名
        /// </summary>
        public string Model { get; private set; }

        /// <summary>
        /// 表达式
        /// </summary>
        public Specification Expr { get; private set; }

        /// <summary>
        /// 是否不附加上级对象表达式
        /// </summary>
        public bool Override { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="accounttype">账户类型</param>
        /// <param name="model">对象名</param>
        /// <param name="expr">表达式</param>
        /// <param name="_override">是否覆盖上级对象表达式</param>
        public AccountFilterAttribute(string accounttype, string model, string expr, bool _override = false)
        {
            if (string.IsNullOrEmpty(accounttype))
                throw new ArgumentNullException("accounttype");
            if (string.IsNullOrEmpty(model))
                throw new ArgumentNullException("model");
            if (string.IsNullOrEmpty(expr))
                throw new ArgumentNullException("expr");
            AccountType = accounttype;
            Model = model.Trim().ToLower();
            Expr = Specification.Create(expr);
            Override = _override;
        }

        public AccountFilterAttribute(string accounttype, Type model, string expr, bool _override = false)
            : this(accounttype, model.MetaName(), expr, _override)
        {

        }
    }

    public abstract class FilterProvider : Provider
    {
        public virtual Specification Filter(Guid? partnerid, string model, string tag, IEnumerable<string> groups) { return null; }

        public virtual Specification Filter(Guid? actionid, string widget, string model, string mandt, IEnumerable<string> roles) { return null; }

        public virtual Dictionary<string, OpScope> GetScope(Guid? partnerid, string model, string tag, IEnumerable<string> groups) { return null; }

        public virtual RoleScope GetScope(string mandt, IEnumerable<string> rolenames, Guid actionid, string widget) { return null; }
        /// <summary>
        /// 账户数据过滤
        /// </summary>
        /// <param name="model"></param>
        /// <param name="accounttype"></param>
        /// <returns></returns>
        public virtual AccountFilterObj FilterAccount(string model, string accounttype) { return null; }

        public virtual IEnumerable<Tuple<Specification, string>> GetValidators(Guid actionid, string widget, string mandt, IEnumerable<string> roles) { return null; }
    }


    public class MetaFilterProvider : FilterProvider
    {
        Dictionary<string, AccountFilterObj> m_filters = new Dictionary<string, AccountFilterObj>();
        public MetaFilterProvider()
        {
            AsmHelper.RegisterResource(Load, UnLoad);
        }
        void Load(string name, Assembly asm)
        {
            foreach (var attr in asm.GetCustomAttributes(typeof(AccountFilterAttribute), false).Cast<AccountFilterAttribute>())
            {
                m_filters[string.Format("{0}_{1}", attr.AccountType, attr.Model)] = new AccountFilterObj() { Model = attr.Model, Expr = attr.Expr, Override = attr.Override };
            }

        }
        void UnLoad(string name, Assembly asm)
        {

        }
        public override AccountFilterObj FilterAccount(string model, string accounttype)
        {
            var key = string.Format("{0}_{1}", accounttype, model);
            return m_filters.ContainsKey(key) ? m_filters[key] : null;
        }
        public override int Priority
        {
            get { return 1; }
        }
    }

    public class FilterEngine : Engine<FilterProvider, FilterEngine>
    {
        /// <summary>
        /// 根据对象过滤
        /// </summary>
        /// <param name="model">对象名</param>
        /// <param name="tag">过滤标签</param>
        /// <param name="groups">用户组</param>
        /// <returns></returns>
        public static Specification Filter(Guid? partnerid, string model, string tag, IEnumerable<string> groups)
        {
            return Execute2<Guid?, string, string, IEnumerable<string>, Specification>(s => s.Filter, partnerid, model, tag, groups);
        }

        public static Specification Filter(Guid? actionid, string widget, string model, string mandt, IEnumerable<string> roles)
        {
            return Execute2<Guid?, string, string, string, IEnumerable<string>, Specification>(s => s.Filter, actionid, widget, model, mandt, roles);
        }
        /// <summary>
        /// 账户数据过滤
        /// </summary>
        /// <param name="meta"></param>
        /// <param name="accounttype"></param>
        /// <returns></returns>
        static List<Specification> FilterAccount(ModelMetadata meta, string accounttype)
        {
            var result = new List<Specification>();
            if (meta != null)
            {
                var filterobj = Execute2<string, string, AccountFilterObj>(s => s.FilterAccount, meta.Name, accounttype);
                if (filterobj != null)
                {
                    if (filterobj.Expr != null)
                        result.Add(filterobj.Expr);
                    if (filterobj.Override)
                        return result;
                }
            }
            var basetype = meta.ModelType.BaseType;
            if (basetype != typeof(AggRoot))
                result.AddRange(FilterAccount(ModelMetadataEngine.GetModel(basetype), accounttype));
            return result;
        }
        /// <summary>
        /// 账户数据过滤
        /// </summary>
        /// <param name="model"></param>
        /// <param name="accounttype"></param>
        /// <returns></returns>
        public static Specification FilterAccount(string model, string accounttype)
        {
            var exprs = FilterAccount(ModelMetadataEngine.GetModel(model), accounttype);
            return exprs.Count == 0 ? null : Specification.And(exprs.ToArray());
        }
        /// <summary>
        /// 获取操作范围
        /// </summary>
        /// <param name="model">对象名</param>
        /// <param name="tag">过滤组标记</param>
        /// <param name="groups">用户组</param>
        /// <returns></returns>
        public static Dictionary<string, OpScope> GetScope(Guid? partnerid, string model, string tag, IEnumerable<string> groups)
        {
            return Execute2<Guid?, string, string, IEnumerable<string>, Dictionary<string, OpScope>>(s => s.GetScope, partnerid, model, tag, groups);
        }
        /// <summary>
        /// 获取角色操作范围
        /// </summary>
        /// <param name="mandt"></param>
        /// <param name="roles"></param>
        /// <param name="actionid"></param>
        /// <param name="widget"></param>
        /// <returns></returns>
        public static RoleScope GetScope(string mandt, IEnumerable<string> roles, Guid actionid, string widget)
        {
            return Execute2<string, IEnumerable<string>, Guid, string, RoleScope>(s => s.GetScope, mandt, roles, actionid, widget);
        }

        public static IEnumerable<Tuple<Specification, string>> GetValidators(Guid actionid, string widget, string mandt, IEnumerable<string> roles)
        {
            return Execute2<Guid, string, string, IEnumerable<string>, IEnumerable<Tuple<Specification, string>>>(s => s.GetValidators, actionid, widget, mandt, roles);
        }
    }
}
