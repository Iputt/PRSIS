using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PS.API.Interface;
using PS.External.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PS.API.Controllers.Auth
{
    /// <summary>
    /// jwt权限验证
    /// </summary>
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class AuthenticationController : ControllerBase
    {
        private readonly IAuthenticateService _authService;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="authService"></param>
        public AuthenticationController(IAuthenticateService authService)
        {
            this._authService = authService;
        }

        /// <summary>
        /// 获取token
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpPost]
        public IActionResult GetToken([FromBody]PLoginDto dto)
        {
            if(!ModelState.IsValid)
            {
                return BadRequest("Invalid Request");
            }

            string token;
            if(_authService.IsAuthenticated(dto, out token))
            {
                return Ok(token);
            }

            return BadRequest("Invalid Request");
        }
    }
}
