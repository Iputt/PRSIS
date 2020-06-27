using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PS.API.Extension.Jwt
{
    public class UseJwtMiddleware
    {
        private readonly RequestDelegate _next;
        private JwtConfig _jwtConfig = new JwtConfig();
        private IJwt _jwt;
        public UseJwtMiddleware(RequestDelegate next, IConfiguration configration, IJwt jwt)
        {
            _next = next;
            this._jwt = jwt;
            configration.GetSection("JwtConfig").Bind(_jwtConfig);
        }
        public Task InvokeAsync(HttpContext context)
        {
            if (_jwtConfig.IgnoreUrls.Contains(context.Request.Path))
            {
                return this._next(context);
            }
            else
            {
                if (context.Request.Headers.TryGetValue(this._jwtConfig.HeadField, out Microsoft.Extensions.Primitives.StringValues authValue))
                {
                    var authstr = authValue.ToString();
                    if (this._jwtConfig.Prefix.Length > 0)
                    {
                        authstr = authValue.ToString().Substring(this._jwtConfig.Prefix.Length + 1, authValue.ToString().Length - (this._jwtConfig.Prefix.Length + 1));
                    }
                    if (this._jwt.ValidateToken(authstr, out Dictionary<string, string> Clims))
                    {
                        foreach (var item in Clims)
                        {
                            context.Items.Add(item.Key, item.Value);
                        }
                        return this._next(context);
                    }
                    else
                    {
                        context.Response.StatusCode = 401;
                        context.Response.ContentType = "application/json";
                        return context.Response.WriteAsync("{\"status\":401,\"statusMsg\":\"auth vaild fail\"}");
                    }
                }
                else
                {
                    context.Response.StatusCode = 401;
                    context.Response.ContentType = "application/json";
                    return context.Response.WriteAsync("{\"status\":401,\"statusMsg\":\"auth vaild fail\"}");
                }
            }
        }
    }
}
