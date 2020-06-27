using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using Owl.Util;
using System.Collections;
using System.Reflection;
using Owl.Feature;

namespace Owl.Domain
{
    public struct ManyToManyPair
    {
        public string Key1 { get; set; }

        public Guid Value1 { get; set; }

        public string Key2 { get; set; }

        public Guid Value2 { get; set; }
    }

    public class ManyToManyTable
    {
        public string TableName { get; set; }

        public List<ManyToManyPair> ForAdd { get; set; }

        public List<ManyToManyPair> ForRemove { get; set; }
    }

    public abstract class RelatedEnd
    {
        /// <summary>
        /// 是否已加载
        /// </summary>
        public bool IsLoaded { get; protected set; }
        /// <summary>
        /// 加载关系
        /// </summary>
        /// <param name="force">是否强制加载</param>
        public abstract void Load(bool force = false);
        /// <summary>
        /// 获取查询
        /// </summary>
        /// <returns></returns>
        public abstract IQueryable GetQuery();
        /// <summary>
        /// 初始化关系
        /// </summary>
        /// <param name="items"></param>
        public abstract void Initialize(IEnumerable<AggRoot> items = null);

        /// <summary>
        /// 添加对象到关系
        /// </summary>
        /// <param name="root"></param>
        internal abstract void Add2(AggRoot root);

