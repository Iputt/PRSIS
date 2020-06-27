using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Util;
using Owl.Feature.Impl.MQ;
namespace Owl.Feature
{
    /// <summary>
    /// 消息队列基础对象
    /// </summary>
    public abstract class MQObject : Owl.Domain.SmartObject
    {

    }
    public class P2SMessage<TBoby>
        where TBoby : class
    {
        /// <summary>
        /// 发布者
        /// </summary>
        public string Publisher { get; private set; }

        /// <summary>
        /// 消息体
        /// </summary>
        public TBoby Body { get; private set; }

        public P2SMessage(string publisher, TBoby body)
        {
            Publisher = publisher;
            Body = body;
        }
    }

    /// <summary>
    /// 消息队列
    /// </summary>
    public class MessageQueue : Engine<MQProvider, MessageQueue>
    {
        protected override EngineMode Mode
        {
            get
            {
                return EngineMode.Single;
            }
        }
        protected override bool SkipException
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// 应用程序名称
        /// </summary>
        public static string Name { get { return Application.Name; } }


        /// <summary>
        /// 订阅消息
        /// </summary>
        /// <param name="topic">主题</param>
        /// <param name="callback">回调函数</param>
        /// <param name="ignoreself">忽略自己的</param>
        public static void Subscrib<MBody>(string topic, Action<P2SMessage<MBody>> callback, bool ignoreself = true)
            where MBody : class
        {
            Action<string> action = s =>
            {
                var msg = s.DeJson<P2SMessage<MBody>>();
                if (msg.Publisher == Name && ignoreself)
                    return;
                callback(msg);
            };
            Execute(s => s.Subscrib, topic, action);
        }

        /// <summary>
        /// 发布一条消息
        /// </summary>
        /// <param name="topic">主题</param>
        /// <param name="body">消息体，可序列化</param>
        public static void Publish<MBody>(string topic, MBody body)
            where MBody : class
        {
            if (Application.AllApplications.Count() > 1)
                Execute(s => s.Publish, topic, new P2SMessage<MBody>(Name, body).ToJson());
        }
    }
}
