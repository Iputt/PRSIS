using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Runtime.Remoting.Proxies;
using System.Runtime.Remoting.Messaging;
using Owl.Domain;
using System.Collections;
using Owl.Util.Impl.AppConfig;
using System.Configuration;
using Owl.Util;
using System.Web;
namespace Owl.Domain.Driver.Repository
{
    public class RestConfigElement : CustomeConfigElement
    {
        /// <summary>
        /// 基础路径
        /// </summary>
        [ConfigurationProperty("url")]
        public string Url
        {
            get { return this["url"] as string; }
            set { this["url"] = value; }
        }


        /// <summary>
        /// 登录名
        /// </summary>
        [ConfigurationProperty("mandt")]
        public string Mandt
        {
            get { return this["mandt"] as string; }
            set { this["mandt"] = value; }
        }

        /// <summary>
        /// 登录名
        /// </summary>
        [ConfigurationProperty("username")]
        public string Login
        {
            get { return this["username"] as string; }
            set { this["username"] = value; }
        }
        /// <summary>
        /// 密码
        /// </summary>
        [ConfigurationProperty("password")]
        public string Password
        {
            get { return this["password"] as string; }
            set { this["password"] = value; }
        }
    }
}
namespace Owl.Domain
{

    public interface IRestfulApi
    {
        /// <summary>
        /// 获取令牌
        /// </summary>
        /// <param name="username">用户名</param>
        /// <param name="password">密码</param>
        /// <returns></returns>
        string Token(string mandt, string username, string password, bool persistent);

        /// <summary>
        /// 保存变更
        /// </summary>
        /// <param name="roots">添加/修改</param>
        /// <param name="keys">删除</param>
        TransferObject Save(IEnumerable<AggRoot> adds, IEnumerable<AggRoot> updates, IEnumerable<AggRoot> removes);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="modelname"></param>
        /// <param name="specification"></param>
        /// <returns></returns>
        bool Exists(string modelname, string specification);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="modelname"></param>
        /// <param name="specification"></param>
        /// <returns></returns>
        int Count(string modelname, string specification);

        IDictionary<string, object> Sum(string modelname, string specification, params string[] selector);

        IEnumerable<TransferObject> Read(string modelname, Guid[] id, params string[] selector);

        IEnumerable<TransferObject> GetList(string modelname, string specification, SortBy sortby, int start = 0, int size = 0, params string[] selector);

        AggRoot FindById(string modelname, Guid Id);

        AggRoot FindFirst(string modelname, string specification);

        IEnumerable<AggRoot> FindAll(string modelname, string specification, SortBy sortby, int start = 0, int count = 0, params string[] selector);
    }
    /// <summary>
    /// restful api 代理
    /// </summary>
    public class RestProxy : RealProxy
    {
        /// <summary>
        /// 路径
        /// </summary>
        protected string BaseUrl { get; private set; }

        /// <summary>
        /// 令牌
        /// </summary>
        protected string Token { get; private set; }

        public RestProxy(string baseurl)
            : base(typeof(IRestfulApi))
        {
            BaseUrl = baseurl;
        }

        public override IMessage Invoke(IMessage msg)
        {
            IMethodCallMessage methodcall = (IMethodCallMessage)msg;
            if (string.IsNullOrEmpty(Token) && methodcall.MethodName != "Token")
                throw new AlertException("error.owl.domain.restfulapi.notoken", "你尚未获得登陆令牌，请获取令牌后再执行此操作");

            Dictionary<string, object> reqdatas = new Dictionary<string, object>(methodcall.ArgCount);
            var parameters = methodcall.MethodBase.GetParameters();
            for (int i = 0; i < methodcall.ArgCount; i++)
            {
                var parameter = parameters[i];
                var name = methodcall.GetArgName(i).ToLower();
                var value = methodcall.GetArg(i);
                //if (value != null && !(parameter.ParameterType.IsValueType || parameter.ParameterType.Name == "String"))
                //{
                //    value = value.ToJson();
                //}
                reqdatas[name] = value;
            }
            var dstr = string.Format("req=remote&data={0}", HttpUtility.UrlEncode(Encoding.UTF8.GetBytes(reqdatas.ToJson())));

            //var dstr = string.Join("&", reqdatas.Select(s => string.Format("{0}={1}", s.Key, s.Value)));
            var data = Encoding.UTF8.GetBytes(dstr);
            var req = (HttpWebRequest)HttpWebRequest.Create(string.Format("{0}{1}", BaseUrl, methodcall.MethodName.ToLower()));
            req.Method = "POST";
            req.ContentType = "application/x-www-form-urlencoded";
            req.ContentLength = data.Length;
            if (methodcall.MethodName != "Token")
                req.Headers["token"] = Token;
            using (var stream = req.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }
            var returntype = (methodcall.MethodBase as System.Reflection.MethodInfo).ReturnType;
            Response res;
            using (var response = req.GetResponse())
            {
                using (var reader = new System.IO.StreamReader(response.GetResponseStream()))
                {
                    res = reader.ReadToEnd().DeJson<Response>(new JsonFieldType("data", returntype));
                }
            }
            if (res.type == "error")
                return new ReturnMessage(new Exception(res.message), methodcall);

            var result = res.data;
            if (methodcall.MethodName == "Token")
                Token = result.ToString();
            return new ReturnMessage(result, null, methodcall.ArgCount - methodcall.InArgCount, methodcall.LogicalCallContext, methodcall);
        }

        static IRestfulApi Create(string baseurl)
        {
            return (IRestfulApi)new RestProxy(baseurl).GetTransparentProxy();
        }

        static IRestfulApi m_instance;
        static object locker = new object();

        public static IRestfulApi Instance
        {
            get
            {
                if (m_instance == null)
                {
                    lock (locker)
                    {
                        if (m_instance == null)
                        {
                            var Config = AppConfig.Section.GetConfig<Owl.Domain.Driver.Repository.RestConfigElement>();
                            m_instance = Create(Config.Url);
                            m_instance.Token(Config.Mandt, Config.Login, Config.Password, false);
                        }
                    }
                }
                return m_instance;
            }
        }
        static Dictionary<string, IRestfulApi> restapis = new Dictionary<string, IRestfulApi>();
        /// <summary>
        /// 创建Api
        /// </summary>
        /// <param name="url"></param>
        /// <param name="mandt"></param>
        /// <param name="login"></param>
        /// <param name="password"></param>
        /// <param name="persistent"></param>
        /// <returns></returns>
        public static IRestfulApi Create(string url, string mandt, string login, string password, bool persistent)
        {
            var key = string.Format("{0}{1}{2}{3}{4}", url, mandt, login, password, persistent);
            if (!restapis.ContainsKey(key))
            {
                lock (restapis)
                {
                    if (!restapis.ContainsKey(key))
                    {
                        var instance = Create(url);
                        instance.Token(mandt, login, password, persistent);
                        restapis[key] = instance;
                    }
                }
            }
            return restapis[key];
        }
    }
}
