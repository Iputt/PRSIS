using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Util;

namespace Owl.Domain
{
    /// <summary>
    /// 聚合根CRUD事件处理器
    /// </summary>
    public abstract class RootEventHandler
    {
        /// <summary>
        /// 对象添加前
        /// </summary>
        /// <param name="root"></param>
        public virtual void OnAdding(AggRoot root) { }

        /// <summary>
        /// 对象删除前
        /// </summary>
        /// <param name="root"></param>
        public virtual void OnRemoving(AggRoot root) { }
        /// <summary>
        /// 对象更新前
        /// </summary>
        /// <param name="root"></param>
        public virtual void OnUpdating(AggRoot root) { }
        /// <summary>
        /// 添加对象后
        /// </summary>
        /// <param name="root"></param>
        public virtual void OnAdded(AggRoot root, bool success) { }
        /// <summary>
        /// 删除对象后
        /// </summary>
        /// <param name="root"></param>
        public virtual void OnRemoved(AggRoot root, bool success) { }
        /// <summary>
        /// 更新对象后
        /// </summary>
        /// <param name="root"></param>
        public virtual void OnUpdated(AggRoot root, bool success) { }

        /// <summary>
        /// 处理器可处理的对象类型
        /// </summary>
        protected abstract Type[] ModelType { get; }

        /// <summary>
        /// 同个对象事件控制器的优先级，用于控制执行顺序
        /// </summary>
        protected virtual int Priority { get { return 1000; } }

        protected virtual bool CanHandle(string modelname)
        {
            return true;
        }
        Type m_type;
        /// <summary>
        /// 当前处理器的类型
        /// </summary>
        protected Type HandlerType
        {
            get
            {
                if (m_type == null)
                    m_type = GetType();
                return m_type;
            }
        }
        static Dictionary<Type, List<RootEventHandler>> Handlers = new Dictionary<Type, List<RootEventHandler>>(200);
        //static Dictionary<Type, RootEventHandler> AllHandlers = new Dictionary<Type, RootEventHandler>();
        static List<RootEventHandler> GenHandlers = new List<RootEventHandler>();
        static void AddHandler(List<RootEventHandler> handlers, RootEventHandler handler)
        {
            var ttype = handler.GetType();
            foreach (var hand in new List<RootEventHandler>(handlers))
            {
                var otype = hand.HandlerType;
                if (otype == ttype || otype.IsSubclassOf(ttype))
                    return;
                if (ttype.IsSubclassOf(otype))
                    handlers.Remove(hand);
            }
            handlers.Add(handler);
        }
        static void loadfromasm(string name, System.Reflection.Assembly asm)
        {
            foreach (var handler in TypeHelper.LoadFromAsm<RootEventHandler>(asm))
            {
                //AllHandlers[handler.GetType()] = handler;
                if (handler.ModelType != null)
                {
                    foreach (var type in handler.ModelType)
                    {
                        if (type == null)
                            continue;
                        if (!Handlers.ContainsKey(type))
                            Handlers[type] = new List<RootEventHandler>();
                        AddHandler(Handlers[type], handler);
                    }
                }
                else
                    AddHandler(GenHandlers, handler);
            }
        }
        static void unloadasm(string name, System.Reflection.Assembly asm)
        {
            foreach (var key in Handlers.Keys.Where(s => s.Assembly.FullName == asm.FullName).ToList())
                Handlers.Remove(key);
        }
        static RootEventHandler()
        {
            AsmHelper.RegisterResource(loadfromasm, unloadasm);
        }
        static IEnumerable<RootEventHandler> GetHandlerFromDict(Type type)
        {
            var handlers = new List<RootEventHandler>();
            if (type != null)
            {
                //if (Handlers.ContainsKey(type))
                //    handlers.AddRange(Handlers[type]);
                //handlers.AddRange(GetHandlerFromDict(type.BaseType));

                if (Handlers.ContainsKey(type))
                    handlers.InsertRange(0, Handlers[type].OrderByDescending(s => s.Priority));
                handlers.InsertRange(0, GetHandlerFromDict(type.BaseType));
            }
            return handlers;
        }

