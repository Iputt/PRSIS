 using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.IO;
using Microsoft.AspNetCore.Authorization;

namespace PS.API.Controllers
{
    /// <summary>
    /// 通用接口
    /// </summary>
    [ApiVersion("1")]
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]/[action]")]
    [Authorize]
    public class CommonController : ControllerBase
    {
        private readonly ILogger<CommonController> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logger"></param>
        public CommonController(ILogger<CommonController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Ps API 测试接口
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> Demo()
        {
            return Ok();
        }

        /// <summary>
        /// 获取图片附件
        /// </summary>
        /// <param name="idsStr"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GetAttachment([FromForm]FileStream file)
        {
            
            return Ok();
        }
    }
}