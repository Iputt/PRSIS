using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Threading;
using System.Transactions;
using Owl.Domain.Driver;
using Owl.Util;


namespace Owl.Domain.Driver.Repository
{
    internal sealed class CachedRepositoryProvider<TEntity> : RepositoryProvider<TEntity>
        where TEntity : AggRoot
    {
        CachedRepositoryContext m_Context
        {
            get
            {
                return UnitOfWork.Current.GetContext<CachedRepositoryContext>();
            }
        }

        #region 加载缓存
        static readonly DateTime InitTime = DateTime.Parse("2000-01-01");
        static readonly SortBy orderbymodified = new SortBy() { { "Modified", SortOrder.Ascending } };
        void Load(DateTime lastupdate)
        {
            Expression<Func<TEntity, bool>> exp = s => s.Modified >= lastupdate;
            decimal count = entityMgr.Count(s => s.Modified >= lastupdate);
            int page = (count / 1500).Ceil();
            List<TEntity> records = new List<TEntity>();
            for (int i = 0; i < page; i++)
                records.AddRange(entityMgr.FindAll(exp, orderbymodified, i * 1500, 1500, null));
            foreach (var record in records)
            {
                Caches[record.Id] = record;
            }
        }
        DateTime LastUpdate = InitTime;
        IAsyncResult LoadComplete;
        Timer Timer;
        /// <summary>
        /// 完成加载
        /// </summary>
        void doneLoad()
        {
            if (LoadComplete == null)
            {
                Action<DateTime> func = Load;
                LoadComplete = func.BeginInvoke(InitTime, null, null);
                LastUpdate = DateTime.Now;
                TimerCallback callback = s =>
                {
                    var lastupdate = LastUpdate;
                    LastUpdate = DateTime.Now;
                    Load(lastupdate);
                };
                Timer = new Timer(callback, null, 180 * 1000, 60 * 3 * 1000);
            }
            if (!LoadComplete.IsCompleted)
                LoadComplete.AsyncWaitHandle.WaitOne();
        }

        SyncDictionary<Guid, AggRoot> Caches;
        IEnumerable<TEntity> ECaches
        {
            get
            {
                doneLoad();
                return Caches.Values.Cast<TEntity>();
            }
        }
        #endregion

        #region

        TEntity Copy(TEntity entity)
        {
            var dest = DomainFactory.Create<TEntity>(MetaData);
            dest.FromDb(entity.Read());
            return dest;
        }

        IEnumerable<TEntity> GetCopy(IEnumerable<TEntity> collection)
        {
            return collection.Select(s => Copy(s));
        }
        #endregion

        #region 仓储实现
        RepositoryProvider<TEntity> entityMgr;
        protected override void onInit()
        {
            entityMgr = RepositoryProviderFactory.CreateOrgProvider<TEntity>(MetaData);
            if (!CachedRepositoryContext.Caches.ContainsKey(MetaData.Name))
            {
                CachedRepositoryContext.Caches[MetaData.Name] = new SyncDictionary<Guid, AggRoot>();
            }
            Caches = CachedRepositoryContext.Caches[MetaData.Name];
        }
        protected override void DoCreateSchema(bool force)
        {
            entityMgr.CreateSchema(force);
        }
        protected override void DoDropSchema()
        {
            entityMgr.DropSchema();
        }
        protected override void DoFixSchema(string oldname)
        {
            entityMgr.FixSchema(oldname);
        }
        protected override void DoAddColumn(string field)
        {
            entityMgr.AddColumn(field);
        }
        protected override void DoDropCoumn(string field)
        {
            entityMgr.DropColumn(field);
        }
        protected override void DoChangeColumn(string field, string newfield)
        {
            entityMgr.ChangeColumn(field, newfield);
        }
        protected override void DoPush(TEntity entity)
        {
            entityMgr.Push(entity);
            m_Context.Push(entity);
        }
        protected override void DoUpdateAll(Expression<Func<TEntity, bool>> expression, TransferObject dto)
        {
            entityMgr.UpdateAll(expression, dto);
        }
        protected override void DoRemove(TEntity entity)
        {
            entityMgr.Remove(entity);
            if (Caches.ContainsKey(entity.Id))
                m_Context.Remove(entity);
        }
        protected override void DoRemoveAll(Expression<Func<TEntity, bool>> expression)
        {
            entityMgr.RemoveAll(expression);

        }
        DelegateCommand getcmd(Expression<Func<TEntity, bool>> expression)
        {
            return new DelegateTranslator(expression).Cmd;
        }
        Func<TEntity, bool> getFunc(DelegateCommand cmd)
        {
            Dictionary<string, object> dict = cmd.Parameters;
            var func = ((Func<TEntity, Dictionary<string, object>, bool>)cmd.Expresssion);
            return s => func(s, dict);
        }

        Func<TEntity, TProperty> getFunc<TProperty>(Expression<Func<TEntity, TProperty>> expression)
        {
            DelegateCommand cmd = new DelegateTranslator(expression).Cmd;
            Dictionary<string, object> dict = cmd.Parameters;
            return s => ((Func<TEntity, Dictionary<string, object>, TProperty>)cmd.Expresssion)(s, dict);
        }

        protected override TEntity DoFindById(Guid Id, string[] selector)
        {
            doneLoad();
            if (Caches.ContainsKey(Id))
                return Copy(Caches[Id] as TEntity);
            return null;
        }
        IEnumerable<TEntity> execute(DelegateCommand cmd, SortBy sortby, int start, int count, string[] selector)
        {
            IEnumerable<TEntity> result = ECaches.Where(getFunc(cmd));
            var parameter = Expression.Parameter(MetaData.ModelType);
            if (sortby != null)
            {
                foreach (var sort in sortby)
                {
                    var member = Expression.MakeMemberAccess(parameter, MetaData.GetField(sort.Key).PropertyInfo);
                    var exp = Expression.Lambda<Func<TEntity, dynamic>>(Expression.Convert(member, typeof(object)), parameter);
                    var func = getFunc(exp);
                    if (sort.Value == SortOrder.Ascending)
                        result = result.OrderBy(func);
                    else
                        result = result.OrderByDescending(func);
                }
            }
            if (count > 0)
                result = result.Skip(start).Take(count);
            return result;
        }
        protected override TEntity DoFindFirst(Expression<Func<TEntity, bool>> expression, SortBy sortby, string[] selector)
        {
            var cmd = getcmd(expression);
            if (cmd.HasRelation)
            {
                return entityMgr.FindFirst(expression, sortby, selector);
            }
            IEnumerable<TEntity> entities = ECaches.Where(getFunc(cmd));
            var parameter = Expression.Parameter(MetaData.ModelType);
            foreach (var sort in sortby)
            {
                var member = Expression.MakeMemberAccess(parameter, MetaData.GetField(sort.Key).PropertyInfo);
                var exp = Expression.Lambda<Func<TEntity, dynamic>>(Expression.Convert(member, typeof(object)), parameter);
                var func = getFunc(exp);
                if (sort.Value == SortOrder.Ascending)
                    entities = entities.OrderBy(func);
                else
                    entities = entities.OrderByDescending(func);
            }
            var entity = entities.FirstOrDefault();
            if (entity == null)
                return null;
            return Copy(entity);
        }
        protected override IEnumerable<TransferObject> DoRead(Guid[] ids, bool translate, string[] selector)
        {
            if (translate)
                return entityMgr.Read(ids, translate, selector);
            doneLoad();
            List<TransferObject> result = new List<TransferObject>();
            if (ids == null)
                return result;
            foreach (var id in ids)
            {
                if (Caches.ContainsKey(id))
                    result.Add(Caches[id].Read());
            }
            return result;
        }
        protected override IEnumerable<TransferObject> DoGetList(Expression<Func<TEntity, bool>> expression, SortBy sortby, int start, int size, bool translate, string[] selector)
        {
            if (translate)
                return entityMgr.GetList(expression, sortby, start, size, translate, selector);
            var cmd = getcmd(expression);
            if (cmd.HasRelation)
                return entityMgr.GetList(expression, sortby, start, size, translate, selector);
            var result = execute(cmd, sortby, start, size, selector);
            return result.Select(s => s.Read());
        }

        protected override IEnumerable<TEntity> DoFindAll(Expression<Func<TEntity, bool>> expression, SortBy sortby, int start, int count, string[] selector)
        {
            var cmd = getcmd(expression);
            if (cmd.HasRelation)
            {
                return entityMgr.FindAll(expression, sortby, start, count, selector);
            }
            var result = execute(cmd, sortby, start, count, selector);
            return GetCopy(result);
        }
        protected override bool DoExists(Expression<Func<TEntity, bool>> expression)
        {
            var cmd = getcmd(expression);
            if (cmd.HasRelation)
                return entityMgr.Exists(expression);
            return ECaches.Any(getFunc(cmd));
        }

        protected override int DoCount(Expression<Func<TEntity, bool>> expression, string[] groupselector)
        {
            if (LoadComplete == null || !LoadComplete.IsCompleted)
                return entityMgr.Count(expression, groupselector);
            var cmd = getcmd(expression);
            if (cmd.HasRelation)
                return entityMgr.Count(expression);
            return ECaches.Count(getFunc(cmd));
        }



        protected override IDictionary<string, object> DoSum(Expression<Func<TEntity, bool>> expression, string[] selector)
        {
            var cmd = getcmd(expression);
            if (cmd.HasRelation)
            {
                return entityMgr.Sum(expression, selector);
            }
            IEnumerable<TEntity> result = ECaches.Where(getFunc(cmd)).ToList();
            TransferObject record = new TransferObject();
            foreach (var property in selector)
            {
                if (MetaData.ContainField(property))
                {
                    var field = MetaData.GetField(property);
                    var ptype = TypeHelper.StripType(field.PropertyType);
                    object sum = null;
                    switch (Type.GetTypeCode(ptype))
                    {
                        case TypeCode.Byte:
                        case TypeCode.SByte:
                        case TypeCode.Int16:
                        case TypeCode.UInt16:
                            sum = field.Required ? result.Sum(s => Convert.ToInt32(s[property])) : result.Sum(s => Convert.ToInt32(s[property] ?? 0));
                            break;
                        case TypeCode.Int32:
                        case TypeCode.UInt32:
                        case TypeCode.Int64:
                        case TypeCode.UInt64:
                            sum = field.Required ? result.Sum(s => Convert.ToInt64(s[property])) : result.Sum(s => Convert.ToInt64(s[property] ?? 0));
                            break;
                        case TypeCode.Decimal:
                            sum = field.Required ? result.Sum(s => (decimal)s[property]) : result.Sum(s => ((decimal?)s[property]) ?? 0);
                            break;
                        case TypeCode.Double:
                        case TypeCode.Single:
                            sum = field.Required ? result.Sum(s => Convert.ToDouble(s[property])) : result.Sum(s => Convert.ToDouble(s[property] ?? 0));
                            break;
                    }
                    record[property] = sum;
                }
            }
            return record.ToDict();
        }

        protected override IEnumerable<TransferObject> DoGroupBy(Expression<Func<TEntity, bool>> expression, IEnumerable<string> keySelector, IEnumerable<ResultSelector> resultSelector, SortBy sortby, int start, int count, bool translate)
        {
            var cmd = getcmd(expression);
            if (cmd.HasRelation || translate)
                return entityMgr.GroupBy(expression, keySelector, resultSelector, sortby, start, count, translate);
            List<TransferObject> result = new List<TransferObject>();
            foreach (var group in ECaches.Where(getFunc(cmd)).GroupBy(s => string.Join(",", keySelector.Select(t => s[t]).Where(t => t != null))))
            {
                var dto = new TransferObject();
                foreach (var rs in resultSelector)
                {
                    switch (rs.Mode)
                    {
                        case ResultMode.Self: dto[rs.Name] = group.FirstOrDefault()[rs.Selector]; break;
                        case ResultMode.Sum: dto[rs.Name] = group.Sum(s => (dynamic)s[rs.Selector]); break;
                        case ResultMode.Count: dto[rs.Name] = group.Count(); break;
                        case ResultMode.Avg: dto[rs.Name] = group.Average(s => (dynamic)s[rs.Selector]); break;
                        case ResultMode.Max: dto[rs.Name] = group.Max(s => (dynamic)s[rs.Selector]); break;
                        case ResultMode.Min: dto[rs.Name] = group.Min(s => (dynamic)s[rs.Selector]); break;
                    }
                }
                result.Add(dto);
            }
            IEnumerable<TransferObject> results = result;
            int sc = 0;
            foreach (var sort in sortby)
            {
                if (sort.Value == SortOrder.Ascending)
                    results = sc == 0 ? result.OrderBy(s => s[sort.Key]) : ((IOrderedEnumerable<TransferObject>)results).ThenBy(s => s[sort.Key]);
                else
                    results = sc == 0 ? result.OrderByDescending(s => s[sort.Key]) : ((IOrderedEnumerable<TransferObject>)results).ThenBy(s => s[sort.Key]);
                sc += 1;
            }
            if (start != 0 && count == 0)
                return results.Skip(start).Take(count);
            return results;
        }
        #endregion

        protected override object DoInvoke(string name, IDictionary<string, object> param)
        {
            return null;
        }
    }
}
