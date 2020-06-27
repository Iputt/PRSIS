using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using Owl.Domain.Driver.Repository;
using Owl.Util;
using System.Threading;
using Owl.Feature;
using Owl.Util.iAppConfig;
using System.Configuration;
using System.Collections;
namespace Owl.Domain
{

    public interface IRootCache : IEnumerable
    {
        string RootName { get; }
        bool IsLoaded { get; }
        void LoadLatest();
    }
    /// <summary>
    /// 缓存管理
    /// </summary>
    /// <typeparam name="TRoot">聚合根</typeparam>
    /// <typeparam name="TObj">缓存对象</typeparam>
    public abstract class RootCache<TCache, TRoot, TObj> : RootEventHandler<TRoot>, IRootCache
        where TCache : RootCache<TCache, TRoot, TObj>, new()
        where TRoot : AggRoot, new()
        where TObj : class, new()
    {
        #region property

        static object _LockerKey = new object();

        static TCache instance;
        /// <summary>
        /// 获取实例
        /// </summary>
        protected static TCache Instance
        {
            get
            {
                if (instance == null)//Double check 技术
                {
                    lock (_LockerKey)
                    {
                        if (instance == null)
                        {
                            instance = RootEventHandler.GetHandler<TCache>(typeof(TRoot));
                            if (instance == null) //用于 RootCache<TRoot> 直接调用缓存
                            {
                                instance = new TCache();
                                RootEventHandler.RegisterHandler(typeof(TRoot), instance);
                            }
                        }
                    }
                }
                return instance;
            }
        }

        ModelMetadata Meta;
        string TopicName;

        bool? m_schemaexists;
        /// <summary>
        /// 表是否在数据库中存在
        /// </summary>
        protected bool SchemaExists
        {
            get
            {
                if (m_schemaexists == null)
                {
                    m_schemaexists = RepositoryProviderFactory.CreateOrgProvider<TRoot>().GetColumns().Count() > 0;
                }
                return m_schemaexists.Value;
            }
            set { m_schemaexists = value; }
        }
        public RootCache()
        {
            var type = GetType();
            var type2 = typeof(TCache);
            if (type != type2 && !type.IsSubclassOf(type2))
                throw new AlertException("owl.domain.rootcache.typemismatch", "名称为 {0} 的 RootCache 的基类泛型类型不能为 {1}！", type.FullName, type2.FullName);
            Meta = ModelMetadataEngine.GetModel(typeof(TRoot));
            TopicName = string.Format("rootcache_{0}", type2.GetTypeName().ToLower().Replace("[", "_").Replace(",", "_").Replace("]", ""));
            MessageQueue.Subscrib<string>(TopicName, s =>
            {
                if (LoadComplete == null)
                    return;
                if (s.Body == "update")
                {
                    _Load(false, true);
                }
                else
                    RemoveObj(new TRoot() { Id = Guid.Parse(s.Body) });
            });
        }
        static bool? isxobj;
        /// <summary>
        /// 是否是owl的基础对象
        /// </summary>
        protected static bool IsXObj
        {
            get
            {
                if (isxobj == null)
                    isxobj = typeof(TObj).IsSubclassOf(typeof(Object2));
                return isxobj.Value;
            }
        }

        /// <summary>
        /// 缓存中的键 可在派生类的静态构造函数中初始化
        /// </summary>
        protected virtual Func<TRoot, string> KeySelector
        {
            get { return s => s.Id.ToString(); }
        }
        /// <summary>
        /// 需加载的字段
        /// </summary>
        protected virtual string[] Selectors
        {
            get
            {
                return new string[0];
            }
        }
        /// <summary>
        /// 缓存条件
        /// </summary>
        protected virtual Expression<Func<TRoot, bool>> Condition
        {
            get { return null; }
        }
        Func<TRoot, bool> m_func;
        protected bool IsMatch(TRoot root)
        {
            if (Condition == null)
                return true;
            if (m_func == null)
                m_func = Condition.Compile();
            return m_func(root);
        }

        protected readonly Dictionary<Guid, TObj> m_Caches = new Dictionary<Guid, TObj>();
        readonly Dictionary<string, Guid> m_Keys = new Dictionary<string, Guid>();
        protected static Dictionary<string, Guid> Keys
        {
            get
            {
                Instance._Load();
                return Instance.m_Keys;
            }
        }
        protected static Dictionary<Guid, TObj> Caches
        {
            get
            {
                Instance._Load();
                return Instance.m_Caches;
            }
        }
        void PushObj(TRoot root)
        {
            if (IsMatch(root))
            {
                TObj obj = null;
                bool bfirst = false;
                if (m_Caches.ContainsKey(root.Id))
                {
                    obj = m_Caches[root.Id];
                    if (m_Keys.Values.Contains(root.Id))
                        m_Keys.Remove(m_Keys.FirstOrDefault(s => s.Value == root.Id).Key);
                }
                else
                {
                    obj = new TObj();
                    bfirst = true;
                }
                SyncObj(root, obj, bfirst);
                if (bfirst)
                    m_Caches[root.Id] = obj;
                m_Keys[KeySelector(root)] = root.Id;
            }
            else
                RemoveObj(root);
        }
        void RemoveObj(TRoot root)
        {
            if (m_Caches.ContainsKey(root.Id))
            {
                var obj = m_Caches[root.Id];
                m_Caches.Remove(root.Id);
                m_Keys.Remove(m_Keys.FirstOrDefault(s => s.Value == root.Id).Key);
                OnRemove(root, obj);
            }
        }
        /// <summary>
        /// 同步对象
        /// </summary>
        /// <param name="root"></param>
        /// <param name="obj"></param>
        /// <param name="first">是否第一次同步</param>
        protected virtual void SyncObj(TRoot root, TObj obj, bool first)
        {
            if (IsXObj)
            {
                (obj as Object2).Write(root);
            }
        }
        /// <summary>
        /// 删除对象时
        /// </summary>
        /// <param name="root"></param>
        /// <param name="obj"></param>
        protected virtual void OnRemove(TRoot root, TObj obj)
        {
        }
        protected virtual void OnLoaded(IEnumerable<TRoot> roots)
        {

        }
        #endregion

        #region 加载缓存
        static readonly DateTime InitTime = DateTime.Parse("2000-01-01");
        DateTime LastUpdate = InitTime;
        protected System.Threading.Tasks.Task LoadComplete { get; private set; }
        object _LoadLockerKey = new object();
        /// <summary>
        /// 执行加载
        /// </summary>
        /// <param name="waitcomplete">等待加载完成</param>
        protected void _Load(bool waitcomplete = true, bool reload = false)
        {
            if (!SchemaExists)
                return;
            if (LoadComplete == null || (LoadComplete.IsCompleted && reload))
            {
                lock (_LoadLockerKey)
                {
                    if (LoadComplete == null || (LoadComplete.IsCompleted && reload))
                    {
                        var task = TaskMgr.StartTask(_DoLoad);
                        task.ContinueWith(s =>
                        {
                            if (LoadCount > 1000)
                                GC.Collect();
                        });
                        LoadComplete = task;
                    }
                }
            }
            if (waitcomplete && !LoadComplete.IsCompleted)
                LoadComplete.Wait();
            if (LoadComplete != null && LoadComplete.IsFaulted)
            {
                lock (_LoadLockerKey)
                {
                    if (LoadComplete != null && LoadComplete.IsFaulted)
                    {
                        var exp = LoadComplete.Exception;
                        LoadComplete = null;
                        throw exp;
                    }
                }

            }
        }
        void _DoLoad()
        {
            var lastupdate = LastUpdate;
            _DoLoad(lastupdate);
            LastUpdate = DateTime.Now;
        }
        int LoadCount;
        void _DoLoad(DateTime lastupdate)
        {
            RepositoryProvider<TRoot> entityMgr = RepositoryProviderFactory.CreateOrgProvider<TRoot>();
            ExpressionSpecification<TRoot> spec = ExpressionSpecification<TRoot>.Create(Condition);
            if (lastupdate != InitTime)
                spec = spec.Where(s => s.Modified >= lastupdate);
            var records = entityMgr.FindAll(spec.Exp, SortBy.Sortby_Modified, 0, 0, Selectors);
            LoadCount = records.Count();
            //var count = entityMgr.Count(exp);
            //var size = 1500;
            //int page = RepHelper.GetPage(count, size);
            //List<TRoot> records = new List<TRoot>();
            //for (int i = 0; i < page; i++)
            //    records.AddRange(entityMgr.FindAll(exp, orderbymodified, i * size, size, Selectors));
            //if (records.Count == 0)
            //    Thread.Sleep(500);
            //if (records.Count == 0)
            //    return;
            OnLoaded(records);
            foreach (var record in records)
            {
                PushObj(record);
            }

        }
        #endregion

        #region 公共方法
        /// <summary>
        /// 加载数据
        /// </summary>
        /// <param name="waitcomplete">是否等待加载完成</param>
        public static void Load(bool waitcomplete = true)
        {
            Instance._Load(waitcomplete);
        }
        /// <summary>
        /// 是否包含健
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static bool ContainsKey(string key)
        {
            if (key == null)
                return false;
            var sk = key.ToString();
            return Keys.ContainsKey(sk) && Caches.ContainsKey(Keys[sk]);
        }
        /// <summary>
        /// 根据键获取聚合根
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static TObj Get(string key)
        {
            if (ContainsKey(key))
                return Caches[Keys[key.ToString()]];
            return null;
        }

        public static TObj Get(Guid key)
        {
            if (Caches.ContainsKey(key))
                return Caches[key];
            return null;
        }

        /// <summary>
        /// 值
        /// </summary>
        public static IEnumerable<TObj> Values
        {
            get
            {
                return Caches.Values;
            }
        }
        #endregion

        protected override void OnRootAdding(TRoot root)
        {
            base.OnRootAdding(root);
        }
        protected sealed override void OnRootAdded(TRoot root)
        {
            if (!SchemaExists)
                SchemaExists = true;
            if (LoadComplete == null)
                return;
            PushObj(root);
            MessageQueue.Publish<string>(TopicName, "update");
        }
        protected sealed override void OnRootAddFailed(TRoot root)
        {
            base.OnRootAddFailed(root);
        }

        protected override void OnRootUpdating(TRoot root)
        {
            base.OnRootUpdating(root);
        }
        protected sealed override void OnRootUpdated(TRoot root)
        {
            if (!SchemaExists)
                SchemaExists = true;
            if (LoadComplete == null)
                return;
            PushObj(root);
            MessageQueue.Publish<string>(TopicName, "update");
        }
        protected sealed override void OnRootUpdateFailed(TRoot root)
        {
            base.OnRootUpdateFailed(root);
        }
        protected sealed override void OnRootRemoving(TRoot root)
        {
            base.OnRootRemoving(root);
        }
        protected sealed override void OnRootRemoved(TRoot root)
        {
            if (!SchemaExists)
                SchemaExists = true;
            if (LoadComplete == null)
                return;
            RemoveObj(root);
            MessageQueue.Publish<string>(TopicName, root.Id.ToString());
        }
        protected sealed override void OnRootRemoveFailed(TRoot root)
        {
            base.OnRootRemoveFailed(root);
        }

        public string RootName
        {
            get { return typeof(TRoot).MetaName(); }
        }

        public bool IsLoaded
        {
            get
            {
                return LoadComplete != null && LoadComplete.IsCompleted;
            }
        }

        public void LoadLatest()
        {
            if (LoadComplete == null || IsLoaded)
            {
                LoadComplete = null;
                _Load(false);
            }
        }

        public IEnumerator GetEnumerator()
        {
            return m_Caches.Values.GetEnumerator();
        }
    }

    public abstract class RootCache<TCache, TRoot> : RootCache<TCache, TRoot, TRoot>
        where TCache : RootCache<TCache, TRoot, TRoot>, new()
        where TRoot : AggRoot, new()
    {

    }

    /// <summary>
    /// 自缓存管理
    /// </summary>
    /// <typeparam name="TRoot"></typeparam>
    public sealed class RootCache<TRoot> : RootCache<RootCache<TRoot>, TRoot, TRoot>
        where TRoot : AggRoot, new()
    {

    }
}
