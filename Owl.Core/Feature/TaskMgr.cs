using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Owl.Util;
using System.Collections;
using Owl.Feature.Tasks;
namespace Owl.Feature.Tasks
{
    #region 任务对象
    /// <summary>
    /// 任务对象
    /// </summary>

    #region 无返回值
    internal class TaskObjectNoArg : TaskObject
    {
        protected Action Action { get; private set; }

        public TaskObjectNoArg(Action action, bool withsession)
            : base(withsession)
        {
            Action = action;
        }
        protected override void _InnerExecute()
        {
            Action();
        }
    }

    internal class TaskObject<TArg> : TaskObject
    {
        protected Action<TArg> Action { get; private set; }

        protected TArg Arg { get; private set; }

        public TaskObject(Action<TArg> action, TArg arg, bool withsession)
            : base(withsession)
        {
            Action = action;
            Arg = arg;
        }

        protected override void _InnerExecute()
        {
            Action(Arg);
        }
    }

    internal class TaskObject<TArg1, TArg2> : TaskObject
    {
        protected Action<TArg1, TArg2> Action { get; private set; }

        protected TArg1 Arg1 { get; private set; }

        protected TArg2 Arg2 { get; private set; }

        public TaskObject(Action<TArg1, TArg2> action, TArg1 arg1, TArg2 arg2, bool withsession)
            : base(withsession)
        {
            Action = action;
            Arg1 = arg1;
            Arg2 = arg2;
        }

        protected override void _InnerExecute()
        {
            Action(Arg1, Arg2);
        }
    }

    internal class TaskObject<TArg1, TArg2, TArg3> : TaskObject
    {
        protected Action<TArg1, TArg2, TArg3> Action { get; private set; }

        protected TArg1 Arg1 { get; private set; }

        protected TArg2 Arg2 { get; private set; }

        protected TArg3 Arg3 { get; private set; }
        public TaskObject(Action<TArg1, TArg2, TArg3> action, TArg1 arg1, TArg2 arg2, TArg3 arg3, bool withsession)
            : base(withsession)
        {
            Action = action;
            Arg1 = arg1;
            Arg2 = arg2;
            Arg3 = arg3;
        }

        protected override void _InnerExecute()
        {
            Action(Arg1, Arg2, Arg3);
        }
    }
    internal class TaskObject<TArg1, TArg2, TArg3, TArg4> : TaskObject
    {
        protected Action<TArg1, TArg2, TArg3, TArg4> Action { get; private set; }

        protected TArg1 Arg1 { get; private set; }

        protected TArg2 Arg2 { get; private set; }

        protected TArg3 Arg3 { get; private set; }

        protected TArg4 Arg4 { get; private set; }
        public TaskObject(Action<TArg1, TArg2, TArg3, TArg4> action, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, bool withsession)
            : base(withsession)
        {
            Action = action;
            Arg1 = arg1;
            Arg2 = arg2;
            Arg3 = arg3;
            Arg4 = arg4;
        }

        protected override void _InnerExecute()
        {
            Action(Arg1, Arg2, Arg3, Arg4);
        }
    }
    internal class TaskObject<TArg1, TArg2, TArg3, TArg4, TArg5> : TaskObject
    {
        protected Action<TArg1, TArg2, TArg3, TArg4, TArg5> Action { get; private set; }

        protected TArg1 Arg1 { get; private set; }

        protected TArg2 Arg2 { get; private set; }

        protected TArg3 Arg3 { get; private set; }

        protected TArg4 Arg4 { get; private set; }

        protected TArg5 Arg5 { get; private set; }
        public TaskObject(Action<TArg1, TArg2, TArg3, TArg4, TArg5> action, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, bool withsession)
            : base(withsession)
        {
            Action = action;
            Arg1 = arg1;
            Arg2 = arg2;
            Arg3 = arg3;
            Arg4 = arg4;
            Arg5 = arg5;
        }

        protected override void _InnerExecute()
        {
            Action(Arg1, Arg2, Arg3, Arg4, Arg5);
        }
    }
    internal class TaskObject<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6> : TaskObject
    {
        protected Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6> Action { get; private set; }

        protected TArg1 Arg1 { get; private set; }

        protected TArg2 Arg2 { get; private set; }

        protected TArg3 Arg3 { get; private set; }

        protected TArg4 Arg4 { get; private set; }

        protected TArg5 Arg5 { get; private set; }

        protected TArg6 Arg6 { get; private set; }
        public TaskObject(Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6> action, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, bool withsession)
            : base(withsession)
        {
            Action = action;
            Arg1 = arg1;
            Arg2 = arg2;
            Arg3 = arg3;
            Arg4 = arg4;
            Arg5 = arg5;
            Arg6 = arg6;
        }

