using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Domain;
using Owl.Util;
namespace Owl.Domain.Msg.Impl
{
    /// <summary>
    /// 消息处理器提供者
    /// </summary>
    public abstract class MessageProvider : Provider
    {
        /// <summary>
        /// 获取消息处理器
        /// </summary>
        /// <param name="name">消息名称</param>
        /// <param name="modelname">对象名称</param>
        /// <returns></returns>
        public abstract MsgHandler GetHandler(string name, string modelname);


        /// <summary>
        /// 获取消息的描述
        /// </summary>
        /// <param name="name"></param>
        /// <param name="modelname"></param>
        /// <returns></returns>
        public abstract MsgDescrip GetDescrip(string name, string modelname);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="modelname"></param>
        /// <returns></returns>
        public virtual IEnumerable<MsgDescrip> GetDescrips(string modelname) { return null; }

        
    }

}
