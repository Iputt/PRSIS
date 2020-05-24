using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PS.External.Model;
using PS.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PS.API.Controllers.Account
{
    /// <summary>
    /// 账号相关接口
    /// </summary>
    [ApiController]
    [Route("v1/[controller]")]
    public class AccountController: ControllerBase
    {
        private readonly ILogin _login;
        private readonly IMapper _mapper;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="mapper"></param>
        public AccountController(ILogin login,IMapper mapper, ILogger<AccountController> logger)
        {
            _login = login ?? throw new ArgumentException(nameof(login));
            _mapper = mapper ?? throw new ArgumentException(nameof(mapper));
        }

        /// <summary>
        /// 方法实现 - 添加
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> AddLogin([FromBody]PLoginDto dto)
        {
            //登录信息映射
            var login = _mapper.Map<PLoginDto, Login>(dto);
            //获取结果
            var result = await _login.Add(login);
            return Ok(result);
        }

        /// <summary>
        /// 获取用户信息
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<List<Login>> GetUserInfo()
        {
            return await _login.GetLoginsAsync();
        }
    }
}
