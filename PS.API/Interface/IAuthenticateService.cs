using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PS.API.Extension;
using PS.External.Model;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace PS.API.Interface
{
    /// <summary>
    /// 接口 - Jwt验证
    /// </summary>
    public interface IAuthenticateService
    {
        /// <summary>
        /// 是否通过验证
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        bool IsAuthenticated(PLoginDto dto, out string token);
    }

    /// <summary>
    /// 实现 - Jwt验证
    /// </summary>
    public class TokenAuthenticationService : IAuthenticateService
    {
        private readonly ILogin _login;
        private readonly TokenManagement _tokenManagement;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="login"></param>
        /// <param name="tokenManagement"></param>
        public TokenAuthenticationService(ILogin login,IOptions<TokenManagement> tokenManagement)
        {
            _login = login;
            _tokenManagement = tokenManagement.Value;
        }

        /// <summary>
        /// 是否通过验证
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public bool IsAuthenticated(PLoginDto dto,out string token)
        {
            token = string.Empty;
            if (!_login.IsValid(dto))
                return false;
            //声明
            var claims = new[]
            {
                //new Claim(JwtRegisteredClaimNames.Nbf,$"{new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds()}"),
                //new Claim(JwtRegisteredClaimNames.Exp,$"{new DateTimeOffset(DateTime.Now.AddMinutes(30)).ToUnixTimeSeconds()}"),
                new Claim(ClaimTypes.Name,dto.Name)
            };
            //密钥
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_tokenManagement.Secret));
            //凭据
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var jwtToken = new JwtSecurityToken(
                _tokenManagement.Issuer,
                _tokenManagement.Audience,
                claims:claims,
                expires:DateTime.Now.AddMinutes(_tokenManagement.AccessExpiration),
                signingCredentials:credentials);
            token = new JwtSecurityTokenHandler().WriteToken(jwtToken);
            return true;
        }
    }

}
