using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Domain;
using Owl.Util;

namespace Owl.Feature.iLog
{
    /// <summary>
    /// 对象操作记录
    /// </summary>
    public class ActionLog : SmartObject
    {
        /// <summary>
        /// 对象名称
        /// </summary>
        public string ModelName { get; private set; }

        /// <summary>
        /// 对象Id
        /// </summary>
        public Guid ObjectId { get; private set; }

        /// <summary>
        /// 操作者
        /// </summary>
        public string Operator { get; private set; }

        /// <summary>
        /// 操作时间
        /// </summary>
        public DateTime Operated { get; private set; }

        /// <summary>
        /// 操作
        /// </summary>
        public string Function { get; private set; }

        /// <summary>
        /// 结果信息
        /// </summary>
        public string Message { get; private set; }

        /// <summary>
        /// 保存操作记录
        /// </summary>
        /// <param name="root"></param>
        /// <param name="message"></param>
        /// <param name="result"></param>
        public static ActionLog Create(AggRoot root, string message, string result)
        {
            if (root == null)
                throw new ArgumentNullException("root");
            ActionLog audit = new ActionLog()
            {
                ModelName = root.Metadata.Name,
                ObjectId = root.Id,
                Operator = OwlContext.Current.UserName,
                Operated = DateTime.Now,
                Function = message,
                Message = result
            };
            return audit;
        }
    }

    public abstract class ActionLogProvider : Provider
    {
        public abstract IEnumerable<ActionLog> GetAudits(Guid rootid);

        public abstract void Save(IEnumerable<ActionLog> audits);
    }

    /// <summary>
    /// 操作记录引擎
    /// </summary>
    internal class ActionLogEngine : Engine<ActionLogProvider, ActionLogEngine>
    {
        protected override bool SkipException
        {
            get
            {
                return true;
            }
        }
        static ActionLogEngine()
        {
            TaskMgr.AddTask(Commit, 10, DateTime.Now.AddMinutes(1));
        }
        static Queue<ActionLog> audits = new Queue<ActionLog>(100);
        static void Commit()
        {
            List<ActionLog> entries = new List<ActionLog>();
            lock (audits)
            {
                while (audits.Count > 0)
                    entries.Add(audits.Dequeue());
            }
            if (entries.Count > 0)
            {
                Execute(s => s.Save, entries);
            }
        }

        /// <summary>
        /// 保存日志
        /// </summary>
        /// <param name="root"></param>
        /// <param name="message"></param>
        /// <param name="result"></param>
        public static void Push(AggRoot root, string message, string result)
        {
            var log = ActionLog.Create(root, message, result);
            lock (audits)
                audits.Enqueue(log);
        }
        public static IEnumerable<ActionLog> GetAudits(Guid rootid)
        {
            return Execute2<Guid, IEnumerable<ActionLog>>(s => s.GetAudits, rootid);
        }
    }
}