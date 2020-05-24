using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
        private static CacheConfig _cache;
        private static string _conn;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="configuration"></param>
        public AppSettingInjectioon(IConfiguration configuration)
        {
            _conn = configuration["ConnectionStrings:MyConn"];

            CacheConfig cache = configuration.GetSection("CacheService") as CacheConfig;
            _cache = cache ?? throw new ArgumentException();
        }

        /// <summary>
        /// 初始化 - Appsetting配置
        /// </summary>
        /// <param name="services"></param>
        public static void Initialize(IServiceCollection services)
        {
            
            //添加数据库服务 - MySql
            services.AddSingleton(typeof(IMySqlService), new MySqlService(_conn){});

            //添加缓存服务 - Redis/MemoryCache
            CacheInjection.Initialize(services, _cache.RedisConnection, _cache.InstanceName, _cache.IsRedis);
        }
    }
}