        /// <summary>
        /// 从关系中删除对象
        /// </summary>
        /// <param name="root"></param>
        internal abstract void Remove2(AggRoot root);

    }
    public abstract class RelatedEnd<TRoot> : RelatedEnd
        where TRoot : AggRoot
    {
        /// <summary>
        /// 关系对应的实体
        /// </summary>
        protected DomainObject Root { get; private set; }
        /// <summary>
        /// 元数据
        /// </summary>
        protected ModelMetadata Metadata { get; private set; }

        /// <summary>
        /// 本端元数据
        /// </summary>
        protected NavigatField SourceMeta { get; private set; }

        /// <summary>
        /// 对端元数据
        /// </summary>
        protected NavigatField TargetMeta { get; private set; }


        /// <summary>
        /// 是否多对多
        /// </summary>
        protected bool ManyToMany { get; private set; }


        public RelatedEnd(DomainObject root, string fieldname)
        {
            if (root == null)
                throw new ArgumentNullException("entity");
            Metadata = root.Metadata;
            if (!Metadata.ContainField(fieldname))
                throw new ArgumentOutOfRangeException("fieldname");
            SourceMeta = Metadata.GetField(fieldname) as NavigatField;
            ManyToMany = SourceMeta.Field_Type == FieldType.many2many;

            if (SourceMeta.Field_Type == FieldType.one2many)
                TargetMeta = SourceMeta.RelationModelMeta.GetFields<Many2OneField>(s => s.RelationModel == Metadata.Name && s.PrimaryField == SourceMeta.RelationField).FirstOrDefault();
            else if (SourceMeta.Field_Type == FieldType.many2one)
                TargetMeta = SourceMeta.RelationModelMeta.GetFields<One2ManyField>(s => s.RelationModel == Metadata.Name && s.RelationField == SourceMeta.PrimaryField).FirstOrDefault();
            else
                TargetMeta = SourceMeta.RelationModelMeta.GetFields<Many2ManyField>(s => s.RelationModel == Metadata.Name && s.RelationField == SourceMeta.GetFieldname()).FirstOrDefault();
            Root = root;
        }

        Expression getSource(Expression para)
        {
            if (SourceMeta.PrimaryInfo != null)
                return Expression.MakeMemberAccess(para, SourceMeta.PrimaryInfo);
            else
                return Expression.Convert(Expression.Call(para, Metadata.GetItem, Expression.Constant(SourceMeta.PrimaryField)), SourceMeta.PrimaryType);
        }

        Expression getTarget(Expression para)
        {
            if (SourceMeta.RelationInfo != null)
                return Expression.MakeMemberAccess(para, SourceMeta.RelationInfo);
            else
                return Expression.Convert(Expression.Call(para, SourceMeta.RelationModelMeta.GetItem, Expression.Constant(SourceMeta.RelationField)), SourceMeta.RelationFieldType);
        }

        Expression<Func<TRoot, bool>> getExpression()
        {
            Type type1 = Metadata.ModelType;
            Type type2 = SourceMeta.RelationModelMeta.ModelType;
            Expression body = null;
            ParameterExpression para = Expression.Parameter(type2);
            if (!string.IsNullOrEmpty(SourceMeta.RelationField))
            {
                Expression member2 = getSource(Expression.Constant(Root));
                if (ManyToMany)
                {
                    ParameterExpression para2 = Expression.Parameter(type1);
                    var method = ExprHelper.GetReferences(type1);
                    var me1 = Expression.Call(para, method, Expression.Constant(SourceMeta.RelationField));
                    Expression anybody = Expression.Lambda(ExprHelper.GetFuncBool(type1), Expression.Equal(getSource(para2), member2), para2);
                    body = Expression.Call(null, ExprHelper.GetAnyMethod(type1), me1, anybody);
                }
                else
                {
                    var member1 = getTarget(para);
                    if (member2.Type != member1.Type)
                    {
                        var mtype1 = TypeHelper.StripType(member1.Type);
                        var mtype2 = TypeHelper.StripType(member2.Type);

                        if (mtype1 == mtype2)
                        {
                            if (member1.Type != mtype1)
                                member2 = Expression.Convert(member2, member1.Type);
                            else
                            {
                                object defvalue = TypeHelper.Default(member1.Type);
                                member2 = Expression.Coalesce(member2, Expression.Constant(defvalue));
                            }
                        }
                        else if ((TypeHelper.IsFloat(mtype1) && TypeHelper.IsNumeric(mtype2)) ||
                            (TypeHelper.IsDigit(mtype1) && TypeHelper.IsDigit(mtype2)))
                        {
                            if (member1.Type != mtype1)
                                member2 = Expression.Convert(member2, member1.Type);
                            else
                            {
                                object defvalue = TypeHelper.Default(member1.Type);
                                member2 = Expression.Coalesce(member2, Expression.Convert(Expression.Constant(defvalue), member1.Type));
                            }
                        }
                        else
                        {
                            object defvalue = TypeHelper.Default(member1.Type);
                            member2 = Expression.Coalesce(member2, Expression.Convert(Expression.Constant(defvalue), member1.Type));
                        }
                    }
                    body = Expression.Equal(member1, member2);
                }
            }
            var spec = SourceMeta.Specific;
            if (spec != null)
            {
                foreach (var parameter in spec.Parameters.Distinct())
                {
                    if (parameter == "MetaName")
                        Variable.CurrentParameters[parameter] = Metadata.Name;
                    else if (parameter.StartsWith("TopObj."))
                    {
                        Variable.CurrentParameters[parameter] = Root.GetDepthValue(parameter);
                    }
                    else if (Metadata.ContainField(parameter))
                        Variable.CurrentParameters[parameter] = Root[Metadata.GetField(parameter).GetFieldname()];


                }
                if (body == null)
                    body = spec.GetExpression(SourceMeta.RelationModelMeta, para).Body;
                else
                    body = Expression.AndAlso(body, spec.GetExpression(SourceMeta.RelationModelMeta, para, new Expression[0]).Body);
            }
            if (body == null)
                return s => true;
            else
                return Expression.Lambda<Func<TRoot, bool>>(body, para);
        }

        /// <summary>
        /// 创建查询
        /// </summary>
        /// <returns></returns>
        public IQueryable<TRoot> CreateQuery()
        {
            return Repository<TRoot>.CreateQuery().Where(getExpression());
        }
        public override IQueryable GetQuery()
        {
            return CreateQuery();
        }
        protected IEnumerable<TRoot> loadedentities;
        protected abstract void OnLoaded();
        protected virtual bool canLoad(bool force)
        {
            if (force)
                return true;
            return IsLoaded ? false : true;
        }
        public sealed override void Load(bool force = false)
        {
            if (canLoad(force))
            {
                if (SourceMeta.RelationModelMeta.GetDomainModel().State == MetaMode.Custom)
                {
                    loadedentities = Repository.FindAll(SourceMeta.RelationModelMeta, getExpression()).Cast<TRoot>();
                }
                else
                    loadedentities = CreateQuery().ToList();
                IsLoaded = true;
                OnLoaded();
            }
        }
    }


