using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using Owl.Util;
using System.IO;


namespace Owl.Domain.Driver.Repository
{
    /// <summary>
    /// 仓储提供者基类
    /// </summary>
    public abstract class RepositoryProvider
    {
        #region 元数据
        /// <summary>
        /// 对象元数据
        /// </summary>
        protected ModelMetadata MetaData { get; private set; }

        /// <summary>
        /// 初始化对象元数据
        /// </summary>
        /// <param name="metadata"></param>
        internal void Init(ModelMetadata metadata)
        {
            MetaData = metadata;
            onInit();
        }
        /// <summary>
        /// 对象元数据初始化时
        /// </summary>
        protected abstract void onInit();
        #endregion

        #region 仓储架构相关
        /// <summary>
        /// 创建存储架构
        /// </summary>
        /// <param name="force">是否删除已有的结构</param>
        internal abstract void CreateSchema(bool force);


        /// <summary>
        /// 修正表名称
        /// </summary>
        /// <param name="oldname"></param>
        internal abstract void FixSchema(string oldname);

        /// <summary>
        /// 删除存储结构
        /// </summary>
        internal abstract void DropSchema();

        /// <summary>
        /// 获取数据库中的所有列名
        /// </summary>
        /// <returns></returns>
        internal abstract IEnumerable<string> GetColumns();

        /// <summary>
        /// 添加列
        /// </summary>
        /// <param name="name">列名</param>
        internal abstract void AddColumn(string name);
        /// <summary>
        /// 删除列
        /// </summary>
        /// <param name="name">列名</param>
        internal abstract void DropColumn(string name);
        /// <summary>
        /// 变更列
        /// </summary>
        /// <param name="name">老列名</param>
        /// <param name="newname">新列名</param>
        internal abstract void ChangeColumn(string name, string newname);
        #endregion

        #region 增删改查
        /// <summary>
        /// 将待添加或修改的对象放入仓储上下文中
        /// </summary>
        /// <param name="root">对象</param>
        internal abstract void Push(AggRoot root);

        /// <summary>
        /// 从仓储中删除指定对象
        /// </summary>
        /// <param name="root">对象</param>
        internal abstract void Remove(AggRoot root);

        /// <summary>
        /// 更新所有符合条件的数据
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="dto"></param>
        internal abstract void UpdateAll(LambdaExpression expression, TransferObject dto);

        /// <summary>
        /// 删除所有符合条件的数据
        /// </summary>
        /// <param name="expression"></param>
        internal abstract void RemoveAll(LambdaExpression expression);
        /// <summary>
        /// 根据id获取对象适用于前端展示
        /// </summary>
        /// <param name="id">标志符集合</param>
        /// <param name="translate">是否翻译</param>
        /// <param name="selector">选择项</param>
        /// <returns>字典集合</returns>
        internal abstract IEnumerable<TransferObject> Read(Guid[] id, bool translate, string[] selector);

        /// <summary>
        /// 获取符合条件的数据集合 适用于向UI提供数据
        /// </summary>
        /// <param name="expression">查询条件</param>
        /// <param name="sortby">排序方式</param>
        /// <param name="start">起始序号</param>
        /// <param name="size">每页大小</param>
        /// <param name="translate">是否转换</param>
        /// <param name="selector">返回筛选项</param>
        /// <returns></returns>
        internal abstract IEnumerable<TransferObject> GetList(LambdaExpression expression, SortBy sortby, int start, int size, bool translate, string[] selector);

        /// <summary>
        /// 根据Id查询数据
        /// </summary>
        /// <param name="Id">标识符</param>
        /// <param name="selector">选择器</param>
        /// <returns></returns>
        internal abstract AggRoot Find(Guid Id, string[] selector);

        /// <summary>
        /// 从仓储中获取第一个符合条件的实体
        /// </summary>
        /// <param name="expression">查询条件</param>
        /// <returns>符合条件的第一个实体</returns>
        internal abstract AggRoot FindFirst(LambdaExpression expression, string[] selector);


