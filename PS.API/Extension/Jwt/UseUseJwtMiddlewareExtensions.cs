using Microsoft.AspNetCore.Builder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PS.API.Extension.Jwt
{
    /// <summary>
    /// 中间件 - UseJwt
    /// </summary>
    public static class UseUseJwtMiddlewareExtensions
    {
        /// <summary>
        /// 权限检查
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseJwt(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<UseJwtMiddleware>();
        }
    }
}
