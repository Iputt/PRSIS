using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PS.Model;
using PS.Provider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PS.API.Extension
{
    /// <summary>
    /// 服务注入 - Appsetting
    /// </summary>
    public class AppSettingInjectioon
    {
        /// <summary>
        /// 初始化 - Appsetting配置
        /// </summary>
        /// <param name="services"></param>
        public static void Initialize(IServiceCollection services,IConfiguration configuration)
        {
            var conn = configuration.GetConnectionString("MyConn");
            //添加数据库服务 - MySql
            //services.AddSingleton(typeof(IMySqlService), new MySqlService(_conn){});
            services.Add(new ServiceDescriptor(typeof(AppDbContext), new AppDbContext(conn) {})) ;

            //var cache = configuration.GetSection("CacheService") as CacheConfig;
            //Configure<CacheConfig>(configuration.GetSection("CacheService"));
       
            //添加缓存服务 - Redis/MemoryCache
            //CacheInjection.Initialize(services, cache.RedisConnection, cache.InstanceName, cache.IsRedis);
        }
    }
}