        /// <summary>
        /// 从仓储中获取第一个符合条件的实体
        /// </summary>
        /// <param name="expression">查询条件</param>
        /// <returns>符合条件的第一个实体</returns>
        internal abstract AggRoot FindFirst(LambdaExpression expression, SortBy sortby, string[] selector);
        /// <summary>
        /// 从仓储中获取第一个符合条件的最后一个实体
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="selector"></param>
        /// <returns></returns>
        internal abstract AggRoot FindLast(LambdaExpression expression, string[] selector);

        /// <summary>
        /// 获取最后一个符合条件的数据
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="sortby"></param>
        /// <param name="selector"></param>
        /// <returns></returns>
        internal abstract AggRoot FindLast(LambdaExpression expression, SortBy sortby, string[] selector);


        /// <summary>
        /// 从仓储中查询符合条件的数据（不排序）
        /// </summary>
        /// <param name="expression">查询条件</param>
        /// <param name="start">起始序号</param>
        /// <param name="count">Index</param>
        /// <param name="selector">字段过滤</param>
        /// <returns></returns>
        internal abstract IEnumerable<AggRoot> FindAll(LambdaExpression expression, int start, int count, string[] selector);

        /// <summary>
        /// 从仓储中查询符合条件的数据（排序）
        /// </summary>
        /// <param name="expression">查询条件</param>
        /// <param name="sortby">排序方式</param>
        /// <param name="start">起始</param>
        /// <param name="count">大小</param>
        /// <param name="selector">筛选</param>
        /// <returns></returns>
        internal abstract IEnumerable<AggRoot> FindAll(LambdaExpression expression, SortBy sortby, int start, int count, string[] selector);
        #endregion

        #region 聚合方法

        /// <summary>
        /// 判断是否存在符合条件的实体
        /// </summary>
        /// <param name="expression">查询条件</param>
        /// <returns>如果是True的话表示实体存在，否则不存在</returns>
        internal abstract bool Exists(LambdaExpression expression);
        /// <summary>
        /// 获取符合条件的总数
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="groupselector">分组字段</param>
        /// <returns></returns>
        internal abstract int Count(LambdaExpression expression, params string[] groupselector);
        /// <summary>
        /// 获取符合条件的数据汇总
        /// </summary>
        /// <param name="expression">查询条件</param>
        /// <param name="selector">需汇总的字段</param>
        /// <returns></returns>
        internal abstract IDictionary<string, object> Sum(LambdaExpression expression, string[] selector);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="keySelector"></param>
        /// <param name="resultSelector"></param>
        /// <param name="sortby"></param>
        /// <param name="start"></param>
        /// <param name="count"></param>
        /// <param name="translate"></param>
        /// <returns></returns>
        internal abstract IEnumerable<TransferObject> GroupBy(LambdaExpression expression, IEnumerable<string> keySelector, IEnumerable<ResultSelector> resultSelector, SortBy sortby, int start, int count, bool translate);
        #endregion

        #region 备份与恢复

        protected static string backpath = Path.Combine(AppConfig.Section.ResPath, "Bak");

        /// <summary>
        /// 备份数据到磁盘
        /// </summary>
        /// <param name="date">时间</param>
        internal abstract void Backup(DateTime date, string version);

        /// <summary>
        /// 从磁盘还原数据
        /// </summary>
        /// <param name="date"></param>
        internal abstract void Restore(DateTime date, string version);

        /// <summary>
        /// 从磁盘恢复最近一次备份
        /// </summary>
        internal abstract void RestoreLatest();
        #endregion

        #region 自定义方法调用
        /// <summary>
        /// 调用自定义过程或存储过程
        /// </summary>
        /// <param name="name">方法名称</param>
        /// <param name="param">参数</param>
        /// <returns></returns>
        internal abstract object Invoke(string name, IDictionary<string, object> param);
        #endregion
    }