        protected static IEnumerable<RootEventHandler> _GetHandler(ModelMetadata metadata)
        {
            var modeltype = metadata.ModelType;
            var handlers = new List<RootEventHandler>();
            foreach (var handler in GenHandlers)
            {
                if (handler.CanHandle(metadata.Name))
                    handlers.Add(handler);
            }
            foreach (var handler in GetHandlerFromDict(modeltype))
            {
                if (handler.CanHandle(metadata.Name))
                    handlers.Add(handler);
            }
            return handlers;
        }
        /// <summary>
        /// 获取对象处理器
        /// </summary>
        /// <param name="modeltype">对象的类型</param>
        /// <returns></returns>
        public static IEnumerable<RootEventHandler> GetHandler(ModelMetadata metadata)
        {
            return Feature.Cache.Thread<IEnumerable<RootEventHandler>>(string.Format("roothandler.{0}", metadata.Name), () =>
            {
                return _GetHandler(metadata);
            });

        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="THandler"></typeparam>
        /// <param name="modeltype">对象的类型</param>
        /// <returns></returns>
        public static THandler GetHandler<THandler>(Type modeltype)
            where THandler : RootEventHandler
        {
            var handlertype = typeof(THandler);
            if (Handlers.ContainsKey(modeltype))
            {
                return Handlers[modeltype].FirstOrDefault(s => s.HandlerType == handlertype || s.HandlerType.IsSubclassOf(handlertype)) as THandler;
            }
            //if (AllHandlers.ContainsKey(typeof(THandler)))
            //    return AllHandlers[typeof(THandler)] as THandler;
            return null;
        }

        public static IEnumerable<RootEventHandler> GetHandlers()
        {
            return Handlers.SelectMany(s => s.Value);
        }

        /// <summary>
        /// 注册处理器
        /// </summary>
        /// <param name="type"></param>
        /// <param name="handler"></param>
        public static void RegisterHandler(Type type, RootEventHandler handler)
        {
            if (type == null || handler == null || Handlers == null)
                return;
            List<RootEventHandler> handlers = null;
            if (!Handlers.ContainsKey(type))
            {
                Handlers[type] = handlers = new List<RootEventHandler>();
            }
            else
                handlers = Handlers[type] as List<RootEventHandler>;
            AddHandler(handlers, handler);
        }
    }
    /// <summary>
    /// 聚合根事件处理器
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class RootEventHandler<T> : RootEventHandler
        where T : AggRoot
    {
        protected sealed override Type[] ModelType
        {
            get { return new Type[] { typeof(T) }; }
        }

        public sealed override void OnAdding(AggRoot root)
        {
            OnRootAdding(root as T);
        }
        public sealed override void OnRemoving(AggRoot root)
        {
            OnRootRemoving(root as T);
        }
        public sealed override void OnUpdating(AggRoot root)
        {
            OnRootUpdating(root as T);
        }



        public sealed override void OnAdded(AggRoot root, bool success)
        {
            if (success)
                OnRootAdded(root as T);
            else
                OnRootAddFailed(root as T);
        }

        public sealed override void OnRemoved(AggRoot root, bool success)
        {
            if (success)
                OnRootRemoved(root as T);
            else
                OnRootRemoveFailed(root as T);
        }

        public sealed override void OnUpdated(AggRoot root, bool success)
        {
            if (success)
                OnRootUpdated(root as T);
            else
                OnRootUpdateFailed(root as T);
        }
        /// <summary>
        /// 添加前
        /// </summary>
        /// <param name="root"></param>
        protected virtual void OnRootAdding(T root) { }
        /// <summary>
        /// 删除前
        /// </summary>
        /// <param name="root"></param>
        protected virtual void OnRootRemoving(T root) { }
        /// <summary>
        /// 更新前
        /// </summary>
        /// <param name="root"></param>
        protected virtual void OnRootUpdating(T root) { }
        /// <summary>
        /// 添加成功后
        /// </summary>
        /// <param name="root"></param>
        protected virtual void OnRootAdded(T root) { }
        /// <summary>
        /// 删除成功后
        /// </summary>
        /// <param name="root"></param>
        protected virtual void OnRootRemoved(T root) { }
        /// <summary>
        /// 更新成功后
        /// </summary>
        /// <param name="root"></param>
        protected virtual void OnRootUpdated(T root) { }

        /// <summary>
        /// 更新失败时
        /// </summary>
        /// <param name="root"></param>
        protected virtual void OnRootAddFailed(T root) { }

        /// <summary>
        /// 更新失败时
        /// </summary>
        /// <param name="root"></param>
        protected virtual void OnRootUpdateFailed(T root) { }

        /// <summary>
        /// 删除失败时
        /// </summary>
        /// <param name="root"></param>
        protected virtual void OnRootRemoveFailed(T root) { }
    }
}
