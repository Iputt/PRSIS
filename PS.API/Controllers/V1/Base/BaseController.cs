using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PS.API.Extension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PS.API.Controllers.Base
{
    /// <summary>
    /// 控制器 - 基础
    /// </summary>
    public class BaseController:ControllerBase
    {
        /// <summary>
        /// 缓存配置
        /// </summary>
        private CacheConfig Config;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="setting"></param>
        public BaseController(IOptions<CacheConfig> setting)
        {
            Config = setting.Value;
        }

    }
}
