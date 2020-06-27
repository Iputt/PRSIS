using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PS.API.Extension.Jwt
{
    /// <summary>
    /// 接口 - Jwt
    /// </summary>
    public interface IJwt
    {
        /// <summary>
        /// 获取token
        /// </summary>
        /// <param name="Clims"></param>
        /// <returns></returns>
        string GetToken(Dictionary<string, string> Clims);

        /// <summary>
        /// 验证token
        /// </summary>
        /// <param name="Token"></param>
        /// <param name="Clims"></param>
        /// <returns></returns>
        bool ValidateToken(string Token, out Dictionary<string, string> Clims);
    }
}
