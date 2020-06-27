using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Domain.Driver.Repository;
using Owl.Domain.Driver;
using Owl.Util;
using Owl.Feature;
namespace Owl.Domain.Driver
{
    /// <summary>
    /// 工作单元
    /// </summary>
    public class UnitOfWork
    {
        private static readonly string key = "Owl.Domain.Driver.UnitOfWork";
        /// <summary>
        /// 当前工作单元
        /// </summary>
        public static UnitOfWork Current
        {
            get
            {
                return Cache.Thread<UnitOfWork>(key);
            }
            set
            {
                Cache.Thread(key, value);
            }
        }

        #region 仓储上下文相关
        Dictionary<Type, RepositoryContext> m_contexts;
        /// <summary>
        /// 本工作单元包含的仓储上下文
        /// </summary>
        protected Dictionary<Type, RepositoryContext> Contexts
        {
            get
            {
                if (m_contexts == null)
                    m_contexts = new Dictionary<Type, RepositoryContext>();
                return m_contexts;
            }
        }
        /// <summary>
        /// 获取指定类型的仓储上下文
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public object GetContext(Type type)
        {
            if (!Contexts.ContainsKey(type))
                Contexts[type] = Activator.CreateInstance(type) as RepositoryContext;
            return Contexts[type];
        }
        /// <summary>
        /// 获取指定类型的仓储上下文
        /// </summary>
        /// <typeparam name="TContext"></typeparam>
        /// <returns></returns>
        public TContext GetContext<TContext>()
            where TContext : RepositoryContext, new()
        {
            Type type = typeof(TContext);
            if (!Contexts.ContainsKey(type))
                Contexts[type] = new TContext();
            return Contexts[type] as TContext;
        }
        #endregion

        #region 保存工作
        void Prepare()
        {
            foreach (var context in Contexts.Values.ToList())
            {
                if (!context.IsPrepared())
                    context.Prepare();
            }
            if (Contexts.Values.Any(s => !s.IsPrepared()))
                Prepare();
        }

        void Complete(bool success)
        {
            foreach (var context in Contexts.Values)
            {
                try
                {
                    context.Complete(success);
                }
                catch
                {
                }
            }
        }
        /// <summary>
        /// 保存并返回变更集
        /// </summary>
        /// <param name="transaction">是否事务提交</param>
        /// <param name="beforeComplete">保存之后，完成之前</param>
        /// <returns></returns>
        internal TransferObject Save(bool transaction = true, Action<Action> callback = null)
        {
            TransferObject result = null;
            bool success = false;
            try
            {
                Prepare();
                var running = new List<RepositoryContext>();
                try
                {
                    var obj = new TransferObject();
                    foreach (var context in Contexts.Values)
                    {
                        running.Add(context);
                        obj.Write(context.Execute(transaction));
                    }
                    result = obj;
                    foreach (var context in Contexts.Values)
                        context.Commit();
                    success = true;
                }
                catch (Exception ex)
                {
                    foreach (var context in running)
                        context.RollBack();
                    throw new Exception(ex.Message, ex);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
            finally
            {
                foreach (var context in Contexts.Values)
                    context.Dispose();
                Current = null;
                if (callback != null)
                    callback(() => Complete(success));
                else
                    Complete(success);
            }
            return result;
        }
        #endregion
    }
}
namespace Owl.Domain
{
    /// <summary>
    /// 工作单元的scop
    /// </summary>
    public class UnitofworkScope : IDisposable
    {
        UnitOfWork current;
        UnitOfWork last;
        public UnitofworkScope()
        {
            last = UnitOfWork.Current;
            current = new UnitOfWork();
            UnitOfWork.Current = current;
        }
        /// <summary>
        /// 提交并返回变更集
        /// </summary>
        /// <returns></returns>
        public TransferObject Complete(bool transaction = true, Action<Action> callback = null)
        {
            DomainContext.Current.Commiting = true;
            var result = current.Save(transaction, callback);
            current = null;
            return result;
        }


        public void Dispose()
        {
            UnitOfWork.Current = last;
        }
    }
}
