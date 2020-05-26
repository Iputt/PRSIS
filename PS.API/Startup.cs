using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Swashbuckle.AspNetCore.SwaggerUI;
using PS.API.Extension;
using AutoMapper;

namespace PS.API
{
    /// <summary>
    /// 启动
    /// </summary>
    public class Startup
    {
        /// <summary>
        /// 配置类-依赖
        /// </summary>
        private IConfiguration Configuration;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="configuration"></param>
        public Startup(IConfiguration configuration)//CacheConfig cacheConfig
        {
            Configuration = configuration;
        }
        
        /// <summary>
        /// 配置服务
        /// </summary>
        /// <param name="services"></param>
        public void ConfigureServices(IServiceCollection services)
        {
            //添加Controllers服务
            services.AddControllers();

            //添加[获取AppSetting]服务
            AppSettingInjectioon.Initialize(services, Configuration);

            //添加Cors跨域服务
            CorsInjection.Initialize(services);

            //添加Swagger服务
            SwaggerInjection.Initialize(services);

            //添加AutoMapper服务
            services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

            services.AddSingleton<ILogin, LoginRepository>();
        }

        /// <summary>
        /// 配置请求管道
        /// </summary>
        /// <param name="app"></param>
        /// <param name="env"></param>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            //判断是否未开发环境
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            //http
            app.UseHttpsRedirection();

            //路由
            app.UseRouting();

            //授权
            app.UseAuthorization();

            //启用中间件服务生成Swagger作为JSON终结点
            app.UseSwagger();

            //启用中间件服务对Swagger-UI，指定Swagger作为JSON终结点
            app.UseSwaggerUI(c =>
            {
                //生成json文档
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "PS API V1");
                //如果设置根目录为swagger，将此值置空
                c.RoutePrefix = "";
                c.DocExpansion(DocExpansion.None);
                c.DefaultModelsExpandDepth(-1);
            });

            //启用跨域代理服务
            app.UseCors("CorsPolicy");

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
