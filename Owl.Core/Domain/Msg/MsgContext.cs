using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Feature;

namespace Owl.Domain
{
    /// <summary>
    /// 消息执行上下文
    /// </summary>
    public class MsgContext
    {
        static string ContextKey = "owl.domain.msgcontext";

        /// <summary>
        /// 当前消息执行上下文
        /// </summary>
        public static MsgContext Current
        {
            get
            {

                return Cache.Thread<MsgContext>(ContextKey, () => new MsgContext(null));
            }
            set
            {
                Cache.Thread(ContextKey, value);
            }
        }

        public MsgContext(MsgHandler handler)
        {
            Handler = handler;
        }
        /// <summary>
        /// 当前消息处理器
        /// </summary>
        public MsgHandler Handler { get; private set; }

        /// <summary>
        /// 消息体
        /// </summary>
        public Message Message { get { return Handler.Message; } }

        /// <summary>
        /// 确认步骤
        /// </summary>
        public string ConfirmStep { get { return Message.Body.GetRealValue<string>("confirmstep"); } }
        /// <summary>
        /// 消息描述
        /// </summary>
        public MsgDescrip Descrip { get { return Handler.Descrip; } }

        /// <summary>
        /// 作用的对象
        /// </summary>
        public IEnumerable<AggRoot> Roots { get { return Handler.Roots; } }

        /// <summary>
        /// 消息处理器的外部表单参数
        /// </summary>
        public FormObject FormObj { get { return Handler.FormObj; } }

        
        /// <summary>
        /// 添加整体日志记录
        /// </summary>
        /// <param name="resname">资源名称</param>
        /// <param name="format">缺省格式</param>
        /// <param name="args">参数</param>
        public void AppendTrackEntire(string resname, string format, params object[] args)
        {
            if (Handler != null)
                Handler.AppendTrackEntire(resname, format, args);
        }
        /// <summary>
        /// 添加对象日志记录
        /// </summary>
        /// <param name="root"></param>
        /// <param name="resname"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void AppendTrackRoot(AggRoot root, string resname, string format, params object[] args)
        {
            if (Handler != null)
                Handler.AppendTrackRoot(root, resname, format, args);
        }

        List<IMessageBehavior> m_behaviors;
        /// <summary>
        /// 当前上下文的临时消息执行后行为
        /// </summary>
        public List<IMessageBehavior> Behaviors
        {
            get
            {
                if (m_behaviors == null)
                    m_behaviors = new List<IMessageBehavior>();
                return m_behaviors;
            }
        }
    }
}
