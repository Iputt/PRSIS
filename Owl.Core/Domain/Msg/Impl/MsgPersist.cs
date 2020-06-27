using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Owl.Domain.Msg.Impl
{
    public abstract class MsgPersistProvider : Provider
    {
        public abstract void Persist(string key, string msg);

        public abstract string Load(string key);
    }
    /// <summary>
    /// 消息持久化
    /// </summary>
    public class MsgPersist : Engine<MsgPersistProvider, MsgPersist>
    {
        /// <summary>
        /// 持久化消息
        /// </summary>
        /// <param name="key">消息索引</param>
        /// <param name="msg">消息</param>
        /// <returns></returns>
        public static void Persist(string key, Message msg)
        {
            Execute(s => s.Persist, key, msg.ToString());
        }

        /// <summary>
        /// 加载消息
        /// </summary>
        /// <param name="key">消息索引</param>
        /// <returns></returns>
        public static Message Load(string key)
        {
            var msgstr = Execute2<string, string>(s => s.Load, key);
            var dto = msgstr.DeJson<TransferObject, TransferObject>();
            var name = dto["name"] as string;
            var context = dto["context"] as TransferObject;
            var body = dto["form"] as TransferObject;
            return Message.Create(Type.GetType(name), context, body);
        }
    }
}
