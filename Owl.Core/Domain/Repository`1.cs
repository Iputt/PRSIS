using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Domain.Driver.Repository;
using System.Linq.Expressions;
using Owl.Util;


namespace Owl.Domain
{
    /// <summary>
    /// 分组结果模式
    /// </summary>
    public enum ResultMode
    {
        /// <summary>
        /// 自身
        /// </summary>
        [DomainLabel("本字段")]
        Self,
        /// <summary>
        /// 和
        /// </summary>
        [DomainLabel("求和")]
        Sum,
        /// <summary>
        /// 总数
        /// </summary>
        [DomainLabel("计数")]
        Count,
        /// <summary>
        /// 平均数
        /// </summary>
        [DomainLabel("平均值")]
        Avg,
        /// <summary>
        /// 最大值
        /// </summary>
        [DomainLabel("最大值")]
        Max,
        /// <summary>
        /// 最小值
        /// </summary>
        [DomainLabel("最小值")]
        Min
    }
    /// <summary>
    /// 分组结果选择器
    /// </summary>
    public class ResultSelector
    {
        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; private set; }
        /// <summary>
        /// 选择字段
        /// </summary>
        public string Selector { get; private set; }
        /// <summary>
        /// 返回模式
        /// </summary>
        public ResultMode Mode { get; private set; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="mode"></param>
        public ResultSelector(string name, ResultMode mode)
        {
            Name = name;
            Selector = name;
            Mode = mode;
        }
        public ResultSelector(string name, string selector, ResultMode mode)
        {
            Name = name;
            Selector = selector;
            Mode = mode;
        }
    }

    /// <summary>
    /// 仓储管理， 统一的数据操作入口
    /// </summary>
    public class Repository
    {
        static RepositoryProvider GetProvider(ModelMetadata metadata)
        {
            return RepositoryProviderFactory.CreateProvider(metadata);
        }

        /// <summary>
        /// 不锁定查询
        /// </summary>
        public static bool NoLock
        {
            get { return RepositoryRunning.NoLock; }
            set { RepositoryRunning.NoLock = value; }
        }
        /// <summary>
        /// 根据条件语句获取 Lambda表达式
        /// </summary>
        /// <param name="meta"></param>
        /// <param name="specification"></param>
        /// <returns></returns>
        public static LambdaExpression GetExpression(ModelMetadata meta, string specification)
        {
            if (string.IsNullOrEmpty(specification) || specification.ToLower() == "true")
                return null;
            if (specification.ToLower() == "false")
                return LambdaExpression.Lambda(Expression.Constant(false), Expression.Parameter(meta.ModelType));
            var spec = Specification.Create(specification);
            if (spec == null)
                return LambdaExpression.Lambda(Expression.Constant(false), Expression.Parameter(meta.ModelType));
            return spec.GetExpression(meta);
        }

        public static AggRoot FindById(ModelMetadata metadata, Guid id, params string[] selector)
        {
            AggRoot entity = GetProvider(metadata).Find(id, selector);
            return entity;
        }

        public static AggRoot FindById(string modelanme, Guid id, params string[] selector)
        {
            return FindById(ModelMetadataEngine.GetModel(modelanme), id, selector);
        }

        public static AggRoot FindFirst(string modelname, string specification, params string[] selector)
        {
            var metadata = ModelMetadataEngine.GetModel(modelname);
            var expression = GetExpression(metadata, specification);
            return FindFirst(metadata, expression, selector);
        }

        public static AggRoot FindFirst(ModelMetadata metadata, Specification specification, params string[] selector)
        {
            var expression = specification.GetExpression(metadata);
            return FindFirst(metadata, expression, selector);
        }

        public static AggRoot FindFirst(ModelMetadata metadata, LambdaExpression expression, params string[] selector)
        {
            return GetProvider(metadata).FindFirst(expression, selector);
        }

        public static AggRoot FindFirst(string modelname, string specification, SortBy sortby, params string[] selector)
        {
            var metadata = ModelMetadataEngine.GetModel(modelname);
            var expression = GetExpression(metadata, specification);
            return FindFirst(metadata, expression, sortby, selector);
        }

        public static AggRoot FindFirst(ModelMetadata metadata, Specification specification, SortBy sortby, params string[] selector)
        {
            var expression = specification.GetExpression(metadata);
            return FindFirst(metadata, expression, sortby, selector);
        }
        public static AggRoot FindFirst(ModelMetadata metadata, LambdaExpression expression, SortBy sortby, params string[] selector)
        {
            return GetProvider(metadata).FindFirst(expression, sortby, selector);
        }


        public static bool Exists(ModelMetadata metadata, LambdaExpression expression)
        {
            return GetProvider(metadata).Exists(expression);
        }

        public static bool Exists(string modelname, string specification)
        {
            var metadata = ModelMetadataEngine.GetModel(modelname);
            return Exists(metadata, GetExpression(metadata, specification));
        }

        public static IEnumerable<AggRoot> FindAll(ModelMetadata metadata, LambdaExpression expression, SortBy sortby = null, int start = 0, int size = 0, params string[] selector)
        {
            if (sortby == null && size == 0)
                return GetProvider(metadata).FindAll(expression, start, size, selector);
            return GetProvider(metadata).FindAll(expression, sortby, start, size, selector);
        }

        public static IEnumerable<AggRoot> FindAll(ModelMetadata metadata, Specification specification, SortBy sortby = null, int start = 0, int size = 0, params string[] selector)
        {
            var expression = specification == null ? null : specification.GetExpression(metadata);
            return FindAll(metadata, expression, sortby, start, size, selector);
        }

        public static IEnumerable<AggRoot> FindAll(ModelMetadata metadata, string specification, SortBy sortby = null, int start = 0, int size = 0, params string[] selector)
        {
            return FindAll(metadata, GetExpression(metadata, specification), sortby, start, size, selector);
        }

        public static IEnumerable<AggRoot> FindAll(string modelname, string specification, SortBy sortby = null, int start = 0, int size = 0, params string[] selector)
        {
            var metadata = ModelMetadataEngine.GetModel(modelname);
            return FindAll(metadata, GetExpression(metadata, specification), sortby, start, size, selector);
        }

        public static IEnumerable<TransferObject> Read(ModelMetadata metadata, Guid[] id, bool translate = true, params string[] selector)
        {
            return GetProvider(metadata).Read(id, translate, selector);
        }

        public static IEnumerable<TransferObject> Read(string modelname, Guid[] id, bool translate = true, params string[] selector)
        {
            var metadata = ModelMetadataEngine.GetModel(modelname);
            return Read(metadata, id, translate, selector);
        }

        public static IEnumerable<TransferObject> GetList(ModelMetadata metadata, LambdaExpression expression, SortBy sortby, int start = 0, int size = 0, bool translate = false, params string[] selector)
        {
            return GetProvider(metadata).GetList(expression, sortby, start, size, translate, selector);
        }

        public static IEnumerable<TransferObject> GetList(ModelMetadata metadata, LambdaExpression expression, SortBy sortby, out int total, int start = 0, int size = 0, params string[] selector)
        {
            var provider = GetProvider(metadata);
            total = provider.Count(expression);
            return provider.GetList(expression, sortby, start, size, true, selector);
        }

        public static int Count(ModelMetadata metadata, LambdaExpression expression, params string[] groupselector)
        {
            return GetProvider(metadata).Count(expression, groupselector);
        }

        public static IDictionary<string, object> Sum(ModelMetadata metadata, LambdaExpression expression, params string[] selector)
        {
            return GetProvider(metadata).Sum(expression, selector);
        }
        /// <summary>
        /// groupby
        /// </summary>
        /// <param name="metadata"></param>
        /// <param name="expression"></param>
        /// <param name="keySelector"></param>
        /// <param name="resultSelector"></param>
        /// <param name="sortby"></param>
        /// <param name="start"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static IEnumerable<TransferObject> GroupBy(ModelMetadata metadata, LambdaExpression expression, IEnumerable<string> keySelector, IEnumerable<ResultSelector> resultSelector, SortBy sortby, int start = 0, int count = 0, bool translate = false)
        {
            return GetProvider(metadata).GroupBy(expression, keySelector, resultSelector, sortby, start, count, translate);
        }

        /// <summary>
        /// 添加或修改对象
        /// </summary>
        /// <param name="root"></param>
        public static void Push(AggRoot root)
        {
            root.Validate();
            GetProvider(root.Metadata).Push(root);
        }

        public static void UpdateAll(ModelMetadata metadata, LambdaExpression expression, TransferObject dto)
        {
            GetProvider(metadata).UpdateAll(expression, dto);
        }

        /// <summary>
        /// 删除对象
        /// </summary>
        /// <param name="root"></param>
        public static void Remove(AggRoot root)
        {
            GetProvider(root.Metadata).Remove(root);
        }


        /// <summary>
        /// 创建存储结构
        /// </summary>
        /// <param name="metadata">元数据</param>
        /// <param name="force">是否删除已有的结构</param>
        public static void CreateSchema(ModelMetadata metadata, bool force)
        {
            GetProvider(metadata).CreateSchema(force);
        }
        /// <summary>
        /// 判断表是否存在
        /// </summary>
        /// <returns></returns>
        public static bool SchemExists(ModelMetadata metadata)
        {
            return GetProvider(metadata).GetColumns().Count() > 0;
        }

        public static IEnumerable<string> GetColumns(ModelMetadata metadata)
        {
            return GetProvider(metadata).GetColumns();
        }

        public static void FixSchema(ModelMetadata metadata, string oldname)
        {
            GetProvider(metadata).FixSchema(oldname);
        }

        /// <summary>
        /// 向存储结构中添加字段
        /// </summary>
        /// <param name="field"></param>
        public static void AddColumn(ModelMetadata metadata, string field)
        {
            GetProvider(metadata).AddColumn(field);
        }
        /// <summary>
        /// 从存储结构删除字段
        /// </summary>
        /// <param name="field"></param>
        public static void DropColumn(ModelMetadata metadata, string field)
        {
            GetProvider(metadata).DropColumn(field);
        }
        /// <summary>
        /// 更改存储结构中的字段
        /// </summary>
        /// <param name="field"></param>
        /// <param name="newfield"></param>
        public static void ChangeColumn(ModelMetadata metadata, string field, string newfield)
        {
            GetProvider(metadata).ChangeColumn(field, newfield);
        }

        /// <summary>
        /// 备份数据到磁盘
        /// </summary>
        /// <param name="date"></param>
        /// <param name="version"></param>
        public static void Backup(ModelMetadata metadata, DateTime date, string version)
        {
            GetProvider(metadata).Backup(date, version);
        }
        /// <summary>
        /// 还原数据
        /// </summary>
        /// <param name="metadata"></param>
        /// <param name="date"></param>
        /// <param name="version"></param>
        public static void Restore(ModelMetadata metadata, DateTime date, string version)
        {
            RepositoryProviderFactory.CreateOrgProvider(metadata).Restore(date, version);
        }
        /// <summary>
        /// 执行仓储的自定义方法或存储过程
        /// </summary>
        /// <param name="metadata"></param>
        /// <param name="name"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public static object Invoke(ModelMetadata metadata, string name, IDictionary<string, object> param)
        {
            return GetProvider(metadata).Invoke(name, param);
        }
    }

    /// <summary>
    /// 仓储管理， 统一的数据操作入口
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Repository<T>
       where T : AggRoot
    {
        static readonly ModelMetadata MetaData = ModelMetadataEngine.GetModel(typeof(T).MetaName());
        static readonly RepositoryProvider<T> Provider = RepositoryProviderFactory.CreateProvider<T>(MetaData);
        /// <summary>
        /// 不锁定查询
        /// </summary>
        public static bool NoLock
        {
            get { return RepositoryRunning.NoLock; }
            set { RepositoryRunning.NoLock = value; }
        }
        /// <summary>
        /// 创建查询接口开始查询
        /// </summary>
        /// <returns></returns>
        public static IQueryable<T> CreateQuery()
        {
            return new System.Query<T>(new Owl.Domain.Driver.Repository.SmartQueryProvider<T>(MetaData));
        }

        /// <summary>
        /// 根据Id获取
        /// </summary>
        /// <param name="Id"></param>
        /// <param name="selector"></param>
        /// <returns></returns>
        public static T Get(Guid Id, params string[] selector)
        {
            return Provider.FindById(Id, selector);
        }

        /// <summary>
        /// 根据Id获取聚合
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        public static T FindById(Guid Id, params string[] selector)
        {
            return Provider.FindById(Id, selector);
        }
        /// <summary>
        /// 读取
        /// </summary>
        /// <param name="Id"></param>
        /// <param name="translate"></param>
        /// <param name="selector"></param>
        /// <returns></returns>
        public static TransferObject Read(Guid Id, bool translate, params string[] selector)
        {
            return Provider.Read(new Guid[] { Id }, translate, selector).FirstOrDefault();
        }

        /// <summary>
        /// 读取符合条件的列表（包含翻译）
        /// </summary>
        /// <param name="predicate"></param>
        /// <param name="sortby"></param>
        /// <param name="selector"></param>
        /// <returns></returns>
        public static IEnumerable<TransferObject> Read(Expression<Func<T, bool>> predicate, SortBy sortby, params string[] selector)
        {
            int total;
            return Read(predicate, sortby, out total, 0, 0, selector);
        }

        public static IEnumerable<TransferObject> Read(string specification, SortBy sortby, params string[] selector)
        {
            var predicate = Specification.Create(specification).getExpression<T>();
            return Read(predicate, sortby, selector);
        }

        /// <summary>
        /// 读取符合条件的列表（包含翻译）分页
        /// </summary>
        /// <param name="predicate"></param>
        /// <param name="sortby"></param>
        /// <param name="total"></param>
        /// <param name="start"></param>
        /// <param name="size"></param>
        /// <param name="selector"></param>
        /// <returns></returns>
        public static IEnumerable<TransferObject> Read(Expression<Func<T, bool>> predicate, SortBy sortby, out int total, int start = 0, int size = 0, params string[] selector)
        {
            total = Provider.Count(predicate);
            return Provider.GetList(predicate, sortby, start, size, true, selector);
        }

        /// <summary>
        /// 获取第一个符合条件的数据
        /// </summary>
        /// <param name="predicate"></param>
        /// <param name="selector"></param>
        /// <returns></returns>
        public static T FindFirst(Expression<Func<T, bool>> predicate, params string[] selector)
        {
            return Provider.FindFirst(predicate, selector);
        }
        /// <summary>
        /// 获取第一个符合条件的数据
        /// </summary>
        /// <param name="specification"></param>
        /// <param name="selector"></param>
        /// <returns></returns>
        public static T FindFirst(string specification, params string[] selector)
        {
            var predicate = Specification.Create(specification).getExpression<T>();
            return Provider.FindFirst(predicate, selector);
        }

        /// <summary>
        /// 查询
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public static IQueryable<T> Where(Expression<Func<T, bool>> predicate)
        {
            if (predicate == null)
                predicate = s => true;
            return CreateQuery().Where(predicate);
        }

        public static IQueryable<T> All()
        {
            return CreateQuery();
        }

        /// <summary>
        /// 是否存在符合条件的数据
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public static bool Exists(Expression<Func<T, bool>> predicate)
        {
            if (predicate == null)
                return true;
            return Provider.Exists(predicate);
        }
        /// <summary>
        /// 添加或修改对象
        /// </summary>
        /// <param name="root">对象</param>
        public static void Push(T root)
        {
            root.Validate();
            Provider.Push(root);
        }
        /// <summary>
        /// 批量更新符合条件的数据
        /// </summary>
        /// <param name="predicate">条件</param>
        /// <param name="dto">更新内容</param>
        public static void UpdateAll(Expression<Func<T, bool>> predicate, TransferObject dto)
        {
            Provider.UpdateAll(predicate, dto);
        }

        /// <summary>
        /// 删除实体
        /// </summary>
        /// <param name="root"></param>
        public static void Remove(T root)
        {
            Provider.Remove(root);
        }
        /// <summary>
        /// 批量删除符合条件的数据
        /// </summary>
        /// <param name="predicate"></param>
        public static void RemoveAll(Expression<Func<T, bool>> predicate)
        {
            using (var context = DomainContext.StartTransaction())
            {
                DomainContext.Current.Host(() => { Provider.RemoveAll(predicate); });
                context.Commit();
            }
        }

        /// <summary>
        /// 创建存储结构
        /// </summary>
        /// <param name="force">是否删除已有的结构</param>
        public static void CreateSchema(bool force)
        {
            Provider.CreateSchema(force);
        }
        /// <summary>
        /// 判断表是否存在
        /// </summary>
        /// <returns></returns>
        public static bool SchemExists()
        {
            return Provider.GetColumns().Count() > 0;
        }
        public static IEnumerable<string> GetColumns()
        {
            return Provider.GetColumns();
        }
        /// <summary>
        /// 从之前的表名称恢复到当前表名称，用于更换表名称
        /// </summary>
        /// <param name="oldname"></param>
        public static void FixSchema(string oldname)
        {
            Provider.FixSchema(oldname);
        }

        /// <summary>
        /// 删除存储结构
        /// </summary>
        public static void DropSchema()
        {
            Provider.DropSchema();
        }
        /// <summary>
        /// 向存储结构中添加字段
        /// </summary>
        /// <param name="field"></param>
        public static void AddColumn(string field)
        {
            Provider.AddColumn(field);
        }
        /// <summary>
        /// 从存储结构删除字段
        /// </summary>
        /// <param name="field"></param>
        public static void DropColumn(string field)
        {
            Provider.DropColumn(field);
        }
        /// <summary>
        /// 更改存储结构中的字段
        /// </summary>
        /// <param name="field"></param>
        /// <param name="newfield"></param>
        public static void ChangeColumn(string field, string newfield)
        {
            Provider.ChangeColumn(field, newfield);
        }
        /// <summary>
        /// 备份数据到磁盘
        /// </summary>
        /// <param name="date"></param>
        /// <param name="version"></param>
        public static void Dump(DateTime date, string version)
        {
            Provider.Backup(date, version);
        }

        public static void Restore(DateTime date, string version)
        {
            RepositoryProviderFactory.CreateOrgProvider<T>().Restore(date, version);
        }
        /// <summary>
        /// 执行仓储的自定义方法或存储过程
        /// </summary>
        /// <param name="name"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public static object Invoke(string name, IDictionary<string, object> param)
        {
            return Provider.Invoke(name, param);
        }
    }
}
