using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PS.API.Extension;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PS.API.Filter
{
    /// <summary>
    /// 数据解密过滤器
    /// 前端只对post请求接口进行加密（先用非对称加密方式（RSA）加密对称加密的密钥，然后对称加密（AES）数据包）
    /// create by LiuCheng 2019.5.29
    /// </summary>
    //public class DataDecryptFilter : ActionFilterAttribute
    //{
    //    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    //    {
    //        var noEncrypt = ConfigHelper.GetValue("NoEncrypt");//白名单
    //        var method = context.HttpContext.Request.Method;
    //        if (method == "POST" && !noEncrypt.Contains(context.HttpContext.Request.Path))
    //        {
    //            string aeskey = context.HttpContext.Request.Headers["aeskey"];
    //            if (aeskey.Length > 36)//判断aeskey是否已经解密，若未解密则先进行解密操作
    //            {
    //                var rsaHelper = new RSAHelper(RSAType.RSA2, Encoding.UTF8);
    //                aeskey = rsaHelper.Decrypt(aeskey);
    //            }
    //            //解密数据包
    //            var data = context.HttpContext.Request.Form.FirstOrDefault().Value;
    //            var dataJson = AESHelper.AesDecrypt(data, aeskey);

    //            if (string.IsNullOrWhiteSpace(dataJson))
    //            {
    //                context.Result = AjaxHelper.JsonResult(HttpStatusCode.BadRequest, " 数据请求不合法！");
    //                return;
    //            }
    //            if (context.ActionArguments.Values.Count > 0)
    //            {
    //                //-----------model接收模式----------//
    //                var type = context.ActionArguments.Values.ToList()[0].GetType();

    //                PropertyInfo[] ps = type.GetProperties();

    //                var model = context.ActionArguments.Values.ToList()[0];

    //                var dy = JsonConvert.DeserializeObject(dataJson, type);

    //                var type2 = dy.GetType();
    //                PropertyInfo[] ps2 = type2.GetProperties();

    //                foreach (PropertyInfo i in ps)
    //                {
    //                    foreach (PropertyInfo i2 in ps2)
    //                    {
    //                        var value = i2.GetValue(dy, null);
    //                        if (i.Name == i2.Name && value != null)
    //                        {
    //                            i.SetValue(model, value, null);
    //                        }
    //                    }
    //                }
    //            }
    //            else
    //            {
    //                //-----------变量接收模式----------//
    //                var dy = (JObject)JsonConvert.DeserializeObject(dataJson);
    //                var parameterslist = context.ActionDescriptor.Parameters.ToList();
    //                foreach (var item in parameterslist)
    //                {
    //                    if (dy[item.Name] == null)
    //                        continue;
    //                    var vaule = ConvertObject(dy[item.Name].ToString(), item.ParameterType);
    //                    context.ActionArguments.Add(item.Name, vaule);
    //                }
    //            }
    //        }
    //        await base.OnActionExecutionAsync(context, next);
    //    }


        /// <summary>
        /// 将一个对象转换为指定类型
        /// </summary>
        /// <param name="obj">待转换的对象</param>
        /// <param name="type">目标类型</param>
        /// <returns>转换后的对象</returns>
        //private object ConvertObject(object obj, Type type)
        //{
        //    if (type == null) return obj;
        //    if (obj == null) return type.IsValueType ? Activator.CreateInstance(type) : null;

        //    Type underlyingType = Nullable.GetUnderlyingType(type);
        //    if (type.IsAssignableFrom(obj.GetType())) // 如果待转换对象的类型与目标类型兼容，则无需转换
        //    {
        //        return obj;
        //    }
        //    else if ((underlyingType ?? type).IsEnum) // 如果待转换的对象的基类型为枚举
        //    {
        //        if (underlyingType != null && string.IsNullOrEmpty(obj.ToString())) // 如果目标类型为可空枚举，并且待转换对象为null 则直接返回null值
        //        {
        //            return null;
        //        }
        //        else
        //        {
        //            return Enum.Parse(underlyingType ?? type, obj.ToString());
        //        }
        //    }
        //    else if (typeof(IConvertible).IsAssignableFrom(underlyingType ?? type)) // 如果目标类型的基类型实现了IConvertible，则直接转换
        //    {
        //        try
        //        {
        //            return Convert.ChangeType(obj, underlyingType ?? type, null);
        //        }
        //        catch
        //        {
        //            return underlyingType == null ? Activator.CreateInstance(type) : null;
        //        }
        //    }
        //    else
        //    {
        //        TypeConverter converter = TypeDescriptor.GetConverter(type);
        //        if (converter.CanConvertFrom(obj.GetType()))
        //        {
        //            return converter.ConvertFrom(obj);
        //        }
        //        ConstructorInfo constructor = type.GetConstructor(Type.EmptyTypes);
        //        if (constructor != null)
        //        {
        //            object o = constructor.Invoke(null);
        //            PropertyInfo[] propertys = type.GetProperties();
        //            Type oldType = obj.GetType();
        //            foreach (PropertyInfo property in propertys)
        //            {
        //                PropertyInfo p = oldType.GetProperty(property.Name);
        //                if (property.CanWrite && p != null && p.CanRead)
        //                {
        //                    property.SetValue(o, ConvertObject(p.GetValue(obj, null), property.PropertyType), null);
        //                }
        //            }
        //            return o;
        //        }
        //    }
        //    return obj;
        //}
   // }
}
