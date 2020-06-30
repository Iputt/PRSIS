using Microsoft.AspNetCore.Mvc;
using PS.API.Extension.Jwt;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PS.API.Controllers.Auth
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class AuthController : ControllerBase
    {
        private IJwt _jwt;
        public AuthController(IJwt jwt)
        {
            this._jwt = jwt;
        }
        /// <summary>
        /// getToken
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public IActionResult GetToken()
        {
            if (true)
            {
                Dictionary<string, string> clims = new Dictionary<string, string>();
                clims.Add("userName", "test");
                return new JsonResult(this._jwt.GetToken(clims));
            }
        }
    }
}
