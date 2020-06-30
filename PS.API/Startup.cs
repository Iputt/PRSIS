using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Swashbuckle.AspNetCore.SwaggerUI;
using PS.API.Extension;
using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using PS.API.Interface;
using PS.API.Extension.Injection;
using PS.API.Extension.Jwt;
using Newtonsoft.Json.Serialization;

namespace PS.API
{
    /// <summary>
    /// ����
    /// </summary>
    public class Startup
    {
        /// <summary>
        /// ������-����
        /// </summary>
        private IConfiguration Configuration;

        /// <summary>
        /// ���캯��
        /// </summary>
        /// <param name="configuration"></param>
        public Startup(IConfiguration configuration)//CacheConfig cacheConfig
        {
            Configuration = configuration;
        }
        
        /// <summary>
        /// ���÷���
        /// </summary>
        /// <param name="services"></param>
        public void ConfigureServices(IServiceCollection services)
        {
            //���[��ȡAppSetting]����
            AppSettingInjectioon.Initialize(services, Configuration);

            //���Cors�������
            //CorsInjection.Initialize(services);

            //���JWT��֤����
            JwtInjection.Initialize(services, Configuration);
            //services.AddTransient<IJwt, Jwt>();

            //���Swagger����
            SwaggerInjection.Initialize(services);

            //���AutoMapper����
            services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

            //���Controllers����
            services.AddControllers().AddNewtonsoftJson(option=> {
                option.SerializerSettings.ContractResolver = new DefaultContractResolver();
            });

            //���Api�汾����
            services.AddApiVersioning(option => {
                option.ReportApiVersions = true;
                option.AssumeDefaultVersionWhenUnspecified = true;
                option.DefaultApiVersion = new Microsoft.AspNetCore.Mvc.ApiVersion(1, 0);
            });

            services.AddSingleton<ILogin, LoginRepository>();

            services.AddScoped<IAuthenticateService, TokenAuthenticationService>();
        }

        /// <summary>
        /// ��������ܵ�
        /// </summary>
        /// <param name="app"></param>
        /// <param name="env"></param>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            //�ж��Ƿ�δ��������
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            //���ÿ���������
            //app.UseCors("CorsPolicy");

            //http
            app.UseHttpsRedirection();

            //Jwt��֤
            app.UseAuthentication();
            //app.UseJwt();

            //·��
            app.UseRouting();

            //��Ȩ
            app.UseAuthorization();

            //�����м����������Swagger��ΪJSON�ս��
            app.UseSwagger();

            //�����м�������Swagger-UI��ָ��Swagger��ΪJSON�ս��
            app.UseSwaggerUI(c =>
            {
                //����json�ĵ�
                c.SwaggerEndpoint("/swagger/v2/swagger.json", "PS API V2");
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "PS API V1");
                //������ø�Ŀ¼Ϊswagger������ֵ�ÿ�
                c.RoutePrefix = "";
                c.DocExpansion(DocExpansion.None);
                c.DefaultModelsExpandDepth(-1);
            });

            //�˵�����       
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
