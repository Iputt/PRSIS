using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Remoting.Proxies;
using System.Runtime.Remoting.Messaging;
using System.Net;
using System.Collections;
using System.Web;

namespace Owl.Util
{
    public class UrlEncode
    {
        static string Encode(string key, object value)
        {
            if (value == null)
                return "";
            if (value is Object2)
            {
                List<string> tr = new List<string>();
                var tmp = value as Object2;
                foreach (var tk in tmp.Keys)
                {
                    var ts = Encode(string.Format("{0}[{1}]", key, tk), tmp[tk]);
                    if (!string.IsNullOrEmpty(ts))
                        tr.Add(ts);
                }
                return string.Join("&", tr);
            }
            else if (TypeHelper.IsArray(value))
            {
                var count = 0;
                List<string> tr = new List<string>();
                foreach (var tmp in value as IEnumerable)
                {
                    var ts = Encode(string.Format("{0}[{1}]", key, count), tmp);
                    if (!string.IsNullOrEmpty(ts))
                        tr.Add(ts);
                    count += 1;
                }
                return string.Join("&", tr);
            }
            else
                return string.Format("{0}={1}", key, HttpUtility.UrlEncode(value.ToString()));
        }
        public static string Encode(TransferObject param)
        {
            List<string> builder = new List<string>();
            foreach (var pair in param)
            {
                var tmp = Encode(pair.Key, pair.Value);
                if (!string.IsNullOrEmpty(tmp))
                    builder.Add(tmp);
            }
            return string.Join("&", builder);
        }
    }