    /// <summary>
    /// 根引用
    /// </summary>
    /// <typeparam name="TRoot"></typeparam>
    public class AggregateReference<TRoot> : RelatedEnd<TRoot>
        where TRoot : AggRoot
    {
        public AggregateReference(DomainObject root, string fieldname)
            : base(root, fieldname)
        {
        }
        protected override void OnLoaded()
        {
            if (loadedentities.Count() == 1)
                Value = loadedentities.FirstOrDefault();
        }

        public override void Initialize(IEnumerable<AggRoot> items = null)
        {
            Value = items == null ? null : items.FirstOrDefault() as TRoot;
        }
        internal override void Add2(AggRoot root)
        {
            Value = root as TRoot;
        }
        internal override void Remove2(AggRoot root)
        {
            if (value != null && value.Id == root.Id)
                Value = null;
        }
        TRoot value { get; set; }
        /// <summary>
        /// 引用的值
        /// </summary>
        public TRoot Value
        {
            get
            {
                if (!IsLoaded)
                    Load();
                return value;
            }
            set
            {
                this.value = value;
                IsLoaded = true;
                if (value != null && TargetMeta != null && Root is AggRoot)
                {
                    RelatedEnd tvalue = value[TargetMeta.Name] as RelatedEnd;
                    if (tvalue != null)
                        tvalue.Add2((AggRoot)Root);
                }
                Root[SourceMeta.PrimaryField] = value == null ? TypeHelper.Default(SourceMeta.PrimaryType) : value[SourceMeta.RelationField];
            }
        }
    }

