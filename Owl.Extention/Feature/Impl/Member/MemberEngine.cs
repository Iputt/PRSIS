using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Util;
using Owl.Domain;
namespace Owl.Feature.Impl.iMember
{
    /// <summary>
    /// 成员管理引擎
    /// </summary>
    public class MemberEngine : Engine<MemberProvider, MemberEngine>
    {
        protected override EngineMode Mode
        {
            get
            {
                return EngineMode.Multiple;
            }
        }
        protected override bool SkipException
        {
            get
            {
                return false;
            }
        }

        protected override object Invoke(string method, object[] args)
        {
            return base.Invoke(method, args);
        }

        public static bool IsValid(string mandt, string login, string md5pwd)
        {
            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(md5pwd))
                throw new Exception("用户名或密码不能为空！");
            if (mandt == "")
                mandt = null;
            return Execute2<string,string,string,bool>(s => s.IsValid, mandt, login, md5pwd);
            //return Providers.FirstOrDefault().IsValid(mandt, login, md5pwd);
        }
        /// <summary>
        /// 登录系统
        /// </summary>
        /// <param name="mandt">客户端</param>
        /// <param name="login">登录名</param>
        /// <param name="pwd">密码</param>
        /// <param name="challenge">挑战信息</param>
        public static void Login(string mandt, string login, string pwd, string challenge = null)
        {
            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(pwd))
                throw new Exception("用户名或密码不能为空！");
            Execute(s => s.Login, mandt ?? "", login, pwd, challenge);
            //Providers.FirstOrDefault().Login(mandt ?? "", login, pwd, challenge);
        }


        public static bool Exists(string mandt, string login)
        {
            return Execute2<string, string, bool>(s => s.Exists, mandt ?? "", login);
        }

        /// <summary>
        /// 创建登录帐号
        /// </summary>
        /// <param name="login">登录名</param>
        /// <param name="email">电子邮件</param>
        /// <param name="name">用户姓名</param>
        /// <returns>创建的密码</returns>
        public static string CreateUser(string mandt, string login, string email, string name)
        {
            mandt = mandt ?? "";
            if (Exists(mandt, login))
                throw new Exception("创建失败：帐号已存在！");
            return Execute2<string, string, string, string, string>(s => s.CreateUser, mandt, login, email, name);
        }


        /// <summary>
        /// 根据登录名称获取成员
        /// </summary>
        /// <param name="login"></param>
        /// <returns></returns>
        public static Member GetMember(string mandt, string login)
        {
            var member = Execute2<string, string, Member>(s => s.GetMember, mandt ?? "", login);
            Execute(s => s.AddExtention, member);
            return member;
        }
    }
}
