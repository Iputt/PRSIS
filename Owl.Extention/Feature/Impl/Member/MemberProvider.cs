using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Util;
using Owl.Domain;
namespace Owl.Feature.Impl.iMember
{

    public abstract class MemberProvider : Provider
    {
        /// <summary>
        /// 判断账号是否可登录
        /// </summary>
        /// <param name="mandt"></param>
        /// <param name="login"></param>
        /// <param name="pwd">md5密码</param>
        /// <returns></returns>
        public abstract bool IsValid(string mandt, string login, string pwd);

        public abstract void Login(string mandt, string login, string pwd, string challenge);

        public abstract string CreateUser(string mandt, string login, string email, string name);

        public abstract bool Exists(string mandt, string login);

        public abstract Member GetMember(string mandt, string login);

        public virtual void AddExtention(Member member) { }
    }
}
