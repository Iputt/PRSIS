using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PS.API.Extension
{
    /// <summary>
    /// 服务注入 - Swagger
    /// </summary>
    public class SwaggerInjection
    {
        /// <summary>
        /// 初始化 - Swagger配置
        /// </summary>
        /// <param name="services"></param>
        public static void Initialize(IServiceCollection services)
        {
            //添加Swagger服务
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "PS API",
                    Description = "Acting on PS",
                    Version = "v1",
                    //TermsOfService="None"
                    //Contact
                    //Licecse
                });
                //为Swagger JSON and UI设置xml文档注释路径
                string xmlPath = Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location), "ps_swagger.xml");
                c.IncludeXmlComments(xmlPath);
            });
        }
    }
}
