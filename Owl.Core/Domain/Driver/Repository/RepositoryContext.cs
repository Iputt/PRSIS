using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace Owl.Domain.Driver.Repository
{
    /// <summary>
    /// 仓储上下文
    /// </summary>
    public abstract class RepositoryContext : IDisposable
    {
        /// <summary>
        /// 所有待添加对象
        /// </summary>
        protected Dictionary<string, AggRoot> ForAdd = new Dictionary<string, AggRoot>();
        /// <summary>
        /// 所有待更新对象
        /// </summary>
        protected Dictionary<string, AggRoot> ForUpdate = new Dictionary<string, AggRoot>();
        /// <summary>
        /// 所有待删除对象
        /// </summary>
        protected Dictionary<string, AggRoot> ForRemove = new Dictionary<string, AggRoot>();

        /// <summary>
        /// 上下文中待更新对象
        /// </summary>
        protected Dictionary<string, AggRoot> PushContext = new Dictionary<string, AggRoot>();

        /// <summary>
        /// 上下文中待删除对象
        /// </summary>
        protected Dictionary<string, AggRoot> RemoveContext = new Dictionary<string, AggRoot>();

        /// <summary>
        /// 将新增或修改的对象放入上下文
        /// </summary>
        /// <param name="root"></param>
        public virtual void Push(AggRoot root)
        {
            if (root == null ||
                ForAdd.ContainsKey(root.RootKey) ||
                ForUpdate.ContainsKey(root.RootKey) ||
                ForRemove.ContainsKey(root.RootKey) ||
                RemoveContext.ContainsKey(root.RootKey))
            {
                return;
            }

            PushContext[root.RootKey] = root;
        }
        /// <summary>
        /// 删除实体
        /// </summary>
        /// <param name="root"></param>
        public virtual void Remove(AggRoot root)
        {
            if (root == null ||
                ForAdd.ContainsKey(root.RootKey) ||
                ForUpdate.ContainsKey(root.RootKey) ||
                ForRemove.ContainsKey(root.RootKey) ||
                RemoveContext.ContainsKey(root.RootKey))
            {
                return;
            }
            if (PushContext.ContainsKey(root.RootKey))
            {
                PushContext.Remove(root.RootKey);
                if (RemoveContext[root.RootKey].IsLoaded)
                    RemoveContext[root.RootKey] = root;
            }
            else
                RemoveContext[root.RootKey] = root;
            RemoveContext[root.RootKey] = root;
        }

        /// <summary>
        /// 是否已经准备好提交数据
        /// </summary>
        /// <returns></returns>
        public virtual bool IsPrepared()
        {
            return RemoveContext.Count == 0 && PushContext.Count == 0;
        }

        /// <summary>
        /// 准备提交的数据
        /// </summary>
        public virtual void Prepare()
        {
            foreach (var pair in PushContext.ToList())
            {
                if (pair.Value.IsLoaded)
                    ForUpdate[pair.Key] = pair.Value;
                else
                    ForAdd[pair.Key] = pair.Value;
                pair.Value.SyncTime();
                PushContext.Remove(pair.Key);
            }
            foreach (var pair in RemoveContext.ToList())
            {
                ForRemove[pair.Key] = pair.Value;
                RemoveContext.Remove(pair.Key);
            }
        }

        /// <summary>
        /// 执行操作
        /// </summary>
        /// <param name="transaction">是否按事务执行</param>
        /// <returns></returns>
        public virtual TransferObject Execute(bool transaction) { return null; }

        /// <summary>
        /// 提交变更,返回变更集
        /// </summary>
        public abstract void Commit();

        /// <summary>
        /// 回滚数据
        /// </summary>
        public abstract void RollBack();

        /// <summary>
        /// 提交数据成功之后
        /// </summary>
        public virtual void Complete(bool success) { }

        public virtual void Dispose()
        {

        }
    }

    /// <summary>
    /// 仓储上下文实现基类
    /// </summary>
    public abstract class RealRepositoryContext : RepositoryContext
    {
        protected abstract TransferObject executeAdd(AggRoot entity);
        protected abstract TransferObject executeUpdate(AggRoot entity);
        protected abstract void executeRemove(AggRoot entity);

        public override void Prepare()
        {
            foreach (var pair in PushContext.ToList())
            {
                var root = pair.Value;
                PushContext.Remove(pair.Key);
                if (root.IsLoaded)
                    ForUpdate[pair.Key] = root;
                else
                    ForAdd[pair.Key] = root;
                root.SyncTime();
                foreach (var handler in RootEventHandler.GetHandler(root.Metadata))
                {
                    if (root.IsLoaded)
                    {
                        if (root.ContainsKey("Deleted") && root.GetRealValue<bool>("Deleted"))
                            handler.OnRemoving(root);
                        else
                            handler.OnUpdating(root);
                    }
                    else
                        handler.OnAdding(root);
                }
            }

            foreach (var pair in RemoveContext.ToList())
            {
                var root = pair.Value;
                RemoveContext.Remove(pair.Key);
                ForRemove[pair.Key] = root;
                foreach (var field in root.Metadata.GetFields<One2ManyField>(s => s.RelationModelMeta.ObjType == DomainType.AggRoot && s.RelationMode == RelationMode.Thick))
                {
                    var relroots = root[field.Name] as RelatedEnd;
                    if(relroots !=null)
                    {
                        relroots.Load();
                        foreach (AggRoot rel in (IEnumerable)relroots)
                            Remove(rel);
                    }
                }
                foreach (var handler in RootEventHandler.GetHandler(root.Metadata))
                    handler.OnRemoving(root);
            }
        }

        public override TransferObject Execute(bool transaction)
        {
            TransferObject result = new TransferObject();
            foreach (var entity in ForAdd.Values)
            {
                result[entity.RootKey] = executeAdd(entity);
            }
            foreach (var entity in ForUpdate.Values)
            {
                result[entity.RootKey] = executeUpdate(entity);
            }
            foreach (var entity in ForRemove.Values)
            {
                executeRemove(entity);
            }
            _Execute(transaction);
            return result;
        }

        protected abstract void _Execute(bool trnsaction);

        public override void Complete(bool success)
        {
            foreach (var entity in ForAdd.Values)
            {
                foreach (var handler in RootEventHandler.GetHandler(entity.Metadata))
                    handler.OnAdded(entity, success);
                entity.SyncOrg(entity.GetChanges());
            }
            foreach (var entity in ForUpdate.Values)
            {
                foreach (var handler in RootEventHandler.GetHandler(entity.Metadata))
                {
                    if (entity.ContainsKey("Deleted") && entity.GetRealValue<bool>("Deleted"))
                        handler.OnRemoved(entity, success);
                    else
                        handler.OnUpdated(entity, success);
                }

                entity.SyncOrg(entity.GetChanges());
            }
            foreach (var entity in ForRemove.Values)
            {
                foreach (var handler in RootEventHandler.GetHandler(entity.Metadata))
                    handler.OnRemoved(entity, success);
            }
        }
    }
}
