using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Domain;

namespace Owl.Feature.iLog
{
    /// <summary>
    /// 消息记录
    /// </summary>
    public class MessageLog : SmartObject
    {
        /// <summary>
        /// 执行者
        /// </summary>
        public string Operator { get; private set; }

        /// <summary>
        /// 执行时间
        /// </summary>
        public DateTime Operated { get; private set; }

        /// <summary>
        /// 日志类型
        /// </summary>
        public MsgLogType Type { get; private set; }

        /// <summary>
        /// 日志代码
        /// </summary>
        public string LogNumber { get; private set; }

        /// <summary>
        /// 对象名称
        /// </summary>
        public string ModelName { get; set; }

        /// <summary>
        /// 消息名称
        /// </summary>
        public string MsgName { get; private set; }

        /// <summary>
        /// 执行此事件的IP地址
        /// </summary>
        public string IpAddr { get; private set; }

        /// <summary>
        /// 浏览器
        /// </summary>
        public string Browser { get; private set; }

        /// <summary>
        /// 消息体
        /// </summary>
        public string Body { get; private set; }

        /// <summary>
        /// 执行概要
        /// </summary>
        public string Summary { get; private set; }

        /// <summary>
        /// 其他信息
        /// </summary>
        public string Remark { get; private set; }

        public static MessageLog Create(Message msg, MsgLogType type, string summary, string remark)
        {
            var now = DateTime.Now;
            MessageLog log = new MessageLog()
            {
                LogNumber = string.Format("{0}{1}", now.ToString("yyyyMMddHHmmss"), Util.Serial.GetRandom(4, false).ToLower()),
                ModelName = msg.ModelName,
                MsgName = msg.Name,
                Type = type,
                Operator = OwlContext.Current.UserName,
                Operated = now,
                Browser = OwlContext.Current.Browser,
                IpAddr = OwlContext.Current.IpAddr,
                Body = msg.ToString(),
                Summary = summary,
                Remark = remark
            };
            return log;
        }
    }

    public abstract class MessageLogProvider : Provider
    {
        public abstract void Save(IEnumerable<MessageLog> entries);
    }

    internal class MessageLogEngine : Engine<MessageLogProvider, MessageLogEngine>
    {
        static MessageLogEngine()
        {
            TaskMgr.AddTask(Commit, 10, DateTime.Now.AddMinutes(1));
        }
        static readonly Queue<MessageLog> events = new Queue<MessageLog>(100);
        /// <summary>
        /// 添加事件日志
        /// </summary>
        /// <param name="name">事件</param>
        /// <param name="content">事件参数</param>
        /// <param name="result">事件执行结果</param>
        public static string Push(Message msg, MsgLogType type, string summary, string remark)
        {
            //if (type != MsgLogType.Exception)
            //    return "";
            var loglevel = Util.AppConfig.Section.MsgLogLevel ?? MsgLogType.Exception;
            if (loglevel > type)
                return "";
            var log = MessageLog.Create(msg, type, summary, remark);
            lock (events)
                events.Enqueue(log);
            return log.LogNumber;
        }

        static void Commit()
        {
            List<MessageLog> entries = new List<MessageLog>();
            lock (events)
            {
                while (events.Count > 0)
                    entries.Add(events.Dequeue());
            }
            if (entries.Count > 0)
                Execute(s => s.Save, entries);
        }
    }
}
