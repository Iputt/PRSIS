using System;

namespace PS.Model
{
    /// <summary>
    /// 用户登录信息
    /// </summary>
    public class Login
    {
        /// <summary>
        /// 姓
        /// </summary>
        public string FirstName { get; set; }

        /// <summary>
        /// 名
        /// </summary>
        public string LastName { get; set; }

        /// <summary>
        /// 账号
        /// </summary>
        public string Account { get; set; }

        /// <summary>
        /// 密码
        /// </summary>
        public string Pwd { get; set; }

        /// <summary>
        /// 性别
        /// </summary>
        public int Getder { get; set; }

        /// <summary>
        /// 电子邮件
        /// </summary>
        public string Email { get; set; }
    }
}
