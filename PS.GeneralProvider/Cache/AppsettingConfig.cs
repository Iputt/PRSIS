using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PS.GeneralProvider.Cache
{
    /// <summary>
    /// 映射 - 缓存配置
    /// </summary>
    public class AppsettingConfig
    {
        /// <summary>
        /// Redis 缓存
        /// </summary>
        public string RedisConnection { get; set; }

        /// <summary>
        /// 实例名称
        /// </summary>
        public string InstanceName { get; set; }

        /// <summary>
        /// 是否使用Redis
        /// </summary>
        public bool IsRedis { get; set; }
    }

}
