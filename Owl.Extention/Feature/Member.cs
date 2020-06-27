using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Domain;
using Owl.Util;
using Owl.Feature.Impl.iMember;
namespace Owl.Feature
{
    /// <summary>
    /// 操作范围
    /// </summary>
    public enum OpScope
    {
        /// <summary>
        /// 所有的
        /// </summary>
        [DomainLabel("所有的")]
        All = 7,

        /// <summary>
        /// 自己的和下级部门的
        /// </summary>
        [DomainLabel("自己的")]
        MySelf = 1,

        /// <summary>
        /// 本部门的和下级部门的
        /// </summary>
        [DomainLabel("本部门的")]
        Department = 2,

        [DomainLabel("本公司的")]
        Bukrs = 4
    }

    /// <summary>
    /// 帐户
    /// </summary>
    public class Account : SmartObject
    {
        /// <summary>
        ///帐户Id 
        /// </summary>
        public Guid? Id { get; set; }
        /// <summary>
        /// 帐户名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 账户代码
        /// </summary>
        public string Code { get; set; }
        /// <summary>
        /// 帐户类型
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// 管理账号
        /// </summary>
        public string Login { get; set; }
    }
    /// <summary>
    /// 成员扩展
    /// </summary>
    public abstract class MemberExtention : SmartObject
    {
        public Member Member { get; internal set; }
        /// <summary>
        /// 扩展别名
        /// </summary>
        public virtual string Alias
        {
            get { return GetType().Name.Replace("Extention", ""); }
        }
    }
    /// <summary>
    /// 成员关系
    /// </summary>
    public class MemberShip : SmartObject
    {
        /// <summary>
        /// 公司
        /// </summary>
        public string BUKRS { get; set; }
        /// <summary>
        /// 组织结构
        /// </summary>
        public string Organization { get; set; }
        /// <summary>
        /// 部门
        /// </summary>
        public string Department { get; set; }

        /// <summary>
        /// 所有部门
        /// </summary>
        public IEnumerable<string> Departments { get; set; }
        /// <summary>
        /// 下级部门
        /// </summary>
        public IEnumerable<string> Children { get; set; }
    }

    /// <summary>
    /// 成员
    /// </summary>
    public class Member : SmartObject
    {
        /// <summary>
        /// 成员的标示符
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// 登录名
        /// </summary>
        public string Login { get; set; }

        /// <summary>
        /// 代码
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// 用户名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 集团/租户/客户
        /// </summary>
        public string MANDT { get; set; }

        /// <summary>
        /// 我的主要公司
        /// </summary>
        public string BUKRS { get; set; }

        /// <summary>
        /// 我的所有公司
        /// </summary>
        public IEnumerable<string> AllBUKRS { get; set; }
        /// <summary>
        /// 缺省组织结构
        /// </summary>
        public string Org { get; set; }

        /// <summary>
        /// 我的主要部门
        /// </summary>
        public string Department { get; set; }

        /// <summary>
        /// 组织结构关系
        /// </summary>
        public IEnumerable<MemberShip> Ships { get; set; }

        /// <summary>
        /// 我的所有部门
        /// </summary>
        public IEnumerable<string> Departments { get; set; }

        /// <summary>
        /// 我的所有子部门
        /// </summary>
        public IEnumerable<string> ChildDepartments { get; set; }


        /// <summary>
        /// 用户组
        /// </summary>
        public IEnumerable<string> Groups { get; set; }

        /// <summary>
        /// 角色
        /// </summary>
        public HashSet<string> Roles { get; set; }

        /// <summary>
        /// 首页
        /// </summary>
        public string HomePage { get; set; }

        /// <summary>
        /// 当前用户所属的帐户
        /// </summary>
        public Account Account { get; set; }


        /// <summary>
        /// 是否管理员
        /// </summary>
        public bool IsAdmin
        {
            get
            {
                var superuser = Config.Section<SysSection>().SuperUser;
                if (string.IsNullOrEmpty(superuser))
                    return false;
                return string.IsNullOrEmpty(MANDT) && Login.ToLower() == superuser.ToLower();
            }
        }


