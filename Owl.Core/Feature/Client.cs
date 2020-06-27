using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using Owl.Util;

namespace Owl.Feature
{
    public abstract class Client : SortedLoader<Client>
    {


        /// <summary>
        /// 当前客户端的sessionid
        /// </summary>
        protected abstract string _SessionId { get; }

        /// <summary>
        /// 重置连接
        /// </summary>
        protected abstract void _ResetSession();

        /// <summary>
        /// 初始化应用程序上下文的信息
        /// </summary>
        /// <param name="context"></param>
        protected abstract void _InitAppContext(OwlContext context);

        /// <summary>
        ///  客户端的登录标识
        /// </summary>
        protected abstract IIdentity _Identity { get; }

        /// <summary>
        /// 是否为移动端设备
        /// </summary>
        protected abstract bool _IsMobile { get; }

        public static readonly string SessionIdName = "owl_pl_web_sessionid";
        /// <summary>
        /// 当前客户端
        /// </summary>
        static Client Current { get { return LoadedObjs.FirstOrDefault(); } }
        /// <summary>
        /// 当前客户端的sessionid
        /// </summary>
        public static string SessionId
        {
            get
            {
                var client = Current;
                return client == null ? null : client._SessionId;
            }
        }
        /// <summary>
        /// 重置连接
        /// </summary>
        public static void ResetSession()
        {
            var client = Current;
            if (client != null)
                client._ResetSession();
        }

        /// <summary>
        /// 初始化应用程序上下文的信息
        /// </summary>
        /// <param name="context"></param>
        public static void InitAppContext(OwlContext context)
        {
            var client = Current;
            if (client != null)
                client._InitAppContext(context);
        }

        /// <summary>
        /// 当前客户端的登录标识
        /// </summary>
        public static IIdentity Identity
        {
            get
            {
                var client = Current;
                return client != null ? client._Identity : null;
            }
        }
        /// <summary>
        /// 是否为移动端浏览器
        /// </summary>
        public static bool IsMobile
        {
            get
            {
                var client = Current;
                return client != null ? client._IsMobile : false;
            }
        }
    }
}