    /// <summary>
    /// URL 代理
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class UrlBindAttribute : Attribute
    {
        /// <summary>
        /// 本方法绑定的路径
        /// </summary>
        /// <value>The path.</value>
        public string Path { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path">绑定的路径</param>
        public UrlBindAttribute(string path)
        {
            Path = path;
        }
    }

    /// <summary>
    /// 返回值
    /// </summary>
    public class ReturnArg
    {

        public string Method { get; internal set; }

        public Type ReturnType { get; internal set; }

        public string ReturnText { get; internal set; }

        public Exception Exception { get;  set; }

        /// <summary>
        /// 异常是否处理完成
        /// </summary>
        public bool IsExpHandled { get; set; }
    }

    /// <summary>
    /// http透明代理
    /// </summary>
    /// <typeparam name="IHttpMethod"></typeparam>
    public class HttpRealProxy : RealProxy
    {
        /// <summary>
        /// 网络路径格式
        /// </summary>
        protected string UrlFormat { get; private set; }

        /// <summary>
        /// 传输方式 GET POST
        /// </summary>
        protected string Method { get; private set; }

        protected Func<string, TransferObject, string> ParamFormater { get; private set; }

        protected Func<ReturnArg, object> ResultFormater { get; private set; }

        public HttpRealProxy(Type httpproxy, string urlformat, Func<string, TransferObject, string> paramformater = null, Func<ReturnArg, object> resultformatr = null)
            : this(httpproxy, urlformat, "POST", paramformater, resultformatr)
        {

        }

        public HttpRealProxy(Type httpproxy, string urlformat, string mehtod = "POST", Func<string, TransferObject, string> paramformater = null, Func<ReturnArg, object> resultformatr = null)
            : base(httpproxy)
        {
            UrlFormat = urlformat;
            Method = mehtod.ToUpper();
            ParamFormater = paramformater;
            if (ParamFormater == null)
                ParamFormater = (s, t) => UrlEncode.Encode(t);
            ResultFormater = resultformatr;
            if (ResultFormater == null)
            {
                ResultFormater = arg => arg.ReturnText;
            }
        }

        Dictionary<string, string> m_urlbind = new Dictionary<string, string>();
        public override IMessage Invoke(IMessage msg)
        {
            IMethodCallMessage methodcall = (IMethodCallMessage)msg;
            var methodname = methodcall.MethodName;
            if (!m_urlbind.ContainsKey(methodname))
            {
                var bind = methodcall.MethodBase.GetCustomAttributes(typeof(UrlBindAttribute), false).Cast<UrlBindAttribute>().FirstOrDefault();
                if (bind == null)
                    m_urlbind[methodname] = methodname;
                else
                    m_urlbind[methodname] = bind.Path;
            }
            TransferObject reqdatas = new TransferObject(methodcall.ArgCount);
            for (int i = 0; i < methodcall.ArgCount; i++)
            {
                var name = methodcall.GetArgName(i);
                var value = methodcall.GetArg(i);
                if (value != null)
                    reqdatas[name] = value;
            }
            var dstr = ParamFormater(methodname, reqdatas);
            string url = string.Format(UrlFormat, m_urlbind[methodname]);
            if (Method == "GET")
            {
                url = string.Format("{0}?{1}", url, dstr);
            }
            var req = (HttpWebRequest)HttpWebRequest.Create(url);
            req.Method = Method;
            req.Accept = "application/json;charset=utf-8;";
            req.ContentType = "application/x-www-form-urlencoded;charset=UTF-8";
            //req.ContentType = "text/html;charset=UTF-8";
            if (Method == "POST")
            {
                var data = Encoding.UTF8.GetBytes(dstr);
                req.ContentLength = data.Length;
                using (var stream = req.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }
            }
            var returnarg = new ReturnArg() { Method = methodname };
            returnarg.ReturnType = (methodcall.MethodBase as System.Reflection.MethodInfo).ReturnType;
            try
            {
                using (var response = req.GetResponse())
                {
                    using (var stream = response.GetResponseStream())
                    {
                        //stream.Position = 0;
                        using (var reader = new System.IO.StreamReader(stream))
                        {
                            returnarg.ReturnText = reader.ReadToEnd();
                        }
                    }
                }
            }
            catch (WebException e)
            {
                returnarg.Exception = e;
            }
            var result = ResultFormater(returnarg);
            if (returnarg.Exception != null && !returnarg.IsExpHandled)
                return new ReturnMessage(returnarg.Exception, methodcall);
            if (returnarg.ReturnType == typeof(void))
                result = null;
            return new ReturnMessage(result, null, methodcall.ArgCount - methodcall.InArgCount, methodcall.LogicalCallContext, methodcall);
        }
        static HttpRealProxy()
        {
            if (ServicePointManager.ServerCertificateValidationCallback == null)
                ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback((sender, cert, chain, error) => true);
        }
        /// <summary>
        /// 创建代理
        /// </summary>
        /// <typeparam name="IHttpProxy">http接口</typeparam>
        /// <param name="urlformat">url路径格式</param>
        /// <param name="paramformater">对象型参数格式化器 参数1为 方法名称，参数2为参数</param>
        /// <param name="resultformater">结果格式器，参数1为方法名，参数2为web返回字符串</param>
        /// <returns></returns>
        public static IHttpProxy Create<IHttpProxy>(string urlformat, Func<string, TransferObject, string> paramformater = null, Func<ReturnArg, object> resultformater = null)
        {
            return (IHttpProxy)new HttpRealProxy(typeof(IHttpProxy), urlformat, paramformater, resultformater).GetTransparentProxy();
        }
    }

    /// <summary>
    /// http 代理工厂
    /// </summary>
    /// <typeparam name="IHttpProxy"></typeparam>
    public abstract class HttpProxyFactory<IHttpProxy>
    {
        protected string UrlFormat { get; private set; }
        protected string Method { get; private set; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="urlformat"></param>
        /// <param name="method">调用方法 POST GET</param>
        public HttpProxyFactory(string urlformat, string method = "POST")
        {
            UrlFormat = urlformat;
            Method = method;
        }

        protected virtual string ParamFormat(string method, TransferObject param)
        {
            return UrlEncode.Encode(param);
        }
        protected virtual object ResultFormat(ReturnArg arg)
        {
            return arg.ReturnText;
        }

        IHttpProxy m_proxy;
        public IHttpProxy Proxy
        {
            get
            {
                if (m_proxy == null)
                {
                    m_proxy = (IHttpProxy)new HttpRealProxy(typeof(IHttpProxy), UrlFormat, Method, ParamFormat, ResultFormat).GetTransparentProxy();
                }
                return m_proxy;
            }
        }
    }
}
