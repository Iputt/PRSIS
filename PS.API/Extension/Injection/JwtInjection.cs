using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PS.API.Extension.Injection
{
    /// <summary>
    /// 服务注入 - Jwt验证
    /// </summary>
    public class JwtInjection
    {
        /// <summary>
        /// 初始化 - Jwt配置
        /// </summary>
        /// <param name="services"></param>
        public static void Initialize(IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<TokenManagement>(configuration.GetSection("TokenConfig"));

            var token = configuration.GetSection("TokenConfig").Get<TokenManagement>();

            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(x =>
            {
                x.RequireHttpsMetadata = false;
                x.SaveToken = true;
                //验证参数
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    //是否验证发行者签名
                    ValidateIssuerSigningKey = true,
                    //获取设置密钥
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(token.Secret)),
                    //获取或设置发行者
                    ValidIssuer = token.Issuer,
                    //获取或设置订阅者
                    ValidAudience = token.Audience,
                    //是否验证发行者
                    ValidateIssuer = false,
                    //是否验订阅者
                    ValidateAudience = false,
                    //是否验证失效时间
                    ValidateLifetime=true,
                    //设置时间滑动
                    ClockSkew=TimeSpan.FromSeconds(30),
                };
            });
        }
    }
}
