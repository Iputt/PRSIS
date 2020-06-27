using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Domain;
using Owl.Feature.iLog;

namespace Owl.Feature
{
    /// <summary>
    /// 消息日志类型
    /// </summary>
    public enum MsgLogType
    {
        /// <summary>
        /// 读取
        /// </summary>
        [DomainLabel("读取")]
        Read = 0x01,
        /// <summary>
        /// 写入
        /// </summary>
        [DomainLabel("写入")]
        Write = 0x02,
        /// <summary>
        /// 异常
        /// </summary>
        [DomainLabel("异常")]
        Exception = 0x04
    }

    /// <summary>
    /// 日志管理
    /// </summary>
    public static class Log
    {
        /// <summary>
        /// 操作日志
        /// </summary>
        public static void Action(AggRoot root, string action, string result)
        {
            ActionLogEngine.Push(root, action, result);
        }

        /// <summary>
        /// 记录消息执行日志
        /// </summary>
        /// <param name="msg">消息</param>
        /// <param name="type">消息日志类型</param>
        /// <param name="summary">执行概要</param>
        /// <param name="remark">其他信息</param>
        /// <returns></returns>
        public static string Message(Message msg, MsgLogType type, string summary, string remark)
        {
            return MessageLogEngine.Push(msg, type, summary, remark);
        }

        static string BuildLog(DateTime time, string level, string tag, string content)
        {
            return string.Format("[{0}] {1}:{2} {3}");
        }

        /// <summary>
        /// 普通信息日志
        /// </summary>
        public static void Info(DateTime time, string tag, string content)
        {

        }
        /// <summary>
        /// 警告信息日志
        /// </summary>
        public static void Warning(DateTime time, string tag, string content)
        {

        }
        /// <summary>
        /// 错误信息日志
        /// </summary>
        /// <param name="time"></param>
        /// <param name="tag"></param>
        /// <param name="content"></param>
        public static void Error(DateTime time, string tag, string content)
        {

        }
    }
}
