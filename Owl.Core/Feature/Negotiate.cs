using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Util;

namespace Owl.Feature
{
    internal enum NegotiateType
    {
        /// <summary>
        /// 主服务器通知
        /// </summary>
        Notify,

        /// <summary>
        /// 启动协商
        /// </summary>
        Start,
        /// <summary>
        /// 请求为主服务器
        /// </summary>
        Request
    }

    /// <summary>
    /// 协商内容
    /// </summary>
    internal class NegotiateContent
    {
        /// <summary>
        /// 协商类型
        /// </summary>
        public NegotiateType Type { get; set; }

        /// <summary>
        /// 服务器启动时间
        /// </summary>
        public DateTime Start { get; set; }

        /// <summary>
        /// 协商时间
        /// </summary>
        public DateTime Time { get; set; }
    }
    /// <summary>
    /// 分布式中的主从协商
    /// </summary>
    public class Negotiate
    {
        static readonly string Chanel = "owl_feature_negotiate";

        /// <summary>
        /// 最后一次服务器通知时间
        /// </summary>
        static DateTime LastTime;

        /// <summary>
        /// 服务启动时间
        /// </summary>
        static DateTime StartTime;

        /// <summary>
        /// 是否是主要服务器
        /// </summary>
        public static bool IsPrimary { get; private set; }
        /// <summary>
        /// 主服务器回调
        /// </summary>
        static List<Action> Callbacks = new List<Action>();
        static Negotiate()
        {
            Subscriber.Subscrib<NegotiateContent>(Chanel, (server, content) =>
            {
                Handle(content);
            });
            LastTime = DateTime.Now;
            StartTime = LastTime;
            TaskMgr.AddTask(Check, 10);
        }
        /// <summary>
        /// 正在协商中
        /// </summary>
        static bool Negotiating;
        static bool PublishRequested;
        static DateTime NegotiatingTime;
        static void Reset()
        {
            IsPrimary = true;
            Negotiating = true;
            NegotiatingTime = DateTime.Now;
            PublishRequested = false;
        }
        static void Handle(NegotiateContent content)
        {
            if (content.Type == NegotiateType.Notify)
            {
                if (LastTime < content.Time)
                    LastTime = content.Time;
                if (Negotiating)
                    Negotiating = false;
            }
            else if (content.Type == NegotiateType.Start || content.Type == NegotiateType.Request)
            {
                if (content.Type == NegotiateType.Start && !Negotiating)
                {
                    Reset();
                }
                if (IsPrimary && content.Start > StartTime)
                {
                    IsPrimary = false;
                }
                if (IsPrimary)
                    PublishRequest();
            }
            #region
            //switch (content.Type)
            //{
            //    case NegotiateType.Notify:
            //        if (LastTime < content.Time)
            //            LastTime = content.Time;
            //        if (Negotiating)
            //            Negotiating = false;
            //        break;
            //    case NegotiateType.Start:
            //        if (!Negotiating)
            //        {
            //            IsPrimary = true;
            //            Negotiating = true;
            //            NegotiatingTime = content.Time;
            //            PublishRequested = false;
            //            if (content.Start > StartTime)
            //            {
            //                IsPrimary = false;
            //            }
            //        }
            //        break;

            //    case NegotiateType.Request:
            //        if (IsPrimary)
            //        {
            //            if (content.Start > StartTime)
            //            {
            //                IsPrimary = false;
            //            }
            //            else
            //            {
            //                PublishRequest();
            //            }
            //        }
            //        break;
            //}
            #endregion
        }
        static void Check()
        {
            if (!IsPrimary && !Negotiating && LastTime.AddSeconds(30) < DateTime.Now)
            {
                StartNegotiate();
            }
            if (IsPrimary && Negotiating && NegotiatingTime.AddSeconds(20) < DateTime.Now)
            {
                Negotiating = false;
                foreach (var callback in Callbacks)
                {
                    TaskMgr.StartTask(callback);
                }
            }
            if (IsPrimary && !Negotiating)
            {
                Subscriber.Publish(Chanel, new NegotiateContent() { Type = NegotiateType.Notify, Start = StartTime, Time = DateTime.Now });
            }
        }
        static void PublishRequest()
        {
            if (!PublishRequested)
            {
                Subscriber.Publish(Chanel, new NegotiateContent()
                {
                    Type = NegotiateType.Request,
                    Start = StartTime
                }.ToJson());
                PublishRequested = true;
            }
        }
        static void StartNegotiate()
        {
            Reset();
            Subscriber.Publish(Chanel, new NegotiateContent()
            {
                Type = NegotiateType.Start,
                Start = StartTime,
                Time = NegotiatingTime
            });
        }


        /// <summary>
        /// 向协商程序中添加回调
        /// </summary>
        public static void PushCallback(Action callback)
        {
            Callbacks.Add(callback);
            if (IsPrimary && !Negotiating)
            {
                TaskMgr.StartTask(callback);
            }
        }
    }
}
