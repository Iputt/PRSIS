using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Feature;
using System.Runtime.Serialization;
using Owl;

namespace System
{
    /// <summary>
    /// Owl异常的基类
    /// </summary>
    public class Exception2 : Exception
    {
        public Exception2()
            : base()
        {

        }

        public Exception2(string message)
            : base(message)
        {

        }
        public Exception2(string format, params object[] args)
            : base(string.Format(format, args))
        {

        }

        public Exception2(string message, Exception innerException)
            : base(message, innerException)
        {

        }

        public Exception2(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }
    }

    /// <summary>
    /// 适用于提醒的异常
    /// </summary>
    public sealed class AlertException : Exception2
    {
        /// <summary>
        /// 资源名称
        /// </summary>
        public string ResKey { get; private set; }

        /// <summary>
        /// 信息格式
        /// </summary>
        public string Format { get; private set; }
        /// <summary>
        /// 信息类型
        /// </summary>
        public ResType? ResType { get; private set; }
        /// <summary>
        /// 是否需要刷新
        /// </summary>
        public bool Refresh { get; private set; }

        /// <summary>
        /// 参数
        /// </summary>
        public object[] Args { get; private set; }

        /// <summary>
        /// 确认信息
        /// </summary>
        public object Confirm { get; private set; }

        /// <summary>
        /// 多语言提醒的异常
        /// </summary>
        /// <param name="reskey">资源名称</param>
        /// <param name="format">提示信息</param>
        /// <param name="args">参数列表</param>
        public AlertException(string reskey, string format, params object[] args)
            : base(string.IsNullOrEmpty(reskey) ? format : Translation.Get(reskey, format, true), args)
        {
            ResKey = reskey;
            Format = format;
            Args = args;
        }
        /// <summary>
        /// 多语言提醒的异常
        /// </summary>
        /// <param name="msg">提示信息</param>
        /// <param name="resType">信息类型：info/error/warning</param>
        /// <param name="args">参数列表</param>
        public AlertException(string msg, ResType resType, params object[] args)
            : base(msg, resType, args)
        {
            ResType = resType;
            Args = args;
        }
        /// <summary>
        /// 多语言提醒的异常
        /// </summary>
        /// <param name="msg">提示信息</param>
        /// <param name="resType">信息类型：info/error/warning</param>
        /// <param name="refresh">确定后是否刷新</param>
        /// <param name="args">参数列表</param>
        public AlertException(string msg, ResType resType, bool refresh, params object[] args)
            : base(msg, resType, refresh, args)
        {
            ResType = resType;
            Refresh = refresh;
            Args = args;
        }
        /// <summary>
        /// 多语言提醒的异常
        /// </summary>
        /// <param name="msg">提示信息</param>
        /// <param name="refresh">确定后是否刷新</param> 
        /// <param name="args">参数列表</param>
        public AlertException(string msg, bool refresh, params object[] args)
            : base(msg, refresh, args)
        {
            Refresh = refresh;
            Args = args;
        }
        /// <summary>
        /// 多语言提醒的异常
        /// </summary>
        /// <param name="reskey">资源名称</param>
        /// <param name="format">提示信息</param>
        /// <param name="resType">信息类型：info/error/warning</param>
        /// <param name="args">参数列表</param>
        public AlertException(string reskey, string format, ResType resType, params object[] args)
            : base(string.IsNullOrEmpty(reskey) ? format : Translation.Get(reskey, format, true), args, resType)
        {
            ResKey = reskey;
            Format = format;
            Args = args;
            ResType = resType;
        }
        /// <summary>
        /// 多语言提醒的异常
        /// </summary>
        /// <param name="reskey">资源名称</param>
        /// <param name="format">提示信息</param>
        /// <param name="resType">信息类型：info/error/warning</param>
        /// <param name="refresh">确定后是否刷新</param>
        /// <param name="args">参数列表</param>
        public AlertException(string reskey, string format, ResType resType, bool refresh, params object[] args)
            : base(string.IsNullOrEmpty(reskey) ? format : Translation.Get(reskey, format, true), resType, refresh, args)
        {
            ResKey = reskey;
            Format = format;
            Args = args;
            ResType = resType;
            Refresh = refresh;
        }
        /// <summary>
        /// 多语言提醒的异常
        /// </summary>
        /// <param name="reskey">资源名称</param>
        /// <param name="format">提示信息</param>
        /// <param name="refresh">确定后是否刷新</param>
        /// <param name="args">参数列表</param>
        public AlertException(string reskey, string format, bool refresh, params object[] args)
            : base(string.IsNullOrEmpty(reskey) ? format : Translation.Get(reskey, format, true), refresh, args)
        {
            ResKey = reskey;
            Format = format;
            Args = args;
            Refresh = refresh;
        }
        /// <summary>
        /// 多语言提醒的异常
        /// </summary>
        /// <param name="format">提示信息</param>
        /// <param name="args"></param>
        public AlertException(string msg)
            : base(msg)
        {

        }
        /// <summary>
        /// 多语言提醒的异常
        /// </summary>
        /// <param name="message">提示信息</param>
        /// <param name="args"></param>
        public AlertException(string message, Exception inner)
            : base(message, inner)
        {

        }

        public AlertException(object confirm)
        {
            Confirm = confirm;
        }
    }
}
