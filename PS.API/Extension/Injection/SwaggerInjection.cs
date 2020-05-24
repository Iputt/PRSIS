using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
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
                //c.OperationFilter<AddAuthTokenHeaderParameter>();
            });
        }
    }

    //public class AddAuthTokenHeaderParameter : IOperationFilter
    //{
    //    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    //    {

    //        if (operation.Parameters == null)
    //        {
    //            operation.Parameters = new List<IParameter>();
    //        }
    //        operation.Parameters.Add(new NonBodyParameter()
    //        {
    //            Name = "token",
    //            In = "header",
    //            Type = "string",
    //            Description = "token认证信息",
    //            Required = true
    //        });
    //    }
    //}
}
