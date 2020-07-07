using Microsoft.AspNetCore.Mvc;
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
                c.SwaggerDoc("v2", new OpenApiInfo
                {
                    Title = "PS API",
                    Description = "Acting on PS",
                    Version = "v2",
                    //TermsOfService="None"
                    //Contact
                    //Licecse
                });

                c.DocInclusionPredicate((docName, apiDesc) =>
                {
                    var versions = apiDesc.CustomAttributes().OfType<ApiVersionAttribute>()
                    .SelectMany(attr => attr.Versions);

                    return versions.Any(v => $"v{v.ToString()}" == docName);
                });

                c.OperationFilter<RemoveVersionParameterOperationFilter>();
                c.DocumentFilter<SetVersionInPathDocumentFilter>();

                //为Swagger JSON and UI设置xml文档注释路径
                string xmlPath = Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location), "ps_swagger.xml");

                c.IncludeXmlComments(xmlPath,true);

                c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, $"externalmodel.xml"),true);
                //c.OperationFilter<AddAuthTokenHeaderParameter>();

                //添加Bearer Token验证
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    { new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference()
                        {
                            Id = "Bearer",
                            Type = ReferenceType.SecurityScheme
                        }
                    }, Array.Empty<string>() }
                });
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

    /// <summary>
    /// 自定义api版本注释过滤
    /// </summary>
    public class SetVersionInPathDocumentFilter : IDocumentFilter
    {
        public void Apply(OpenApiDocument swaggerDoc,DocumentFilterContext context)
        {
            var updatePaths = new OpenApiPaths();

            foreach(var entry in swaggerDoc.Paths)
            {
                updatePaths.Add(
                    entry.Key.Replace("v{version}", swaggerDoc.Info.Version),
                    entry.Value);
            }

            swaggerDoc.Paths = updatePaths;
        }
    }

    /// <summary>
    /// 自定义api版本参数过滤
    /// </summary>
    public class RemoveVersionParameterOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            // Remove version parameter from all Operations
            var versionParameter = operation.Parameters.Single(p => p.Name == "version");
            operation.Parameters.Remove(versionParameter);
        }
    }
}