        protected override void _InnerExecute()
        {
            Action(Arg1, Arg2, Arg3, Arg4, Arg5, Arg6);
        }
    }
    internal class TaskObject<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7> : TaskObject
    {
        protected Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7> Action { get; private set; }

        protected TArg1 Arg1 { get; private set; }

        protected TArg2 Arg2 { get; private set; }

        protected TArg3 Arg3 { get; private set; }

        protected TArg4 Arg4 { get; private set; }

        protected TArg5 Arg5 { get; private set; }

        protected TArg6 Arg6 { get; private set; }

        protected TArg7 Arg7 { get; private set; }
        public TaskObject(Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7> action, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, bool withsession)
            : base(withsession)
        {
            Action = action;
            Arg1 = arg1;
            Arg2 = arg2;
            Arg3 = arg3;
            Arg4 = arg4;
            Arg5 = arg5;
            Arg6 = arg6;
            Arg7 = arg7;
        }

        protected override void _InnerExecute()
        {
            Action(Arg1, Arg2, Arg3, Arg4, Arg5, Arg6, Arg7);
        }
    }
    internal class TaskObject<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8> : TaskObject
    {
        protected Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8> Action { get; private set; }

        protected TArg1 Arg1 { get; private set; }

        protected TArg2 Arg2 { get; private set; }

        protected TArg3 Arg3 { get; private set; }

        protected TArg4 Arg4 { get; private set; }

        protected TArg5 Arg5 { get; private set; }

        protected TArg6 Arg6 { get; private set; }

        protected TArg7 Arg7 { get; private set; }

        protected TArg8 Arg8 { get; private set; }
        public TaskObject(Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8> action, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, bool withsession)
            : base(withsession)
        {
            Action = action;
            Arg1 = arg1;
            Arg2 = arg2;
            Arg3 = arg3;
            Arg4 = arg4;
            Arg5 = arg5;
            Arg6 = arg6;
            Arg7 = arg7;
            Arg8 = arg8;
        }

        protected override void _InnerExecute()
        {
            Action(Arg1, Arg2, Arg3, Arg4, Arg5, Arg6, Arg7, Arg8);
        }
    }
    #endregion

    #region 有返回值
    internal class TaskObject2<TReturn> : TaskObject
    {
        protected Func<TReturn> Func { get; private set; }

        public TaskObject2(Func<TReturn> func, bool withsession)
            : base(withsession)
        {
            Func = func;
        }
        protected override void _InnerExecute()
        {
            Result = Func();
        }
    }

    internal class TaskObject2<TArg, TReturn> : TaskObject
    {
        protected Func<TArg, TReturn> Func { get; private set; }

        protected TArg Arg { get; private set; }

        public TaskObject2(Func<TArg, TReturn> func, TArg arg, bool withsession)
            : base(withsession)
        {
            Func = func;
            Arg = arg;
        }

        protected override void _InnerExecute()
        {
            Result = Func(Arg);
        }
    }

    internal class TaskObject2<TArg1, TArg2, TReturn> : TaskObject
    {
        protected Func<TArg1, TArg2, TReturn> Func { get; private set; }

        protected TArg1 Arg1 { get; private set; }

        protected TArg2 Arg2 { get; private set; }

        public TaskObject2(Func<TArg1, TArg2, TReturn> func, TArg1 arg1, TArg2 arg2, bool withsession)
            : base(withsession)
        {
            Func = func;
            Arg1 = arg1;
            Arg2 = arg2;
        }

        protected override void _InnerExecute()
        {
            Result = Func(Arg1, Arg2);
        }
    }
    internal class TaskObject2<TArg1, TArg2, TArg3, TReturn> : TaskObject
    {
        protected Func<TArg1, TArg2, TArg3, TReturn> Func { get; private set; }

        protected TArg1 Arg1 { get; private set; }

        protected TArg2 Arg2 { get; private set; }

        protected TArg3 Arg3 { get; private set; }

        public TaskObject2(Func<TArg1, TArg2, TArg3, TReturn> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, bool withsession)
            : base(withsession)
        {
            Func = func;
            Arg1 = arg1;
            Arg2 = arg2;
            Arg3 = arg3;
        }

        protected override void _InnerExecute()
        {
            Result = Func(Arg1, Arg2, Arg3);
        }
    }

    internal class TaskObject2<TArg1, TArg2, TArg3, TArg4, TReturn> : TaskObject
    {
        protected Func<TArg1, TArg2, TArg3, TArg4, TReturn> Func { get; private set; }

        protected TArg1 Arg1 { get; private set; }