        Dictionary<Type, string> m_alias = new Dictionary<Type, string>();

        /// <summary>
        /// 添加扩展
        /// </summary>
        /// <param name="ext"></param>
        public void AddExtention(MemberExtention ext)
        {
            ext.Member = this;
            this[ext.Alias] = ext;
            m_alias[ext.GetType()] = ext.Alias;
        }
        /// <summary>
        /// 获取扩展
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetExtention<T>()
            where T : MemberExtention
        {
            var type = typeof(T);
            var alias = "";
            if (m_alias.ContainsKey(type))
                alias = m_alias[type];
            if (!string.IsNullOrEmpty(alias))
                return this[alias] as T;
            return null;
        }

        /// <summary>
        /// 获取对象的授权
        /// </summary>
        /// <param name="modelname">对象名称</param>
        /// <returns></returns>
        public Permission GetAuth(string modelname, Guid actionid)
        {
            if (IsAdmin)
                return Permission.All;
            return Auth.GetAuth(actionid, modelname, Login, Roles);
        }
        /// <summary>
        /// 获取对象过滤
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public Specification Filter(string model, Guid? actionid = null, string widget = null, string tag = null)
        {
            if (actionid == Guid.Empty)
                actionid = null;
            var innerfilter = Auth.Filter(actionid, widget, model, MANDT, Roles);
            if (innerfilter == null)
                innerfilter = Auth.Filter(Account.Id, model, tag, Groups);

            return Specification.And(Auth.FilterAccount(model, Account.Type), innerfilter);
        }

        /// <summary>
        /// 获取没有权限的字段
        /// </summary>
        /// <param name="modelname">对象名称</param>
        public HashSet<string> DenyFields(string modelname)
        {
            var fields = new HashSet<string>();
            var meta = ModelMetadataEngine.GetModel(modelname);
            if (meta == null)
                return fields;
            foreach (var field in meta.GetFieldNames())
            {
                if (IsAdmin || Auth.HasPower(null, field, modelname, AuthTarget.Field, Login, Roles))
                    continue;
                fields.Add(field);
            }
            return fields;
        }
        /// <summary>
        /// 判断是否拥有执行消息的权限
        /// </summary>
        /// <param name="model">对象名称</param>
        /// <param name="msgname">消息名称</param>
        /// <returns></returns>
        public bool HasPower(Guid actionid, string model, string msgname)
        {
            if (IsAdmin)
                return true;
            if (Roles.Count == 0)
                return false;
            return Auth.HasPower(actionid, msgname, model, AuthTarget.Message, Login, Roles);
        }
        /// <summary>
        /// 用户验证
        /// </summary>
        /// <param name="mandt"></param>
        /// <param name="login"></param>
        /// <param name="md5pwd"></param>
        /// <returns></returns>
        public static bool Validate(string mandt, string login, string md5pwd)
        {
            return MemberEngine.IsValid(mandt, login, md5pwd);
        }

        public static void Login2(string mandt, string login, string pwd, string challenge = null)
        {
            MemberEngine.Login(mandt, login, pwd, challenge);
        }

        public static bool Exists(string mandt, string login)
        {
            return MemberEngine.Exists(mandt, login);
        }

        public static string CreateUser(string mandt, string login, string email, string name)
        {
            return MemberEngine.CreateUser(mandt, login, email, name);
        }

        static readonly string currentkey = "owl.feature.member.current";
        /// <summary>
        /// 当前成员
        /// </summary>
        public static Member Current
        {
            get
            {
                var member = Cache.Session<Member>(currentkey);
                if (member == null && !string.IsNullOrEmpty(OwlContext.Current.UserName))
                {
                    lock (Cache.SessionId)
                    {
                        member = Cache.Session<Member>(currentkey);
                        if (member == null)
                        {
                            member = MemberEngine.GetMember(OwlContext.Current.Mandt, OwlContext.Current.UserName);
                            Cache.Session(currentkey, member);
                        }
                    }
                }
                return member;
            }
            set
            {
                Cache.Session(currentkey, value);
            }
        }
    }
}
