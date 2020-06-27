using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Runtime.Remoting.Messaging;
using Owl.Feature;
using Owl.Domain;
using Owl.Util;
namespace System
{
    /// <summary>
    /// 应用程序上下文
    /// </summary>
    public sealed class OwlContext : Dictionary<string, object>
    {
        public const string ContextKey = "owl.util.appcontext";
        public const string ContextHeaderlocalName = "ApplicationContext";
        public const string ContextHeaderNamespace = "http://www.Owl.com";
        /// <summary>
        /// 本次请求的应用程序上下文
        /// </summary>
        public static OwlContext Current
        {
            get
            {
                if (!string.IsNullOrEmpty(Cache.SessionId))
                {
                    return Cache.Session<OwlContext>(ContextKey, () => new OwlContext());
                }
                else
                    return Cache.Thread<OwlContext>(ContextKey, () => new OwlContext());
            }
        }

        public void CopyFrom(OwlContext context)
        {
            Clear();
            foreach (var key in context.Keys)
                this[key] = context[key];
        }

        public OwlContext()
        {
            Client.InitAppContext(this);

            if (string.IsNullOrEmpty(Language))
                Language = System.Globalization.CultureInfo.CurrentCulture.Name.Replace("-", "_");
        }

        /// <summary>
        /// 客户端
        /// </summary>
        public string Mandt
        {
            get
            {
                return this.ContainsKey("____Mandt") ? (string)this["____Mandt"] : "";
            }
            set
            {
                this["____Mandt"] = value;
            }
        }
        /// <summary>
        /// 登录用户名
        /// </summary>
        public string UserName
        {
            get
            {
                return this.ContainsKey("____UserName") ? (string)this["____UserName"] : "";
            }
            set
            {
                this["____UserName"] = value;
            }
        }
        /// <summary>
        /// 登录的IP地址
        /// </summary>
        public string IpAddr
        {
            get
            {
                return this.ContainsKey("__ipaddr") ? (string)this["__ipaddr"] : "";
            }
            set
            {
                this["__ipaddr"] = value;
            }
        }
        /// <summary>
        /// 浏览器
        /// </summary>
        public string Browser
        {
            get
            {
                return this.ContainsKey("__Browser") ? (string)this["__Browser"] : "";
            }
            set
            {
                this["__Browser"] = value;
            }
        }
        /// <summary>
        /// 当前的语言
        /// </summary>
        public string Language
        {
            get
            {
                return this.ContainsKey("__language") ? (string)this["__language"] : "";
            }
            set
            {
                this["__language"] = value;
            }
        }
        /// <summary>
        /// 主机名
        /// </summary>
        public string HostName
        {
            get
            {
                return this.ContainsKey("__HostName") ? (string)this["__HostName"] : "";
            }
            set { this["__HostName"] = value; }
        }
        /// <summary>
        /// 端口号
        /// </summary>
        public string Port
        {
            get
            {
                return this.ContainsKey("__HostPort") ? (string)this["__HostPort"] : "";
            }
            set { this["__HostPort"] = value; }
        }

        //string m_siteurl;
        /// <summary>
        /// 当前站点url
        /// </summary>
        public string SiteUrl
        {
            get
            {
                return this.ContainsKey("__SiteUrl") ? (string)this["__SiteUrl"] : "";
            }
            set { this["__SiteUrl"] = value; }
        }

    }

}
