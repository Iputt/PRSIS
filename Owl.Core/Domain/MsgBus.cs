using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Util;
using Owl.Domain.Msg.Impl;
using Owl.Feature;
namespace Owl.Domain
{
    public class MessageBus : Engine<MessageProvider, MessageBus>
    {
        protected override bool SkipException
        {
            get { return true; }
        }
        /// <summary>
        /// 获取消息处理器
        /// </summary>
        /// <param name="name">消息名称</param>
        /// <param name="model">对象名称</param>
        /// <param name="type">消息类型</param>
        /// <returns></returns>
        public static MsgHandler GetHandler(string name, string model)
        {
            return Execute2<string, string, MsgHandler>(s => s.GetHandler, name, model);
        }
        /// <summary>
        /// 获取消息描述
        /// </summary>
        /// <param name="name"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        public static MsgDescrip GetDescrip(string name, string model)
        {
            return Execute2<string, string, MsgDescrip>(s => s.GetDescrip, name, model);
        }

        /// <summary>
        /// 获取对象的消息列表
        /// </summary>
        /// <param name="model">对象名称</param>
        /// <returns></returns>
        public static Dictionary<string, MsgDescrip> GetDescrips(string model)
        {
            Dictionary<string, MsgDescrip> result = new Dictionary<string, MsgDescrip>();
            foreach (var msg in Execute3<string, MsgDescrip>(s => s.GetDescrips, model.ToLower()))
            {
                if (result.ContainsKey(msg.Name))
                    continue;
                result[msg.Name] = msg;
            }
            return result;
        }


        static Exception StripExp(Exception exp)
        {
            if (exp.InnerException != null)
                return StripExp(exp.InnerException);
            return exp;
        }
        public static Action<MsgContext> MessageSuccess = (context) =>
        {
            MsgContext.Current = context;
            //AppContext.Current = context.AppContext;
            var handler = context.Handler;
            if (handler.Roots == null || handler.Roots.Count() == 0)
                return;
            var behaviors = new List<IMessageBehavior>();
            behaviors.AddRange(context.Behaviors);
            if (handler.Descrip.Behaviors != null)
                behaviors.AddRange(handler.Descrip.Behaviors);
            if (behaviors.Count == 0)
                return;
            foreach (var behavior in behaviors)
            {
                behavior.OnSuccess(context.Handler.Roots);
            }
            DomainContext.Current.Commit();
        };
        /// <summary>
        /// 处理消息，如果消息处理器被封装的则执行封装
        /// </summary>
        /// <param name="message">消息</param>
        /// <returns></returns>
        public static Response HandleMessage(Message message)
        {
            string summary = "";
            Exception unhandleexp = null;
            object result = null;
            MsgDescrip descrip = null;
            var success = true;
            var refresh = false;
            ResType resType = ResType.success;
            List<string> checkinkeys = new List<string>();
            try
            {
                var handler = GetHandler(message.Name.ToLower(), (message.ModelName ?? "*").ToLower());
                descrip = handler.Descrip;
                if (descrip.Singleton)
                {
                    // 单例执行模式进行资源迁入加锁
                    var keys = message.GetKeys(true);
                    if (keys != null)
                    {
                        foreach (var key in keys)
                        {
                            var ckey = string.Format("{0}.{1}.{2}", message.ModelName, descrip.Name, key);
                            if (!CheckHelper.CheckIn(ckey, descrip.SingleTimeout ?? 60))
                                throw new AlertException(descrip.SingleNotifyResource, descrip.SingleNotify);
                            checkinkeys.Add(ckey);
                        }
                    }
                }
                MsgContext.Current = new MsgContext(handler);
                handler.Initial(message);
                if (descrip.Behaviors != null && handler.Roots != null)
                {
                    foreach (var behaivor in descrip.Behaviors)
                        behaivor.OnPrepared(handler.Roots);
                }

                result = handler.Execute();
                DomainContext.Current.Commit();
                summary = handler.EntireTrack;
                TaskMgr.StartTaskWithSession(MessageSuccess, MsgContext.Current);
            }
            catch (Exception ex)
            {
                ex = StripExp(ex);
                if (ex is AlertException && (ex as AlertException).Confirm != null)
                {
                    result = (ex as AlertException).Confirm;
                    success = true;
                }
                else
                {
                    resType = ResType.error;
                    if (ex is AlertException && (ex as AlertException).ResType.HasValue)
                    {
                        resType = (ex as AlertException).ResType.Value;
                        refresh = (ex as AlertException).Refresh;
                    }
                    success = false;
                    if (ex is System.Data.SqlClient.SqlException)
                    {
                        var sqlex = ex as System.Data.SqlClient.SqlException;
                        if (sqlex.Number == 2601)
                            ex = new AlertException("error.owl.domain.msgbus.exception.duplexindex", "写入数据重复，请检查");
                    }
                    summary = ex.Message;
                    if (!(ex is AlertException) && !AppConfig.Section.Debug)
                    {
                        unhandleexp = ex;
                    }
                }
            }
            finally
            {
                foreach (var ckey in checkinkeys) //释放迁入的资源
                    CheckHelper.CheckOut(ckey);
                if (unhandleexp != null)
                {
                    var lognumber = Log.Message(message, MsgLogType.Exception, summary, unhandleexp.StackTrace);
                    summary = string.Format(Translation.Get("error.owl.domain.msgbus.exception.nohandle", "对不起，出错了，请联系管理员错误代码是{0}", true), lognumber);
                }
                else
                {
                    Log.Message(message, descrip.Type == MsgType.Read ? MsgLogType.Read : MsgLogType.Write, summary, "");
                }
            }
            return new Response(resType, refresh, summary, result);
        }
    }
}