    /// <summary>
    /// 实体集合
    /// </summary>
    /// <typeparam name="TRoot"></typeparam>
    public class AggregateCollection<TRoot> : RelatedEnd<TRoot>, IEnumerable<TRoot>
        where TRoot : AggRoot
    {
        protected Dictionary<Guid, ManyToManyPair> m_waitforadd;
        protected Dictionary<Guid, ManyToManyPair> m_waitforremove;

        public AggregateCollection(AggRoot root, string fieldname)
            : base(root, fieldname)
        {
            if (ManyToMany)
            {
                m_waitforadd = new Dictionary<Guid, ManyToManyPair>();
                m_waitforremove = new Dictionary<Guid, ManyToManyPair>();
            }
        }

        protected override void OnLoaded()
        {
            if (loadedentities != null)
            {
                foreach (var entity in loadedentities)
                    _Add(entity);
            }
        }
        public override void Initialize(IEnumerable<AggRoot> items = null)
        {
            IsLoaded = true;
            if (items != null)
            {
                foreach (TRoot entity in items)
                    _Add(entity);
            }
        }
        /// <summary>
        /// 初始化
        /// </summary>
        public void Initialize(IEnumerable<TRoot> items = null)
        {
            IsLoaded = true;
            if (items != null)
            {
                foreach (var entity in items)
                    _Add(entity);
            }
        }

        Dictionary<Guid, TRoot> RootCollection = new Dictionary<Guid, TRoot>(100);
        protected void AddRoot(TRoot root)
        {
            if (root == null || root.Id == Guid.Empty)
                return;
            RootCollection[root.Id] = root;
        }

        internal override void Add2(AggRoot root)
        {
            AddRoot((TRoot)root);
        }
        internal override void Remove2(AggRoot root)
        {
            if (RootCollection.ContainsKey(root.Id))
                RootCollection.Remove(root.Id);
        }
        #region ICollection 实现
        protected bool _Add(TRoot item)
        {
            AddRoot(item);
            if (!ManyToMany)
            {
                item[SourceMeta.RelationField] = Root[SourceMeta.PrimaryField];
                if (TargetMeta != null && item.Metadata.GetDomainModel().State == MetaMode.Base)
                    item[TargetMeta.Name] = Root;
            }
            else
            {
                if (TargetMeta != null)
                {
                    var related = item[TargetMeta.Name] as RelatedEnd;
                    if (related != null)
                    {
                        related.Add2((AggRoot)Root);
                    }
                }
                AddM2M(item.Id);
            }
            return true;
        }

        public void Add(TRoot item)
        {
            _Add(item);
            AddM2M(item.Id);
        }

        /// <summary>
        /// 添加集合
        /// </summary>
        /// <param name="items"></param>
        public void AddRange(IEnumerable<TRoot> items)
        {
            if (items != null)
                foreach (var item in items)
                    Add(item);
        }

        public bool Remove(TRoot item)
        {
            RemoveM2M(item.Id);
            if (RootCollection.ContainsKey(item.Id))
            {
                bool removed = RootCollection.Remove(item.Id);
                if (ManyToMany)
                {
                    if (TargetMeta != null)
                    {
                        var related = item[TargetMeta.Name] as RelatedEnd;
                        if (related != null)
                        {
                            related.Remove2((AggRoot)Root);
                        }
                    }
                }
                return removed;
            }
            return false;
        }

        public void RemoveAll(Func<TRoot, bool> expression)
        {
            foreach (var item in RootCollection.Values.Where(expression).ToList())
                Remove(item);
        }
        public void Clear()
        {
            foreach (var item in RootCollection.Values.ToList())
                Remove(item);
        }

        public int Count
        {
            get
            {
                return RootCollection.Count;
            }
        }

        public IEnumerator<TRoot> GetEnumerator()
        {
            if (!IsLoaded)
                Load();
            return RootCollection.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        #endregion

        #region 多对多处理
        HashSet<Guid> m_m2mkeys;
        /// <summary>
        /// 本关系的多对多键集合
        /// </summary>
        protected HashSet<Guid> M2MKeys
        {
            get
            {
                if (m_m2mkeys == null)
                {
                    var m2mkeys = ((AggRoot)Root).M2mKeys;
                    HashSet<Guid> keys = null;
                    if (!m2mkeys.ContainsKey(SourceMeta.Name))
                        keys = m2mkeys[SourceMeta.Name] = new HashSet<Guid>();
                    else
                        keys = m2mkeys[SourceMeta.Name];
                    m_m2mkeys = keys;
                }
                return m_m2mkeys;
            }
        }
        /// <summary>
        /// 为多对多关系添加一项
        /// </summary>
        /// <param name="itemid"></param>
        public void AddM2M(Guid itemid)
        {
            if (ManyToMany && !M2MKeys.Contains(itemid))
            {
                m_waitforadd[itemid] = GetM2MPair(itemid);
                M2MKeys.Add(itemid);
            }
        }
        /// <summary>
        /// 删除多对多关系用的一项
        /// </summary>
        /// <param name="itemid"></param>
        public void RemoveM2M(Guid itemid)
        {
            if (ManyToMany)
            {
                if (m_waitforadd.ContainsKey(itemid))
                    m_waitforadd.Remove(itemid);
                else
                    m_waitforremove[itemid] = GetM2MPair(itemid);
                if (M2MKeys.Contains(itemid))
                    M2MKeys.Remove(itemid);
            }
        }

        /// <summary>
        /// 同步多对多关系数据
        /// </summary>
        /// <param name="itemids"></param>
        public void SyncM2M(IEnumerable<Guid> itemids)
        {
            HashSet<Guid> tmp = new HashSet<Guid>(itemids ?? new Guid[0]);
            foreach (var id in M2MKeys.ToArray())
            {
                if (!tmp.Contains(id))
                {
                    RemoveM2M(id);
                }
            }
            foreach (var id in tmp)
            {
                if (!M2MKeys.Contains(id))
                    AddM2M(id);
            }

        }

        protected ManyToManyPair GetM2MPair(Guid itemid)
        {
            var m2m = SourceMeta as Many2ManyField;
            string role1name = m2m.MiddleField;
            string role2name = m2m.TargetMiddleField;
            var value1 = (Guid)Root["Id"];
            return new ManyToManyPair() { Key1 = role1name, Value1 = value1, Key2 = role2name, Value2 = itemid };
        }

        /// <summary>
        /// 获取多对多关系的处理
        /// </summary>
        /// <returns></returns>
        public ManyToManyTable GetManyToMany()
        {
            if (!ManyToMany)
                return null;
            var m2m = SourceMeta as Many2ManyField;
            return new ManyToManyTable()
            {
                TableName = m2m.MiddleTable,
                ForAdd = new List<ManyToManyPair>(m_waitforadd.Values),
                ForRemove = new List<ManyToManyPair>(m_waitforremove.Values)
            };
        }
        #endregion
    }
}