        protected TArg2 Arg2 { get; private set; }

        protected TArg3 Arg3 { get; private set; }

        protected TArg4 Arg4 { get; private set; }
        public TaskObject2(Func<TArg1, TArg2, TArg3, TArg4, TReturn> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, bool withsession)
            : base(withsession)
        {
            Func = func;
            Arg1 = arg1;
            Arg2 = arg2;
            Arg3 = arg3;
            Arg4 = arg4;
        }

        protected override void _InnerExecute()
        {
            Result = Func(Arg1, Arg2, Arg3, Arg4);
        }
    }
    internal class TaskObject2<TArg1, TArg2, TArg3, TArg4, TArg5, TReturn> : TaskObject
    {
        protected Func<TArg1, TArg2, TArg3, TArg4, TArg5, TReturn> Func { get; private set; }

        protected TArg1 Arg1 { get; private set; }

        protected TArg2 Arg2 { get; private set; }

        protected TArg3 Arg3 { get; private set; }

        protected TArg4 Arg4 { get; private set; }

        protected TArg5 Arg5 { get; private set; }
        public TaskObject2(Func<TArg1, TArg2, TArg3, TArg4, TArg5, TReturn> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, bool withsession)
            : base(withsession)
        {
            Func = func;
            Arg1 = arg1;
            Arg2 = arg2;
            Arg3 = arg3;
            Arg4 = arg4;
            Arg5 = arg5;
        }

        protected override void _InnerExecute()
        {
            Result = Func(Arg1, Arg2, Arg3, Arg4, Arg5);
        }
    }
    internal class TaskObject2<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TReturn> : TaskObject
    {
        protected Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TReturn> Func { get; private set; }

        protected TArg1 Arg1 { get; private set; }

        protected TArg2 Arg2 { get; private set; }

        protected TArg3 Arg3 { get; private set; }

        protected TArg4 Arg4 { get; private set; }

        protected TArg5 Arg5 { get; private set; }

        protected TArg6 Arg6 { get; private set; }
        public TaskObject2(Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TReturn> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, bool withsession)
            : base(withsession)
        {
            Func = func;
            Arg1 = arg1;
            Arg2 = arg2;
            Arg3 = arg3;
            Arg4 = arg4;
            Arg5 = arg5;
            Arg6 = arg6;
        }

        protected override void _InnerExecute()
        {
            Result = Func(Arg1, Arg2, Arg3, Arg4, Arg5, Arg6);
        }
    }
    internal class TaskObject2<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TReturn> : TaskObject
    {
        protected Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TReturn> Func { get; private set; }

        protected TArg1 Arg1 { get; private set; }

        protected TArg2 Arg2 { get; private set; }

        protected TArg3 Arg3 { get; private set; }

        protected TArg4 Arg4 { get; private set; }

        protected TArg5 Arg5 { get; private set; }

        protected TArg6 Arg6 { get; private set; }

        protected TArg7 Arg7 { get; private set; }
        public TaskObject2(Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TReturn> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, bool withsession)
            : base(withsession)
        {
            Func = func;
            Arg1 = arg1;
            Arg2 = arg2;
            Arg3 = arg3;
            Arg4 = arg4;
            Arg5 = arg5;
            Arg6 = arg6;
            Arg7 = arg7;
        }

        protected override void _InnerExecute()
        {
            Result = Func(Arg1, Arg2, Arg3, Arg4, Arg5, Arg6, Arg7);
        }
    }
    internal class TaskObject2<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TReturn> : TaskObject
    {
        protected Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TReturn> Func { get; private set; }

        protected TArg1 Arg1 { get; private set; }

        protected TArg2 Arg2 { get; private set; }

        protected TArg3 Arg3 { get; private set; }

        protected TArg4 Arg4 { get; private set; }

        protected TArg5 Arg5 { get; private set; }

        protected TArg6 Arg6 { get; private set; }

        protected TArg7 Arg7 { get; private set; }

        protected TArg8 Arg8 { get; private set; }
        public TaskObject2(Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TReturn> func, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, bool withsession)
            : base(withsession)
        {
            Func = func;
            Arg1 = arg1;
            Arg2 = arg2;
            Arg3 = arg3;
            Arg4 = arg4;
            Arg5 = arg5;
            Arg6 = arg6;
            Arg7 = arg7;
            Arg8 = arg8;
        }

        protected override void _InnerExecute()
        {
            Result = Func(Arg1, Arg2, Arg3, Arg4, Arg5, Arg6, Arg7, Arg8);
        }
    }
    #endregion
    #endregion

