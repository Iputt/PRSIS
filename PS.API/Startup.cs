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
            //���Controllers����
            services.AddControllers();

            //���[��ȡAppSetting]����
            AppSettingInjectioon.Initialize(services, Configuration);

            //���Cors�������
            CorsInjection.Initialize(services);

            //���Swagger����
            SwaggerInjection.Initialize(services);

            //���AutoMapper����
            services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

            services.AddSingleton<ILogin, LoginRepository>();
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
            //http
            app.UseHttpsRedirection();

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
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "PS API V1");
                //������ø�Ŀ¼Ϊswagger������ֵ�ÿ�
                c.RoutePrefix = "";
                c.DocExpansion(DocExpansion.None);
                c.DefaultModelsExpandDepth(-1);
            });

            //���ÿ���������
            app.UseCors("CorsPolicy");

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
