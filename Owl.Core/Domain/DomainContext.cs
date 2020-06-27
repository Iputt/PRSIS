using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Remoting.Messaging;
using Owl.Domain.Driver;
using Owl.Feature;

namespace Owl.Domain
{
    /// <summary>
    /// 事件保存类型
    /// </summary>
    public enum SaveEventType
    {
        Adding,
        Updating,
        Removing,
        Added,
        Updated,
        Removed,
        AddFailed,
        UpdateFailed,
        RemoveFailed
    }
    public interface IDomainTransaction : IDisposable
    {
        /// <summary>
        /// 提交本事务的数据
        /// </summary>
        void Commit(bool transaction = true);
    }
    internal class DomainTransaction : IDomainTransaction
    {
        DomainContext m_oldcontext;
        DomainContext current;
        public DomainTransaction()
        {
            m_oldcontext = DomainContext.GetCurrent();
            current = new DomainContext();
            DomainContext.Current = current;
        }

        /// <summary>
        /// Commit the specified transaction.
        /// </summary>
        /// <returns>The commit.</returns>
        /// <param name="transaction">是否事务提交</param>
        public void Commit(bool transaction = true)
        {
            current.Commit(transaction);
        }
        public void Dispose()
        {
            DomainContext.Current = m_oldcontext;
        }
    }

    /// <summary>
    /// 领域上下文，实现透明持久化
    /// </summary>
    public class DomainContext
    {
        internal DomainContext()
        {
            Init();
        }
        Dictionary<string, AggRoot> waitforpush;
        Dictionary<string, AggRoot> waitforremove;
        List<Action> m_actions;
        void Init()
        {
            waitforpush = new Dictionary<string, AggRoot>();
            waitforremove = new Dictionary<string, AggRoot>();
            m_actions = new List<Action>();
            Temp = new Dictionary<object, object>();
            TempSaveEvents = new Dictionary<string, Action<AggRoot, SaveEventType>>();
        }
        /// <summary>
        /// 临时存取区
        /// </summary>
        public Dictionary<object, object> Temp;

        public Dictionary<string, Action<AggRoot, SaveEventType>> TempSaveEvents;



        /// <summary>
        /// 将对象推送入当前上下文中
        /// </summary>
        /// <param name="root"></param>
        public void Phsh(AggRoot root)
        {
            if (root == null)
                return;
            //if (UnitOfWork.Current != null)
            //{
            //    Repository.Push(root);
            //    return;
            //}
            if (Commiting)
            {
                Repository.Push(root);
                return;
            }
            if (!waitforremove.ContainsKey(root.RootKey))
                waitforpush[root.RootKey] = root;
        }

        public bool InRemove(AggRoot root)
        {
            if (root.ContainsKey("Deleted") && root.GetRealValue<bool>("Deleted"))
            {
                return waitforpush.ContainsKey(root.RootKey);
            }
            else
                return waitforremove.ContainsKey(root.RootKey);
        }

        /// <summary>
        /// 删除上下文中的聚合根
        /// </summary>
        /// <param name="root">聚合根</param>
        public void Remove(AggRoot root)
        {
            if (root == null)
                return;
            //if (UnitOfWork.Current != null)
            //{
            //    Repository.Remove(root);
            //    return;
            //}
            if (Commiting)
            {
                Repository.Remove(root);
                return;
            }
            if (waitforpush.ContainsKey(root.RootKey))
            {
                waitforpush.Remove(root.RootKey);
                if (root.IsLoaded)
                    waitforremove[root.RootKey] = root;
            }
            else
                waitforremove[root.RootKey] = root;
        }


        /// <summary>
        /// 数据处理托管
        /// </summary>
        /// <param name="action"></param>
        public void Host(Action action)
        {
            m_actions.Add(action);
        }
        /// <summary>
        /// 提交处理中
        /// </summary>
        public bool Commiting { get; internal set; }
        /// <summary>
        /// 提交上下文并返会变更集
        /// <param name="transaction">是否事务提交</param>
        /// </summary>
        public TransferObject Commit(bool transaction = true)
        {
            TransferObject result = null;
            if (waitforpush.Count > 0 || waitforremove.Count > 0 || m_actions.Count > 0)
            {
                try
                {
                    using (UnitofworkScope scop = new UnitofworkScope())
                    {
                        foreach (var root in waitforpush.Values)
                            Repository.Push(root);
                        foreach (var root in waitforremove.Values)
                            Repository.Remove(root);
                        foreach (var action in m_actions)
                            action();
                        result = scop.Complete(transaction, complete =>
                        {
                            using (var trans = new DomainTransaction())
                            {
                                if (complete != null)
                                    complete();
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message, ex);
                }
                finally
                {
                    Commiting = false;
                    Init();
                }
            }
            return result;
        }

        /// <summary>
        /// 启动新事务
        /// </summary>
        public static IDomainTransaction StartTransaction()
        {
            return new DomainTransaction();
        }

        private static readonly string domaincontextkey = "Owl.Domain.Driver.DomainContext";

        internal static DomainContext GetCurrent()
        {
            return Cache.Thread<DomainContext>(domaincontextkey);
        }

        /// <summary>
        /// 当前领域上下文
        /// </summary>
        public static DomainContext Current
        {
            get
            {
                return Cache.Thread<DomainContext>(domaincontextkey, () => new DomainContext());
            }
            internal set
            {
                Cache.Thread(domaincontextkey, value);
            }
        }
    }
    /// <summary>
    /// 临时保存事件
    /// </summary>
    public class TempSaveEvent : RootEventHandler<AggRoot>
    {
        protected override int Priority => 10;
        protected override bool CanHandle(string modelname)
        {
            return DomainContext.Current.TempSaveEvents.ContainsKey(modelname);
        }
        protected override void OnRootAdding(AggRoot root)
        {
            base.OnRootAdding(root);
            DomainContext.Current.TempSaveEvents[root.Metadata.Name](root, SaveEventType.Adding);
        }
        protected override void OnRootUpdating(AggRoot root)
        {
            DomainContext.Current.TempSaveEvents[root.Metadata.Name](root, SaveEventType.Updating);
        }
        protected override void OnRootRemoving(AggRoot root)
        {
            base.OnRootRemoving(root);
            DomainContext.Current.TempSaveEvents[root.Metadata.Name](root, SaveEventType.Removing);
        }
        protected override void OnRootAdded(AggRoot root)
        {
            base.OnRootAdded(root);
            DomainContext.Current.TempSaveEvents[root.Metadata.Name](root, SaveEventType.Added);
        }
        protected override void OnRootUpdated(AggRoot root)
        {
            base.OnRootUpdated(root);

        }

        protected override void OnRootRemoved(AggRoot root)
        {
            base.OnRootRemoved(root);
            DomainContext.Current.TempSaveEvents[root.Metadata.Name](root, SaveEventType.Updated);
        }
        protected override void OnRootAddFailed(AggRoot root)
        {
            base.OnRootAddFailed(root);
            DomainContext.Current.TempSaveEvents[root.Metadata.Name](root, SaveEventType.AddFailed);
        }
        protected override void OnRootRemoveFailed(AggRoot root)
        {
            base.OnRootRemoveFailed(root);
            DomainContext.Current.TempSaveEvents[root.Metadata.Name](root, SaveEventType.RemoveFailed);
        }
        protected override void OnRootUpdateFailed(AggRoot root)
        {
            base.OnRootUpdateFailed(root);
            DomainContext.Current.TempSaveEvents[root.Metadata.Name](root, SaveEventType.UpdateFailed);
        }

    }
}