    #region 可循环执行的任务对象
    /// <summary>
    /// 循环执行的任务对象
    /// </summary>
    internal class LoopTask
    {
        /// <summary>
        /// 任务的Id
        /// </summary>
        public string Id { get; private set; }
        /// <summary>
        /// 下次触发时间
        /// </summary>
        public ulong Next { get; private set; }
        /// <summary>
        /// 间隔时长
        /// </summary>
        public uint Seconds { get; private set; }

        /// <summary>
        /// 执行体
        /// </summary>
        public Action Action { get; private set; }
        /// <summary>
        /// 任务对象
        /// </summary>
        /// <param name="action"></param>
        /// <param name="next"></param>
        /// <param name="interval">间隔的秒数</param>
        public LoopTask(Action action, ulong next, uint interval)
        {
            Id = Serial.GetRandom(10);
            Action = action;
            Next = next;
            Seconds = interval;
        }

        public bool CanTrigger(ulong times)
        {
            return Next == times;
        }

        public void Excute()
        {
            TaskMgr.StartTask(Action);
            if (Seconds == 0)
                TaskMgr.RemoveTask(Id);
            else
                Next = Next + Seconds;
        }
    }
    #endregion

    #region 任务队列
    /// <summary>
    /// 先进先出的任务队列
    /// </summary>
    public class TaskQueue : IAsyncResult
    {
        AutoResetEvent ResetEvent = new AutoResetEvent(false);
        Queue<TaskObject> Tasks = new Queue<TaskObject>();
        protected bool Running { get; private set; }
        protected IAsyncResult AsyncResult { get; private set; }

        /// <summary>
        /// 时段限制，单位秒
        /// </summary>
        protected int TimeLimit { get; set; }

        /// <summary>
        /// 执行限制
        /// </summary>
        protected int FrequencyLimit { get; set; }

        /// <summary>
        /// 时段内已执行数量
        /// </summary>
        protected int Count { get; set; }
        /// <summary>
        /// 开始计算时间
        /// </summary>
        protected DateTime StartTime { get; set; }

        protected void InitStartTime(DateTime time)
        {
            if (TimeLimit == 0)
                StartTime = time;
            else
            {
                var tmp = (int)(time - time.Date).TotalSeconds / TimeLimit;
                StartTime = time.Date.AddSeconds(TimeLimit * tmp);
            }
        }
        /// <summary>
        /// 验证执行，弱次数耗尽则等待到时间限制结束
        /// </summary>
        /// <param name="now"></param>
        protected void ValidAndSleep(DateTime now)
        {
            if (TimeLimit == 0 || FrequencyLimit == 0)
                return;
            if ((now - StartTime).TotalSeconds > TimeLimit)
            {
                Count = 0;
                InitStartTime(now);
            }
            Count++;
            if (Count == FrequencyLimit)
                Thread.Sleep((int)(StartTime.AddSeconds(TimeLimit) - now).TotalMilliseconds);
        }
        /// <summary>
        /// 在N秒内最多执行m次的同步任务队列，为0表示无限
        /// </summary>
        /// <param name="timelimit">时间限制，单位秒</param>
        /// <param name="frequencylimit">次数限制</param>
        public TaskQueue(int timelimit = 0, int frequencylimit = 0)
        {
            Running = true;
            AsyncResult = TaskMgr.StartTask(ExecuteTask);
            TimeLimit = timelimit;
            FrequencyLimit = frequencylimit;
            InitStartTime(DateTime.Now);
        }

        /// <summary>
        /// 添加任务到队列中
        /// </summary>
        /// <param name="task"></param>
        public void AddTask(TaskObject task)
        {
            lock (Tasks)
            {
                if (Running)
                {
                    Tasks.Enqueue(task);
                    ResetEvent.Set();
                }
            }
        }

        void ExecuteTask()
        {
            TaskObject task = null;
            lock (Tasks)
            {
                if (Tasks.Count > 0)
                    task = Tasks.Dequeue();
            }
            if (task == null)
            {
                if (!Running)
                {
                    return;
                }
                ResetEvent.WaitOne();
            }
            else
            {
                Exception exp = null;
                try
                {
                    task.Execute();
                }
                catch (Exception ex)
                {
                    exp = ex;
                }
                finally
                {
                    task.Complete(exp);
                    ValidAndSleep(DateTime.Now);
                }
            }
            ExecuteTask();
        }

        public bool IsCompleted { get { return AsyncResult.IsCompleted; } }

        public WaitHandle AsyncWaitHandle { get { return AsyncResult.AsyncWaitHandle; } }

        public object AsyncState { get { return AsyncResult.AsyncState; } }

        public bool CompletedSynchronously { get { return AsyncResult.CompletedSynchronously; } }

