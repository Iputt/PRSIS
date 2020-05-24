using System;
using System.Collections.Generic;
using System.Text;

namespace PS.External.Model
{
    /// <summary>
    /// 接受参数 - 登录信息
    /// </summary>
    public class PLoginDto
    {
        /// <summary>
        /// 昵称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 账号
        /// </summary>
        public string Account { get; set; }
    }
}
