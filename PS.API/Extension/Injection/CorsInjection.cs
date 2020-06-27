using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PS.API.Extension
{
    /// <summary>
    /// 服务注入 - CORS 跨域
    /// </summary>
    public class CorsInjection
    {
        /// <summary>
        /// 初始化 - CORS配置
        /// </summary>
        /// <param name="services"></param>
        public static void Initialize(IServiceCollection services)
        {
            //添加Cors服务
            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy", builder =>
                {
                    //配置允许所有域名通过跨域，builder.AllowAnyOrigin()，客户端请求的时候携带cookie或者其他参数的时候出现以下错误，必须通过builder.WithOrigins()指定域名
                    builder.AllowAnyOrigin(); //客户端不携带cookie时，可以配置
                    //builder.WithOrigins(ConfigHelper.GetSectionModel<List<string>>("CorsOrigins").ToArray()); //客户端携带cookie、或者在请求报文定义其他字段属性时，必须指定域名
                    builder.AllowAnyHeader();
                    builder.AllowAnyMethod();
                    builder.AllowCredentials();
                    builder.SetPreflightMaxAge(TimeSpan.FromSeconds(60));  //如果接口已验证过一次跨域，则在60秒内再次请求时，将不需要验证跨域
                });

            });
        }
    }
}