        /// <summary>
        /// 关闭任务队列停止接收新任务
        /// </summary>
        public void Close()
        {
            lock (Tasks)
            {
                if (Running)
                {
                    Running = false;
                    if (Tasks.Count == 0)
                        ResetEvent.Set();
                }
            }
        }
    }


    #endregion
}

namespace Owl.Feature
{
    /// <summary>
    /// 回调参数
    /// </summary>
    public class TaskCallbackArg
    {
        public object Result { get; private set; }

        public Exception Exception { get; private set; }

        public TaskCallbackArg(object result, Exception exception)
        {
            Result = result;
            Exception = exception;
        }
    }
    /// <summary>
    /// 任务对象
    /// </summary>
    public abstract class TaskObject
    {
        /// <summary>
        /// 会话Id
        /// </summary>
        public string SessionId { get; private set; }

        /// <summary>
        /// 内部执行
        /// </summary>
        protected abstract void _InnerExecute();

        /// <summary>
        /// 执行完成之后的回掉函数
        /// </summary>
        protected Action<TaskCallbackArg> Callback { get; set; }

        AutoResetEvent m_ResetEvent;
        protected AutoResetEvent ResetEvent
        {
            get
            {
                if (m_ResetEvent == null)
                    m_ResetEvent = new AutoResetEvent(false);
                return m_ResetEvent;
            }
        }

        protected object Result { get; set; }

        protected Exception Exception { get; set; }
        /// <summary>
        /// 执行Task
        /// </summary>
        internal void Execute()
        {
            if (!string.IsNullOrEmpty(SessionId))
                Cache.SessionId = SessionId;
            Result = null;
            _InnerExecute();
        }

        bool IsCompleted = false;
        internal void Complete(Exception exp = null)
        {
            IsCompleted = true;
            Exception = exp;
            if (Callback != null)
                Callback(new TaskCallbackArg(Result, Exception));
            if (m_ResetEvent != null)
                m_ResetEvent.Set();
        }
        /// <summary>
        /// 操作完成之后执行
        /// </summary>
        /// <param name="action">参数为返回值</param>
        public TaskObject ContinueWith(Action<TaskCallbackArg> action)
        {
            Callback = action;
            return this;
        }
        /// <summary>
        /// 等待任务执行完毕,并返回执行结果
        /// </summary>
        /// <returns>返回执行结果</returns>
        public object WaitOne()
        {
            if (!IsCompleted)
            {
                ResetEvent.WaitOne();
                ResetEvent.Close();
            }
            if (Exception != null)
                throw new Exception(Exception.Message, Exception);
            return Result;
        }

        public TaskObject(bool withsession)
        {
            if (withsession)
                SessionId = Cache.SessionId;
        }
        #region  无返回值的任务
        /// <summary>
        /// 创建一个无参数的任务对象
        /// </summary>
        /// <param name="action"></param>
        /// <param name="withsession"></param>
        /// <returns></returns>
        public static TaskObject Create(Action action, bool withsession)
        {
            return new TaskObjectNoArg(action, withsession);
        }

        /// <summary>
        /// 创建包含一个参数的任务对象
        /// </summary>
        /// <typeparam name="TArg"></typeparam>
        /// <param name="action"></param>
        /// <param name="arg"></param>
        /// <param name="withsession"></param>
        /// <returns></returns>
        public static TaskObject Create<TArg>(Action<TArg> action, TArg arg, bool withsession)
        {
            return new TaskObject<TArg>(action, arg, withsession);
        }

        public static TaskObject Create<TArg1, TArg2>(Action<TArg1, TArg2> action,
            TArg1 arg1, TArg2 arg2, bool withsession)
        {
            return new TaskObject<TArg1, TArg2>(action, arg1, arg2, withsession);
        }
        public static TaskObject Create<TArg1, TArg2, TArg3>(Action<TArg1, TArg2, TArg3> action,
            TArg1 arg1, TArg2 arg2, TArg3 arg3, bool withsession)
        {
            return new TaskObject<TArg1, TArg2, TArg3>(action, arg1, arg2, arg3, withsession);
        }
        public static TaskObject Create<TArg1, TArg2, TArg3, TArg4>(Action<TArg1, TArg2, TArg3, TArg4> action,
            TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, bool withsession)
        {
            return new TaskObject<TArg1, TArg2, TArg3, TArg4>(action, arg1, arg2, arg3, arg4, withsession);
        }
        public static TaskObject Create<TArg1, TArg2, TArg3, TArg4, TArg5>(Action<TArg1, TArg2, TArg3, TArg4, TArg5> action,
            TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, bool withsession)
        {
            return new TaskObject<TArg1, TArg2, TArg3, TArg4, TArg5>(action, arg1, arg2, arg3, arg4, arg5, withsession);
        }
        public static TaskObject Create<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6> action,
            TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, bool withsession)
        {
            return new TaskObject<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(action, arg1, arg2, arg3, arg4, arg5, arg6, withsession);
        }
        public static TaskObject Create<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7>(Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7> action,
            TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, bool withsession)
        {
            return new TaskObject<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7>(action, arg1, arg2, arg3, arg4, arg5, arg6, arg7, withsession);
        }
        public static TaskObject Create<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8>(
            Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8> action,
            TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, bool withsession)
        {
            return new TaskObject<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8>(action, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, withsession);
        }
        #endregion

