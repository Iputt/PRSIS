using Microsoft.Extensions.DependencyInjection;
using PS.GeneralProvider.Cache;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PS.API.Extension
{
    public class CacheInjection
    {
        public static void Initialize(IServiceCollection services, string redisCon, string instanceName, bool isRedis = false)
        {
            //注册缓存服务
            services.AddMemoryCache();
            if (isRedis)
            {
                //Use Redis
                services.AddSingleton(typeof(ICacheService), new RedisCacheService(new RedisCacheOptions
                {
                    Configuration = redisCon,
                    InstanceName = instanceName
                }, 0));
            }
            else
            {
                //Use MemoryCache
                services.AddSingleton<IMemoryCache>(factory =>
                {
                    var cache = new MemoryCache(new MemoryCacheOptions());
                    return cache;
                });
                services.AddSingleton<ICacheService, MemoryCacheService>();
            }
        }
    }
}
