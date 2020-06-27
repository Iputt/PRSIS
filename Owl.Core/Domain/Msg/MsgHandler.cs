using Owl.Feature;
using Owl.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Owl.Domain
{
    /// <summary>
    /// 消息处理器
    /// </summary>
    public abstract class MsgHandler : BehaviorObject, IHandler
    {
        /// <summary>
        /// 该处理器对应的消息
        /// </summary>
        public Message Message { get; private set; }

        /// <summary>
        /// 消息描述
        /// </summary>
        public MsgDescrip Descrip { get; set; }

        /// <summary>
        /// 作用的对象
        /// </summary>
        public IEnumerable<AggRoot> Roots { get; protected set; }

        /// <summary>
        /// 消息处理器的外部表单参数
        /// </summary>
        public FormObject FormObj { get; private set; }

        protected override void _Write(TransferObject dto)
        {
            base._Write(dto);
            if (Descrip != null && Descrip.ParamMetadata != null && Metadata.Name != Descrip.ParamModel)
            {
                FormObj = DomainFactory.Create<FormObject>(Descrip.ParamMetadata);
                FormObj.Write(dto);
                FormObj.Validate();
            }
        }
        bool _inited = false;
        /// <summary>
        /// 初始化消息处理器
        /// </summary>
        /// <param name="msg"></param>
        public void Initial(Message msg)
        {
            if (_inited)
                throw new AlertException("error.owl.domain.message.handler.initialization.failure", "初始化已经完成，不能重复初始化！");
            Message = msg;
            Write(msg.Body);
            Validate();
            Prepare();
            if (Message.Modified.HasValue)
            {
                try
                {
                    if (Roots == null)
                        Roots = Message.GetRoot(Descrip.Restrict);
                }
                catch { }
                if (Roots != null && Roots.Count() > 0)
                {
                    var root = Roots.FirstOrDefault();
                    if (root.Modified.HasValue)
                    {
                        var modified = root.Modified.Value;
                        try
                        {
                            if (root.Metadata.Name == "om.sys.wf.instance")
                            {
                                var tran = root["Root"] as AggRoot;
                                modified = tran.Modified.Value;
                            }
                        }
                        catch { }
                        if (DateTime.Compare(Message.Modified.Value, modified.Precision(TimePrecision.Second)) != 0)
                            throw new AlertException(string.Format(Translation.Get("error.owl.domain.message.handler.data.change", "数据已被 {0} 修改，请确认最新数据后操作！"), root.ModifiedBy), ResType.info, true);
                    }
                }
            }
            _inited = true;
        }
        /// <summary>
        /// 执行准备工作
        /// </summary>
        protected abstract void Prepare();
        /// <summary>
        /// 执行消息处理
        /// </summary>
        /// <returns></returns>
        public abstract object Execute();

        #region 日志跟踪

        /// <summary>
        /// 整体执行跟踪信息
        /// </summary>
        public string EntireTrack
        {
            get
            {
                if (m_EntireTrackBuilder.Length > 0)
                    return m_EntireTrackBuilder.ToString();

                //if (FormObj != null)
                //    return FormObj.GetSummary();

                //if (RootTracks.Count > 0)
                //    return string.Join("\r\n", RootTracks.Values);
                return "";
            }
        }

        StringBuilder m_EntireTrackBuilder = new StringBuilder();
        /// <summary>
        /// 添加整体执行信息,用于消息执行完成后的提示
        /// </summary>
        /// <param name="resname"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        [Obsolete("本方法即将废弃,请用 AppendLog 代替", false)]
        public void AppendTrackEntire(string resname, string format, params object[] args)
        {
            AppendLog(resname, format, args);
        }

        /// <summary>
        /// 添加消息的执行概要，可用于消息执行完成后的提示信息和消息日志的整体记录
        /// </summary>
        /// <param name="resname"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void AppendLog(string resname, string format, params object[] args)
        {
            if (string.IsNullOrEmpty(resname) && string.IsNullOrEmpty(format))
                return;
            if (string.IsNullOrEmpty(resname))
                m_EntireTrackBuilder.AppendFormat(format, args);
            else
                m_EntireTrackBuilder.AppendFormat(Translation.Get(resname, format) ?? format, args);
        }

        Dictionary<Guid, StringBuilder> m_RootTrackBuilders = new Dictionary<Guid, StringBuilder>();
        /// <summary>
        /// 添加对象的执行信息，可用于单个对象的日志记录
        /// </summary>
        /// <param name="resname"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        [Obsolete("本方法即将废弃,请用 AppendLog 代替", false)]
        public void AppendTrackRoot(AggRoot root, string resname, string format, params object[] args)
        {
            AppendLog(root, resname, format, args);
        }
        /// <summary>
        /// 添加消息的对象执行信息，可用于记录本对象操作日志
        /// </summary>
        public void AppendLog(AggRoot root, string resname, string format, params object[] args)
        {
            if (string.IsNullOrEmpty(resname) && string.IsNullOrEmpty(format))
                return;
            StringBuilder builder = null;
            if (!m_RootTrackBuilders.ContainsKey(root.Id))
            {
                builder = new StringBuilder();
                m_RootTrackBuilders[root.Id] = builder;
            }
            else
                builder = m_RootTrackBuilders[root.Id];

            if (string.IsNullOrEmpty(resname))
                builder.AppendFormat(format, args);
            else
                builder.AppendFormat(Translation.Get(resname, format) ?? format, args);
        }
        /// <summary>
        /// 获取对象的执行日志
        /// </summary>
        /// <param name="rootid"></param>
        /// <returns></returns>
        public string GetLog(Guid rootid)
        {
            if (m_RootTrackBuilders.ContainsKey(rootid))
            {
                return m_RootTrackBuilders[rootid].ToString();
            }
            if (FormObj != null)
                return FormObj.GetSummary();
            return "";
        }
        #endregion
    }
    /// <summary>
    /// 通用消息处理器
    /// </summary>
    public abstract class GenericMessageHandler : MsgHandler
    {
        protected override void Prepare()
        {

        }
    }

    /// <summary>
    /// 对象消息处理器
    /// </summary>
    public abstract class RootMessageHandler : MsgHandler
    {
        protected override void Prepare()
        {
            Roots = Message.GetRoot(Descrip.Restrict);
            Roots.LoadNav(Descrip.Relations);
        }

        public sealed override object Execute()
        {
            if (Roots != null)
            {
                bool onlyone = Roots.Count() == 1;
                List<AggRoot> tmproots = new List<AggRoot>();
                foreach (var root in Roots)
                {
                    if (Descrip.Condition != null && !Descrip.Condition.IsValid(root))
                    {
                        if (onlyone)
                            throw new AlertException("error.owl.domain.message.handler.condition.novalid", "你无法在当前约束下执行此操作！");
                        else
                            continue;
                    }
                    tmproots.Add(root);
                }
                Roots = tmproots;
            }
            return _Execute();
        }

        protected virtual object _Execute()
        {
            if (Roots.Count() == 0)
                return null;
            List<object> objs = new List<object>();
            List<AggRoot> completes = new List<AggRoot>();
            List<Exception> exceptions = new List<Exception>();
            foreach (var root in Roots)
            {
                try
                {
                    object obj = null;
                    //if (Descrip.Singleton)
                    //{
                    //    if (!CheckHelper.CheckIn(string.Format("{0}.{1}.singlelock", root.Id, Descrip.Name), Descrip.SingleTimeout ?? 60))
                    //        throw new AlertException(Descrip.SingleNotifyResource, Descrip.SingleNotify);
                    //    try
                    //    {
                    //        using (var transaction = DomainContext.StartTransaction())
                    //        {
                    //            obj = _Execute(root);
                    //            transaction.Commit();
                    //        }
                    //    }
                    //    catch (Exception ex)
                    //    {
                    //        throw new Exception(ex.Message, ex);
                    //    }
                    //    finally
                    //    {
                    //        CheckHelper.CheckOut(string.Format("{0}.{1}.singlelock", root.Id, Descrip.Name));
                    //    }
                    //}
                    //else
                    //{
                    using (var transaction = DomainContext.StartTransaction())
                    {
                        obj = _Execute(root);
                        transaction.Commit();
                    }
                    //}

                    completes.Add(root);
                    if (obj != null)
                        objs.Add(obj);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                    MsgContext.Current.AppendTrackEntire("", "{0}<br>", ex.Message);
                }
            }
            Roots = completes;
            if (Roots.Count() == 0)
            {
                var exp = exceptions.FirstOrDefault();
                throw new Exception(exp.Message, exceptions.FirstOrDefault());
            }
            return objs.Count == 0 ? null : objs.Count == 1 ? objs[0] : objs;
        }

        protected virtual object _Execute(AggRoot root) { return null; }
    }
    /// <summary>
    /// 强类型根消息处理器
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class RootMessageHandler<T> : RootMessageHandler
        where T : AggRoot
    {
        public new IEnumerable<T> Roots
        {
            get { return base.Roots.Cast<T>(); }
            protected set { base.Roots = value; }
        }

        protected sealed override object _Execute(AggRoot root)
        {
            return execute((T)root);
        }

        protected virtual object execute(T root) { return null; }
    }
}