        public static TaskObject Create<TResult>(Func<TResult> func, bool withsession)
        {
            return new TaskObject2<TResult>(func, withsession);
        }

        public static TaskObject Create<TArg, TResult>(Func<TArg, TResult> func, TArg arg, bool withsession)
        {
            return new TaskObject2<TArg, TResult>(func, arg, withsession);
        }
        public static TaskObject Create<TArg1, TArg2, TResult>(Func<TArg1, TArg2, TResult> func, TArg1 arg1, TArg2 arg2, bool withsession)
        {
            return new TaskObject2<TArg1, TArg2, TResult>(func, arg1, arg2, withsession);
        }
        public static TaskObject Create<TArg1, TArg2, TArg3, TResult>(Func<TArg1, TArg2, TArg3, TResult> func,
            TArg1 arg1, TArg2 arg2, TArg3 arg3, bool withsession)
        {
            return new TaskObject2<TArg1, TArg2, TArg3, TResult>(func, arg1, arg2, arg3, withsession);
        }
        public static TaskObject Create<TArg1, TArg2, TArg3, TArg4, TResult>(Func<TArg1, TArg2, TArg3, TArg4, TResult> func,
            TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, bool withsession)
        {
            return new TaskObject2<TArg1, TArg2, TArg3, TArg4, TResult>(func, arg1, arg2, arg3, arg4, withsession);
        }
        public static TaskObject Create<TArg1, TArg2, TArg3, TArg4, TArg5, TResult>(Func<TArg1, TArg2, TArg3, TArg4, TArg5, TResult> func,
            TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, bool withsession)
        {
            return new TaskObject2<TArg1, TArg2, TArg3, TArg4, TArg5, TResult>(func, arg1, arg2, arg3, arg4, arg5, withsession);
        }
        public static TaskObject Create<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult>(Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult> func,
            TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, bool withsession)
        {
            return new TaskObject2<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult>(func, arg1, arg2, arg3, arg4, arg5, arg6, withsession);
        }
        public static TaskObject Create<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TResult>(Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TResult> func,
            TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, bool withsession)
        {
            return new TaskObject2<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TResult>(func, arg1, arg2, arg3, arg4, arg5, arg6, arg7, withsession);
        }
        public static TaskObject Create<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TResult>(
            Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TResult> func,
            TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, bool withsession)
        {
            return new TaskObject2<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TResult>(func, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, withsession);
        }
    }
    /// <summary>
    /// 任务池
    /// </summary>
    public class TaskPool
    {
        protected List<TaskQueue> Queues = new List<TaskQueue>();
        protected int Index = 0;
        /// <summary>
        /// 并发数
        /// </summary>
        protected int Count { get; private set; }
        /// <summary>
        /// 指定并发数及每并发时间规定时间内允许执行的任务数
        /// </summary>
        /// <param name="count">并发数</param>
        /// <param name="timelimit">秒</param>
        /// <param name="frequencylimit">次</param>
        public TaskPool(int count = 1, int timelimit = 0, int frequencylimit = 0)
        {
            Count = count;
            for (var i = 0; i < count; i++)
                Queues.Add(new TaskQueue(timelimit, frequencylimit));
        }
        /// <summary>
        /// 添加任务对象到任务池中
        /// </summary>
        /// <param name="taskObject"></param>
        public TaskObject AddTask(TaskObject taskObject)
        {
            lock (Queues)
            {
                Queues[Index].AddTask(taskObject);
                Index++;
                if (Index == Count)
                    Index = 0;
            }
            return taskObject;
        }
        /// <summary>
        /// 关闭任务池
        /// </summary>
        public void Close()
        {
            foreach (var queue in Queues)
                queue.Close();
        }
        /// <summary>
        /// 关闭任务池并等待所有任务完成
        /// </summary>
        public void WaitAll()
        {
            Close();
            foreach (var queue in Queues)
            {
                if (!queue.IsCompleted)
                    queue.AsyncWaitHandle.WaitOne();
            }
        }
    }
    /// <summary>
    /// 任务集
    /// </summary>
    public class TaskCollection : IEnumerable<IAsyncResult>
    {
        List<IAsyncResult> inner = new List<IAsyncResult>();
        public IEnumerator<IAsyncResult> GetEnumerator()
        {
            return inner.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return inner.GetEnumerator();
        }

        public void Add(IAsyncResult task)
        {
            inner.Add(task);
        }
        public void WaitAll()
        {
            foreach (var task in inner)
            {
                if (!task.IsCompleted)
                    task.AsyncWaitHandle.WaitOne();
            }
        }
        public void WaitAll(TimeSpan timespan)
        {
            foreach (var task in inner)
            {
                if (!task.IsCompleted)
                    task.AsyncWaitHandle.WaitOne(timespan);
            }
        }
    }


    /// <summary>
    /// 用于Task定时管理及高性能的异步调用
    /// </summary>
    public class TaskMgr
    {
        static Timer m_Timer;
        static ulong Times = 0;
        static DateTime StartTime;
        static TaskMgr()
        {
            Times = 0;
            var now = DateTime.Now;
            StartTime = now.Precision(TimePrecision.Second, true);
            m_Timer = new Timer(ScheduleTask, null, new TimeSpan((StartTime - now).Ticks), TimeSpan.FromSeconds(1));
        }
        static void ScheduleTask(object state)
        {
            LoopTask[] tasks;
            ulong times;
            lock (m_TaskObjs)
            {
                times = Times;
                Times = Times + 1;
                tasks = m_TaskObjs.Values.ToArray();
            }
            foreach (var task in tasks)
            {
                if (task.CanTrigger(times))
                    task.Excute();
            }
        }

        static Dictionary<string, LoopTask> m_TaskObjs = new Dictionary<string, LoopTask>();

        static ulong GetNext(DateTime next, DateTime start, uint interval)
        {
            if (next < start)
                return GetNext(next.AddSeconds(interval), start, interval);
            return (ulong)(next - StartTime).TotalSeconds;
        }
        /// <summary>
        /// 添加一个定期执行的任务
        /// </summary>
        /// <param name="action">任务操作</param>
        /// <param name="start">开始时间</param>
        /// <param name="interval">间隔的秒数 0表示仅执行一次</param>
        public static string AddTask(Action action, uint interval, DateTime start)
        {
            if (interval == 0 && start < StartTime)
                return null;
            lock (m_TaskObjs)
            {
                var task = new LoopTask(action, GetNext(start.Precision(TimePrecision.Second), StartTime, interval), interval);
                m_TaskObjs[task.Id] = task;
                return task.Id;
            }
        }
        /// <summary>
        /// 添加一个定期执行的任务
        /// </summary>
        /// <param name="action">任务操作</param>
        /// <param name="interval">间隔的秒数 0表示仅执行一次</param>
        /// <param name="dueseconds">延迟的秒数</param>
        /// <returns></returns>
        public static string AddTask(Action action, uint interval, uint dueseconds = 0)
        {
            lock (m_TaskObjs)
            {
                var task = new LoopTask(action, Times + dueseconds + 1, interval);
                m_TaskObjs[task.Id] = task;
                return task.Id;
            }
        }

        /// <summary>
        /// 删除任务
        /// </summary>
        /// <param name="key"></param>
        public static void RemoveTask(string key)
        {
            lock (m_TaskObjs)
            {
                if (m_TaskObjs.ContainsKey(key))
                    m_TaskObjs.Remove(key);
            }
        }


        static Action<Task> TaskDispose = task =>
        {
            task.Dispose();
        };

        /// <summary>
        /// 执行一个立即执行的任务
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public static Task StartTask(Action action)
        {
            Task task = Task.Factory.StartNew(TaskObject.Create(action, false).Execute);
            task.ContinueWith(TaskDispose);
            return task;
        }

        public static Task StartTask<TArg>(Action<TArg> action, TArg arg)
        {
            var task = Task.Factory.StartNew(TaskObject.Create(action, arg, false).Execute);
            task.ContinueWith(TaskDispose);
            return task;
        }

        public static Task StartTask<TArg1, TArg2>(Action<TArg1, TArg2> action, TArg1 arg1, TArg2 arg2)
        {
            var task = Task.Factory.StartNew(TaskObject.Create(action, arg1, arg2, false).Execute);
            task.ContinueWith(TaskDispose);
            return task;
        }

        public static Task StartTask<TArg1, TArg2, TArg3>(Action<TArg1, TArg2, TArg3> action, TArg1 arg1, TArg2 arg2, TArg3 arg3)
        {
            var task = Task.Factory.StartNew(TaskObject.Create(action, arg1, arg2, arg3, false).Execute);
            task.ContinueWith(TaskDispose);
            return task;
        }

        public static Task StartTask<TArg1, TArg2, TArg3, TArg4>(Action<TArg1, TArg2, TArg3, TArg4> action, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4)
        {
            var task = Task.Factory.StartNew(TaskObject.Create(action, arg1, arg2, arg3, arg4, false).Execute);
            task.ContinueWith(TaskDispose);
            return task;
        }
        public static Task StartTask<TArg1, TArg2, TArg3, TArg4, TArg5>(Action<TArg1, TArg2, TArg3, TArg4, TArg5> action, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5)
        {
            var task = Task.Factory.StartNew(TaskObject.Create(action, arg1, arg2, arg3, arg4, arg5, false).Execute);
            task.ContinueWith(TaskDispose);
            return task;
        }
        public static Task StartTask<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6> action, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6)
        {
            var task = Task.Factory.StartNew(TaskObject.Create(action, arg1, arg2, arg3, arg4, arg5, arg6, false).Execute);
            task.ContinueWith(TaskDispose);
            return task;
        }
        public static Task StartTask<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7>(Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7> action, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7)
        {
            var task = Task.Factory.StartNew(TaskObject.Create(action, arg1, arg2, arg3, arg4, arg5, arg6, arg7, false).Execute);
            task.ContinueWith(TaskDispose);
            return task;
        }
        public static Task StartTask<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8>(Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8> action, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8)
        {
            var task = Task.Factory.StartNew(TaskObject.Create(action, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, false).Execute); task.ContinueWith(TaskDispose);
            task.ContinueWith(TaskDispose);
            return task;
        }

        /// <summary>
        /// 执行一个立即执行的任务
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public static Task StartTaskWithSession(Action action)
        {
            Task task = Task.Factory.StartNew(TaskObject.Create(action, true).Execute);
            task.ContinueWith(TaskDispose);
            return task;
        }

        public static Task StartTaskWithSession<TArg>(Action<TArg> action, TArg arg)
        {
            var task = Task.Factory.StartNew(TaskObject.Create(action, arg, true).Execute);
            task.ContinueWith(TaskDispose);
            return task;
        }

        public static Task StartTaskWithSession<TArg1, TArg2>(Action<TArg1, TArg2> action, TArg1 arg1, TArg2 arg2)
        {
            var task = Task.Factory.StartNew(TaskObject.Create(action, arg1, arg2, true).Execute);
            task.ContinueWith(TaskDispose);
            return task;
        }

        public static Task StartTaskWithSession<TArg1, TArg2, TArg3>(Action<TArg1, TArg2, TArg3> action, TArg1 arg1, TArg2 arg2, TArg3 arg3)
        {
            var task = Task.Factory.StartNew(TaskObject.Create(action, arg1, arg2, arg3, true).Execute);
            task.ContinueWith(TaskDispose);
            return task;
        }

        public static Task StartTaskWithSession<TArg1, TArg2, TArg3, TArg4>(Action<TArg1, TArg2, TArg3, TArg4> action, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4)
        {
            var task = Task.Factory.StartNew(TaskObject.Create(action, arg1, arg2, arg3, arg4, true).Execute);
            task.ContinueWith(TaskDispose);
            return task;
        }
        public static Task StartTaskWithSession<TArg1, TArg2, TArg3, TArg4, TArg5>(Action<TArg1, TArg2, TArg3, TArg4, TArg5> action, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5)
        {
            var task = Task.Factory.StartNew(TaskObject.Create(action, arg1, arg2, arg3, arg4, arg5, true).Execute);
            task.ContinueWith(TaskDispose);
            return task;
        }
        public static Task StartTaskWithSession<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6> action, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6)
        {
            var task = Task.Factory.StartNew(TaskObject.Create(action, arg1, arg2, arg3, arg4, arg5, arg6, true).Execute);
            task.ContinueWith(TaskDispose);
            return task;
        }
        public static Task StartTaskWithSession<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7>(Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7> action, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7)
        {
            var task = Task.Factory.StartNew(TaskObject.Create(action, arg1, arg2, arg3, arg4, arg5, arg6, arg7, true).Execute);
            task.ContinueWith(TaskDispose);
            return task;
        }
        public static Task StartTaskWithSession<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8>(Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8> action, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8)
        {
            var task = Task.Factory.StartNew(TaskObject.Create(action, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, true).Execute);
            task.ContinueWith(TaskDispose);
            return task;
        }
    }
}
