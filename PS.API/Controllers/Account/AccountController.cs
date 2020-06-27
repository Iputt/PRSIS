using AutoMapper;
using Microsoft.AspNetCore.Authorization;
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
    [Route("v1/[controller]/[action]")]
    [Authorize]
    public class AccountController: ControllerBase
    {
        private readonly ILogin _login;
        private readonly IMapper _mapper;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="login"></param>
        /// <param name="mapper"></param>
        /// <param name="logger"></param>
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
        //[ApiExplorerSettings(IgnoreApi = true)]
        public async Task<ActionResult<IEnumerable<RLoginDto>>> GetUserInfo()
        {
            var infoList = await _login.GetLoginsAsync();
            var dto= _mapper.Map<IEnumerable<RLoginDto>>(infoList);
            return Ok(dto);
        }
    }
}
