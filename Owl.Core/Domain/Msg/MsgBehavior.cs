using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Owl.Domain
{


    /// <summary>
    /// 消息行为，订制消息执行过程
    /// </summary>
    public interface IMessageBehavior
    {
        /// <summary>
        /// 数据准备好之后
        /// </summary>
        /// <param name="roots">准备好的对象列表</param>
        void OnPrepared(IEnumerable<AggRoot> roots);

        /// <summary>
        /// 消息成功执行后
        /// </summary>
        /// <param name="roots">对象</param>
        void OnSuccess(IEnumerable<AggRoot> roots);
    }

    /// <summary>
    /// 消息行为基类
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public abstract class MessageBehaviorAttribute : Attribute, IMessageBehavior
    {
        protected Dictionary<string, HashSet<dynamic>> group(IEnumerable<AggRoot> roots, params string[] selectors)
        {
            var results = new Dictionary<string, HashSet<dynamic>>();
            foreach (var selector in selectors)
                results[selector] = new HashSet<dynamic>();
            foreach (var root in roots)
            {
                foreach (var selector in selectors)
                {
                    var value = root[selector];
                    if (!results[selector].Contains(value))
                        results[selector].Add(value);
                }
            }
            return results;
        }

        public virtual void OnPrepared(IEnumerable<AggRoot> roots) { }

        public virtual void OnSuccess(IEnumerable<AggRoot> roots) { }
    }

    /// <summary>
    /// 记录操作情况
    /// </summary>
    public class LogActionAttribute : MessageBehaviorAttribute
    {
        public override void OnSuccess(IEnumerable<AggRoot> roots)
        {
            var handler = MsgContext.Current.Handler;
            foreach (var root in roots)
            {
                Feature.Log.Action(root, handler.Descrip.Label,handler.GetLog(root.Id));
            }
        }
    }
}
