using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace PS.Core.App
{
    public class TokenHelper
    {
        public string GetToken<T>(T t)
        {
            var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(Contanst.JwtSecurityKey));
            var claims = new Claim[]
            {
            new Claim(ClaimTypes.Name,""),//user.userAccount
            new Claim(ClaimTypes.NameIdentifier,""),//user.userId
            new Claim(ClaimTypes.Role,""),//user.userRole
            new Claim(ClaimTypes.Actor,""),//user.userName
            };
            var expires = DateTime.Now.AddHours(12);//生命周期 12小时
            var token = new JwtSecurityToken(
                        issuer: "",//user.userName,//非必须。issuer 请求实体，可以是发起请求的用户的信息，
                        audience: "http://example.com",//非必须。接收该JWT的一方。
                        claims: claims,
                        notBefore: DateTime.Now,
                        expires: expires,
                        signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

            //生成Token
            string jwtToken = new JwtSecurityTokenHandler().WriteToken(token);
            return jwtToken;
        }
    }
}
