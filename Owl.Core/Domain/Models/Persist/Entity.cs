using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace Owl.Domain
{
    /// <summary>
    /// 实体基类
    /// </summary>
    public abstract class Entity : PersistObject, IEntity
    {
        /// <summary>
        /// 唯一标识符
        /// </summary>
        [DomainField(IgnoreLogModify = true, Resource = "fieldlabel.common.id")]
        public virtual Guid Id { get; set; }

        internal virtual void SyncTime(string modifiedby = null, DateTime? time = null)
        {
            foreach (var fild in Metadata.GetEntityRelated())
            {
                foreach (Entity entity in (EntityCollection)this[fild.Name])
                {
                    entity.SyncTime(modifiedby, time);
                }
            }
        }
        /// <summary>
        /// 同步对象数据库原始值, 数据更新之后自动执行
        /// </summary>
        /// <param name="dto"></param>
        internal void SyncOrg(ModelChange change)
        {
            if (change == null)
                return;

            foreach (var pair in change.Changes)
            {
                ((IDataTracer)this).Original[pair.Key] = pair.Value;
            }
            foreach (var navfield in Metadata.GetEntityRelated())
            {
                if (change.Children.ContainsKey(navfield.GetFieldname()))
                {
                    var childchanges = change.Children[navfield.GetFieldname()].ToDictionary(s => s.Key);
                    foreach (Entity entity in this[navfield.Name] as EntityCollection)
                    {
                        if (childchanges.ContainsKey(entity.Id))
                            entity.SyncOrg(childchanges[entity.Id]);
                    }
                }
            }
        }
        One2ManyField _field;
        DomainObject _topobj;
        /// <summary>
        /// 上级对象
        /// </summary>
        [IgnoreField]
        public DomainObject TopObj
        {
            get { return _topobj; }
            set
            {
                _topobj = value;
                if (value is Entity)
                {
                    if (_field == null)
                        _field = value.Metadata.GetFields<One2ManyField>(s => s.RelationModelMeta.Name == this.Metadata.Name).FirstOrDefault();
                    if (_field != null)
                    {
                        this[_field.RelationField] = (value as Entity).Id;
                    }
                }
            }
        }
        protected override object GetValue(string property)
        {
            if (property == "TopObj")
                return TopObj;
            return base.GetValue(property);
        }
        /// <summary>
        /// 获取编码
        /// </summary>
        /// <returns></returns>
        public virtual string GetNumber()
        {
            return Id.ToString();
        }
    }

    /// <summary>
    /// 排序的实体、行项目
    /// </summary>
    public abstract class OrderedEntity : Entity
    {
        [DomainField(Label = "序号")]
        public int? ItemNo { get; set; }
    }

    public abstract class Entity<TObj> : Entity
        where TObj : DomainObject
    {
        [IgnoreField]
        public TObj Parent
        {
            get { return (TObj)TopObj; }
            set { TopObj = value; }
        }
    }

    public abstract class EntityCollection : IEnumerable
    {
        public abstract void Add(Entity entity);

        public abstract void Remove(Entity entity);

        /// <summary>
        /// 数量
        /// </summary>
        public abstract int Count { get; }

        IEnumerator IEnumerable.GetEnumerator() { return _GetEnumerator(); }

        protected abstract IEnumerator _GetEnumerator();
    }

    /// <summary>
    /// 实体
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    [Serializable]
    public class EntityCollection<TEntity> : EntityCollection, ICollection<TEntity>, IEnumerable<TEntity>
        where TEntity : Entity
    {
        List<TEntity> collection;
        List<TEntity> waitforremove;
        DomainObject Topobj;
        public EntityCollection(DomainObject parent)
        {
            collection = new List<TEntity>();
            waitforremove = new List<TEntity>();
            Topobj = parent;
        }

        #region icollection 实现
        public void Add(TEntity entity)
        {
            entity.TopObj = Topobj;
            collection.Add(entity);
        }
        public bool Remove(TEntity entity)
        {
            if (((IDataTracer)entity).IsLoaded)
                waitforremove.Add(entity);
            return collection.Remove(entity);
        }
        public bool Remove(Predicate<TEntity> match)
        {
            foreach (var entity in collection.Where(s => match(s)).ToList())
                Remove(entity);
            return true;
        }

        public void Clear()
        {
            foreach (var entity in collection.ToList())
                Remove(entity);
        }

        public bool Contains(TEntity entity)
        {
            return collection.Contains(entity);
        }
        public override int Count
        {
            get { return collection.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }


        public IEnumerator<TEntity> GetEnumerator()
        {
            foreach (TEntity obj in collection)
            {
                yield return obj;
            }
        }
        protected override IEnumerator _GetEnumerator()
        {
            return GetEnumerator();
        }


        public void CopyTo(TEntity[] array, int arrayIndex)
        {
            collection.CopyTo(array, arrayIndex);
        }
        #endregion

        public TEntity GetFirst(Func<TEntity, bool> predicate)
        {
            return collection.FirstOrDefault(predicate);
        }


        /// <summary>
        /// 排序
        /// </summary>
        /// <param name="property">排序表达式</param>
        /// <param name="asc">是否正序</param>
        public void Sort<TKey>(Func<TEntity, TKey> expression, bool asc)
        {
            collection.Sort(asc? Comparer2<TEntity>.Asc(expression): Comparer2<TEntity>.Desc(expression));
        }

        /// <summary>
        /// 排序
        /// </summary>
        /// <param name="property">排序表达式</param>
        /// <param name="asc">是否正序</param>
        public void Sort(Func<TEntity, object> property, bool asc)
        {
            collection.Sort(asc ? Comparer2<TEntity>.Asc(property): Comparer2<TEntity>.Desc(property));
        }

        
        public void ForEach(Action<TEntity> action)
        {
            collection.ForEach(action);
        }
        /// <summary>
        /// 获取删除的实体
        /// </summary>
        /// <returns></returns>
        public IEnumerable<TEntity> GetRemoved()
        {
            return waitforremove.AsReadOnly();
        }

        public override void Add(Entity entity)
        {
            Add(entity as TEntity);
        }

        public override void Remove(Entity entity)
        {
            Remove(entity as TEntity);
        }
    }
}