    public abstract class RepositoryProvider<TAggRoot> : RepositoryProvider
        where TAggRoot : AggRoot
    {

        #region 仓储架构相关

        /// <summary>
        /// 创建存储架构
        /// </summary>
        protected abstract void DoCreateSchema(bool force);


        /// <summary>
        /// 
        /// </summary>
        /// <param name="oldname"></param>
        protected virtual void DoFixSchema(string oldname) { }
        /// <summary>
        /// 删除存储结构
        /// </summary>
        protected abstract void DoDropSchema();

        /// <summary>
        /// 获取数据库中现有的列
        /// </summary>
        /// <returns></returns>
        protected virtual IEnumerable<string> DoGetColumns() { return null; }
        /// <summary>
        /// 添加列
        /// </summary>
        /// <param name="field"></param>
        protected abstract void DoAddColumn(string field);
        /// <summary>
        /// 删除列
        /// </summary>
        /// <param name="field"></param>
        protected abstract void DoDropCoumn(string field);
        /// <summary>
        /// 更改列
        /// </summary>
        /// <param name="field"></param>
        /// <param name="newfield"></param>
        protected abstract void DoChangeColumn(string field, string newfield);


        /// <summary>
        /// 创建存储架构
        /// </summary>
        /// <param name="force">是否删除已有的结构</param>
        internal override sealed void CreateSchema(bool force) { if (!MetaData.GetDomainModel().NoTable) DoCreateSchema(force); }

        internal override IEnumerable<string> GetColumns()
        {
            return DoGetColumns() ?? new string[0];
        }

        internal override sealed void FixSchema(string oldname) { DoFixSchema(oldname); }
        /// <summary>
        /// 删除存储结构
        /// </summary>
        internal override sealed void DropSchema() { DoDropSchema(); }

        /// <summary>
        /// 添加列
        /// </summary>
        /// <param name="name">列名</param>
        internal override sealed void AddColumn(string name) { DoAddColumn(name); }
        /// <summary>
        /// 删除列
        /// </summary>
        /// <param name="name">列名</param>
        internal override sealed void DropColumn(string name) { DoDropCoumn(name); }
        /// <summary>
        /// 变更列
        /// </summary>
        /// <param name="name">老列名</param>
        /// <param name="newname">新列名</param>
        internal override sealed void ChangeColumn(string name, string newname) { DoChangeColumn(name, newname); }
        #endregion

        Expression<Func<TAggRoot, bool>> WrapExp(Expression<Func<TAggRoot, bool>> expression)
        {
            if (expression == null)
                return s => true;
            return expression;
        }

        Expression<Func<TAggRoot, bool>> WrapExp(LambdaExpression expression)
        {
            if (expression == null)
                return s => true;
            return (Expression<Func<TAggRoot, bool>>)expression;
        }

        #region 增删改

        /// <summary>
        /// 将待添加或修改的对象放入仓储上下文中
        /// </summary>
        /// <param name="root">聚合根</param>
        protected abstract void DoPush(TAggRoot root);

        /// <summary>
        /// 将待添加或修改的对象放入仓储上下文中
        /// </summary>
        /// <param name="root"></param>
        internal void Push(TAggRoot root)
        {
            string username = OwlContext.Current.UserName;
            DateTime now = DateTime.Now;
            if (!root.IsLoaded)
            {
                if (string.IsNullOrEmpty(root.CreatedBy))
                    root.CreatedBy = username;
                if (root.Created == null)
                    root.Created = now;
                if (string.IsNullOrEmpty(root.ModifiedBy))
                    root.ModifiedBy = username;
                if (root.Modified == null)
                    root.Modified = now;
            }
            else
            {
                root.Modified = now;
                root.ModifiedBy = username;
            }
            DoPush(root);
        }

        internal sealed override void Push(AggRoot root)
        {
            Push((TAggRoot)root);
        }

        protected abstract void DoUpdateAll(Expression<Func<TAggRoot, bool>> expression, TransferObject dto);

        internal void UpdateAll(Expression<Func<TAggRoot, bool>> expression, TransferObject dto)
        {
            if (expression == null || dto == null || dto.Count == 0)
                return;
            DoUpdateAll(expression, dto);
        }
        internal sealed override void UpdateAll(LambdaExpression expression, TransferObject dto)
        {
            UpdateAll(WrapExp(expression), dto);
        }


        /// <summary>
        /// 从仓储中删除一个实体
        /// </summary>
        /// <param name="root">待删除的实体</param>
        protected abstract void DoRemove(TAggRoot root);
        /// <summary>
        /// 从仓储中删除一个实体
        /// </summary>
        /// <param name="entity">将要被删除的实体</param>
        internal void Remove(TAggRoot entity)
        {
            DoRemove(entity);
        }
        internal override sealed void Remove(AggRoot root)
        {
            Remove((TAggRoot)root);
        }

        protected abstract void DoRemoveAll(Expression<Func<TAggRoot, bool>> expression);

        internal void RemoveAll(Expression<Func<TAggRoot, bool>> expression)
        {
            DoRemoveAll(expression);
        }
        internal sealed override void RemoveAll(LambdaExpression expression)
        {
            RemoveAll(WrapExp(expression));
        }
        #endregion

        #region UI查询
        /// <summary>
        /// 读取实体
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        protected abstract IEnumerable<TransferObject> DoRead(Guid[] id, bool translate, string[] selector);
        /// <summary>
        /// 根据id获取对象适用于前端展示
        /// </summary>
        /// <param name="id"></param>
        /// <returns>字典集合</returns>
        internal override sealed IEnumerable<TransferObject> Read(Guid[] id, bool translate, string[] selector)
        {
            return DoRead(id, translate, selector ?? new string[0]);
        }

        /// <summary>
        /// 按条件读取实体
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="sortby"></param>
        /// <param name="start"></param>
        /// <param name="size"></param>
        /// <param name="selector"></param>
        /// <returns></returns>
        protected abstract IEnumerable<TransferObject> DoGetList(Expression<Func<TAggRoot, bool>> expression, SortBy sortby, int start, int size, bool translate, string[] selector);

        internal IEnumerable<TransferObject> GetList(Expression<Func<TAggRoot, bool>> expression, SortBy sortby, int start, int size, bool translate, string[] selector)
        {
            return DoGetList(WrapExp(expression), sortby, start, size, translate, selector ?? new string[0]);
        }

        internal override IEnumerable<TransferObject> GetList(LambdaExpression expression, SortBy sortby, int start, int size, bool translate, string[] selector)
        {
            return DoGetList(WrapExp(expression), sortby, start, size, translate, selector ?? new string[0]);
        }
        #endregion

        #region 查询数据

        protected SortBy WrapSort(SortBy sortby)
        {
            return sortby == null || sortby.Count == 0 ? SortBy.Sortby_Modified : sortby;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        protected abstract TAggRoot DoFindById(Guid Id, string[] selector);

        /// <summary>
        /// 根据Id查询
        /// </summary>
        /// <param name="Id"></param>
        /// <param name="selector"></param>
        /// <returns></returns>
        internal TAggRoot FindById(Guid Id, string[] selector)
        {
            return DoFindById(Id, selector ?? new string[0]);
        }

        /// <summary>
        /// 根据Id查询
        /// </summary>
        /// <param name="Id"></param>
        /// <param name="selector"></param>
        /// <returns></returns>
        internal override sealed AggRoot Find(Guid Id, string[] selector)
        {
            return DoFindById(Id, selector ?? new string[0]);
        }

        /// <summary>
        /// 获取符合条件的第一条数据
        /// </summary>
        /// <typeparam name="TProperty"></typeparam>
        /// <param name="expression"></param>
        /// <param name="sortby"></param>
        /// <param name="selector"></param>
        /// <returns></returns>
        protected abstract TAggRoot DoFindFirst(Expression<Func<TAggRoot, bool>> expression, SortBy sortby, string[] selector);

        /// <summary>
        /// 从仓储中获取第一个符合条件的实体
        /// </summary>
        /// <param name="expression">查询条件</param>
        /// <returns>符合条件的第一个实体</returns>
        internal TAggRoot FindFirst(Expression<Func<TAggRoot, bool>> expression, string[] selector)
        {
            return DoFindFirst(WrapExp(expression), SortBy.Sortby_Modified, selector ?? new string[0]);
        }

        internal override sealed AggRoot FindFirst(LambdaExpression expression, string[] selector)
        {
            return DoFindFirst(WrapExp(expression), SortBy.Sortby_Modified, selector ?? new string[0]);
        }

        /// <summary>
        /// 从仓储中获取第一个符合条件的实体
        /// </summary>
        /// <param name="expression">查询条件</param>
        /// <returns>符合条件的第一个实体</returns>
        internal TAggRoot FindFirst(Expression<Func<TAggRoot, bool>> expression, SortBy sortby, string[] selector)
        {
            return DoFindFirst(WrapExp(expression), WrapSort(sortby), selector ?? new string[0]);
        }

        internal override AggRoot FindFirst(LambdaExpression expression, SortBy sortby, string[] selector)
        {
            return DoFindFirst(WrapExp(expression), WrapSort(sortby), selector ?? new string[0]);
        }


        /// <summary>
        /// 从仓储中获取第一个符合条件的最后一个实体
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="selector"></param>
        /// <returns></returns>
        internal TAggRoot FindLast(Expression<Func<TAggRoot, bool>> expression, string[] selector)
        {
            return DoFindFirst(WrapExp(expression), SortBy.Sortby_Modified_Desc, selector ?? new string[0]);
        }

        internal override AggRoot FindLast(LambdaExpression expression, string[] selector)
        {
            return DoFindFirst(WrapExp(expression), SortBy.Sortby_Modified_Desc, selector ?? new string[0]);
        }
        internal TAggRoot FindLast(Expression<Func<TAggRoot, bool>> expression, SortBy sortby, string[] selector)
        {
            var ts = new SortBy();
            foreach (var sort in sortby)
            {
                ts[sort.Key] = sort.Value == SortOrder.Ascending ? SortOrder.Descending : SortOrder.Ascending;
            }
            return DoFindFirst(WrapExp(expression), ts, selector ?? new string[0]);
        }
        internal override AggRoot FindLast(LambdaExpression expression, SortBy sortby, string[] selector)
        {
            return FindLast(WrapExp(expression), WrapSort(sortby), selector);
        }


        /// <summary>
        /// 查找符合条件的所有数据
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="sortby"></param>
        /// <param name="start"></param>
        /// <param name="count"></param>
        /// <param name="selector"></param>
        /// <returns></returns>
        protected abstract IEnumerable<TAggRoot> DoFindAll(Expression<Func<TAggRoot, bool>> expression, SortBy sortby, int start, int count, string[] selector);

        /// <summary>
        /// 从仓储中查询符合条件的数据（排序）
        /// </summary>
        /// <param name="expression">查询条件</param>
        /// <param name="sortby">排序方式</param>
        /// <param name="start">起始</param>
        /// <param name="count">大小</param>
        /// <param name="selector">筛选</param>
        /// <returns></returns>
        internal IEnumerable<TAggRoot> FindAll(Expression<Func<TAggRoot, bool>> expression, SortBy sortby, int start, int count, string[] selector)
        {
            return DoFindAll(WrapExp(expression), WrapSort(sortby), start, count, selector ?? new string[0]);
        }
        internal IEnumerable<TAggRoot> FindAll(Expression<Func<TAggRoot, bool>> expression, SortBy sortby, int start, int count, string[] selector, string[] relations)
        {
            var results = DoFindAll(WrapExp(expression), WrapSort(sortby), start, count, selector ?? new string[0]);
            if (relations != null && relations.Length > 0)
            {
                results.LoadNav(relations);
            }
            return results;
        }
        internal override IEnumerable<AggRoot> FindAll(LambdaExpression expression, SortBy sortby, int start, int count, string[] selector)
        {
            return DoFindAll(WrapExp(expression), WrapSort(sortby), start, count, selector ?? new string[0]);
        }

        /// <summary>
        /// 从仓储中查询符合条件的数据（不排序）
        /// </summary>
        /// <param name="expression">查询条件</param>
        /// <param name="start">起始Index</param>
        /// <param name="count">Index</param>
        /// <param name="selector">字段过滤</param>
        /// <returns></returns>
        internal IEnumerable<TAggRoot> FindAll(Expression<Func<TAggRoot, bool>> expression, int start, int count, string[] selector)
        {
            return FindAll(WrapExp(expression), WrapSort(null), start, count, selector);
        }

        internal override IEnumerable<AggRoot> FindAll(LambdaExpression expression, int start, int count, string[] selector)
        {
            return FindAll(WrapExp(expression), WrapSort(null), start, count, selector);
        }
        #endregion

        #region 聚合
        /// <summary>
        /// 判断是否存在
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        protected abstract bool DoExists(Expression<Func<TAggRoot, bool>> expression);
        /// <summary>
        /// 判断是否存在符合条件的实体
        /// </summary>
        /// <param name="expression">查询条件</param>
        /// <returns>如果是True的话表示实体存在，否则不存在</returns>
        internal bool Exists(Expression<Func<TAggRoot, bool>> expression)
        {
            expression = WrapExp(expression);
            return DoExists(expression);
        }
        internal override sealed bool Exists(LambdaExpression expression)
        {
            return DoExists(WrapExp(expression));
        }



        /// <summary>
        /// 获取符合条件的总数
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        protected abstract int DoCount(Expression<Func<TAggRoot, bool>> expression, string[] groupselector);

        /// <summary>
        /// 获取符合条件的总数
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        internal int Count(Expression<Func<TAggRoot, bool>> expression, params string[] groupselector)
        {
            return DoCount(WrapExp(expression), groupselector);
        }
        internal override sealed int Count(LambdaExpression expression, params string[] groupselector)
        {
            return DoCount(WrapExp(expression), groupselector);
        }


        /// <summary>
        /// 求和
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="selector"></param>
        /// <returns></returns>
        protected abstract IDictionary<string, object> DoSum(Expression<Func<TAggRoot, bool>> expression, string[] selector);


        /// <summary>
        /// 获取符合条件的数据汇总
        /// </summary>
        /// <param name="expression">查询条件</param>
        /// <param name="selector">需汇总的字段</param>
        /// <returns></returns>
        internal IDictionary<string, object> Sum(Expression<Func<TAggRoot, bool>> expression, string[] selector)
        {
            List<string> selectors = new List<string>();
            Dictionary<string, AtomSpecification> expr = new Dictionary<string, AtomSpecification>();
            foreach (var ts in selector)
            {
                if (MetaData.ContainField(ts))
                    selectors.Add(ts);
                else if (ts.Contains(" as "))
                {
                    var tf = ts.Split(new string[] { " as " }, StringSplitOptions.None);
                    var math = Specification.Math(tf[0]);
                    if (math != null && math.Members.All(s => MetaData.ContainField(s)))
                        selectors.AddRange(math.Members);
                    expr[tf[1]] = math;
                }
            }
            var sums = DoSum(WrapExp(expression), selectors.ToArray());
            if (expr.Count > 0)
            {
                var dto = new TransferObject(sums);
                foreach (var pair in expr)
                {
                    sums[pair.Key] = pair.Value.GetValue(dto);
                }
                foreach (var key in sums.Keys.ToList())
                {
                    if (!selector.Contains(key) && !expr.ContainsKey(key))
                        sums.Remove(key);
                }
            }
            return sums;
        }
        internal override IDictionary<string, object> Sum(LambdaExpression expression, string[] selector)
        {
            return Sum(WrapExp(expression), selector ?? new string[0]);
        }



        protected abstract IEnumerable<TransferObject> DoGroupBy(
            Expression<Func<TAggRoot, bool>> expression,
            IEnumerable<string> keySelector,
            IEnumerable<ResultSelector> resultSelector,
            SortBy sortby, int start, int count, bool translate);

        internal IEnumerable<TransferObject> GroupBy(
           Expression<Func<TAggRoot, bool>> expression,
           IEnumerable<string> keySelector,
           IEnumerable<ResultSelector> resultSelector,
           SortBy sortby, int start, int count, bool translate)
        {
            return DoGroupBy(WrapExp(expression), keySelector, resultSelector, sortby, start, count, translate);
        }

        internal override sealed IEnumerable<TransferObject> GroupBy(
            LambdaExpression expression,
            IEnumerable<string> keySelector,
            IEnumerable<ResultSelector> resultSelector,
            SortBy sortby, int start, int count, bool translate)
        {
            return DoGroupBy(WrapExp(expression), keySelector, resultSelector, sortby, start, count, translate);
        }

        internal IEnumerable<TResult> GroupByExp<TKey, TResult>(
            Expression<Func<TAggRoot, bool>> expression,
            Expression<Func<TAggRoot, TKey>> keySelector,
            Expression<Func<TKey, IEnumerable<TAggRoot>, TResult>> resultSelector,
            SortBy sortby, int start, int count)
        {
            expression = WrapExp(expression);
            Dictionary<string, string> keyselector = new Dictionary<string, string>();
            List<ResultSelector> resultselector = new List<ResultSelector>();
            var keybody = ExprHelper.StripQuotes(keySelector.Body);
            switch (keybody.NodeType)
            {
                case ExpressionType.MemberAccess:
                    keyselector[((MemberExpression)keybody).Member.Name] = ((MemberExpression)keybody).Member.Name;
                    break;
                case ExpressionType.New:
                    var newexp = ((NewExpression)keybody);
                    for (int i = 0; i < newexp.Members.Count; i++)
                    {
                        //var m = ExprHelper.GetMembers(newexp.Arguments[i]);
                        keyselector[newexp.Members[i].Name] = ExprHelper.GetMemberName((MemberExpression)newexp.Arguments[i]);
                    }
                    break;
            }
            switch (resultSelector.Body.NodeType)
            {
                case ExpressionType.MemberAccess:
                    resultselector.Add(new ResultSelector(((MemberExpression)resultSelector.Body).Member.Name, ResultMode.Self));
                    break;
                case ExpressionType.Parameter:
                    resultselector.Add(new ResultSelector(keyselector.Values.FirstOrDefault(), ResultMode.Self));
                    break;
                case ExpressionType.New:
                    var newexp = ((NewExpression)resultSelector.Body);
                    for (int i = 0; i < newexp.Members.Count; i++)
                    {
                        var name = newexp.Members[i].Name;
                        ResultSelector rselector = null;
                        switch (newexp.Arguments[i].NodeType)
                        {
                            case ExpressionType.Parameter:
                                rselector = new ResultSelector(name, keyselector.Values.FirstOrDefault(), ResultMode.Self);
                                break;
                            case ExpressionType.MemberAccess:
                                rselector = new ResultSelector(name, keyselector[((MemberExpression)newexp.Arguments[i]).Member.Name], ResultMode.Self);
                                break;
                            case ExpressionType.Call:
                                var call = (MethodCallExpression)newexp.Arguments[i];
                                var selector = "";
                                if (call.Arguments.Count > 1)
                                    selector = new SpecifactionTranslater().Translate(call.Arguments[1]).ToString();// ((MemberExpression)((LambdaExpression)call.Arguments[1]).Body).Member.Name;
                                switch (call.Method.Name)
                                {
                                    case "Sum": rselector = new ResultSelector(name, selector, ResultMode.Sum); break;
                                    case "Count": rselector = new ResultSelector(name, ResultMode.Count); break;
                                    case "Average": rselector = new ResultSelector(name, selector, ResultMode.Avg); break;
                                    case "Max": rselector = new ResultSelector(name, selector, ResultMode.Max); break;
                                    case "Min": rselector = new ResultSelector(name, selector, ResultMode.Min); break;
                                }
                                break;
                        }
                        if (rselector != null)
                            resultselector.Add(rselector);
                    }
                    break;
            }
            var record = DoGroupBy(expression, keyselector.Values, resultselector, sortby, start, count, false);
            switch (resultSelector.Body.NodeType)
            {
                case ExpressionType.Parameter:
                case ExpressionType.MemberAccess:
                    return record.Select(s => (TResult)s.Values.FirstOrDefault());
                case ExpressionType.New:
                    var constructor = ((NewExpression)resultSelector.Body).Constructor;
                    var param = constructor.GetParameters();
                    List<TResult> results = new List<TResult>();
                    foreach (var s in record)
                    {
                        object[] cp = new object[param.Length];
                        int i = 0;
                        foreach (var d in s.Where(d => resultselector.Any(t => t.Name == d.Key)))
                        {
                            cp[i] = Convert2.ChangeType(d.Value, param[i].ParameterType);
                            i++;
                        }
                        results.Add((TResult)constructor.Invoke(cp));
                    }
                    return results;
            }
            return null;
        }
        #endregion

        #region 备份与恢复
        /// <summary>
        /// 备份数据到磁盘
        /// </summary>
        /// <param name="date">时间</param>
        internal override sealed void Backup(DateTime date, string version)
        {
            if (MetaData.ModelType.IsSubclassOf(typeof(ClearRoot)))
                return;
            string path = Path.Combine(backpath, date.ToString("yyyy-MM-dd"), version, MetaData.Name);
            if (Directory.Exists(path))
                Directory.Delete(path, true);
            Directory.CreateDirectory(path);
            string filepath = "";
            var size = 500;
            if (MetaData.ContainField("Id"))
            {
                var count = Count(s => true);
                var times = RepHelper.GetPage(count, size);
                for (int i = 0; i < times; i++)
                {
                    filepath = Path.Combine(path, string.Format("bak{0}", (i + 1).ToString("D5")));
                    FileHelper.WriteAllText(filepath, FindAll(s => true, SortBy.Sortby_Id, i * size, size, null).ToJson());
                }
            }
            else
            {
                filepath = Path.Combine(path, "bak00001");
                FileHelper.WriteAllText(filepath, FindAll(s => true, 0, 0, null).ToJson());
            }
        }
        protected virtual void DoRestore(IEnumerable<TAggRoot> roots) { }


        protected void _Restore(string date, string version)
        {
            if (MetaData.ModelType.IsSubclassOf(typeof(ClearRoot)))
                return;
            string path = Path.Combine(backpath, date, version, MetaData.Name) + Path.DirectorySeparatorChar;
            if (!Directory.Exists(path))
                return;
            using (UnitofworkScope scop = new UnitofworkScope())
            {
                CreateSchema(true);
                scop.Complete();
            }

            foreach (var file in Directory.EnumerateFiles(path))
            {
                var json = File.ReadAllText(file.Replace("._", ""));
                if (MetaData.Name == "sc.res.configuration")
                {

                }
                var roots = json.DeJson<IEnumerable<TAggRoot>>();
                DoRestore(roots);
            }
        }
        /// <summary>
        /// 从磁盘还原数据
        /// </summary>
        /// <param name="date">日期 iso 格式 2000-01-01</param>
        internal override sealed void Restore(DateTime date, string version)
        {
            _Restore(date.ToString("yyyy-MM-dd"), version);
        }
        internal override sealed void RestoreLatest()
        {
            if (MetaData.ModelType.IsSubclassOf(typeof(ClearRoot)))
                return;
            var data = Directory.EnumerateDirectories(backpath).OrderByDescending(s => s).FirstOrDefault();
            var version = Directory.EnumerateDirectories(Path.Combine(backpath, data)).OrderByDescending(s => s).FirstOrDefault();
            _Restore(data, version);
        }
        #endregion

        /// <summary>
        /// 自定义方法调用
        /// </summary>
        /// <param name="name">名称</param>
        /// <param name="param">参数</param>
        /// <returns></returns>
        protected abstract object DoInvoke(string name, IDictionary<string, object> param);
        internal sealed override object Invoke(string name, IDictionary<string, object> param)
        {
            return DoInvoke(name, param);
        }
    }
}
