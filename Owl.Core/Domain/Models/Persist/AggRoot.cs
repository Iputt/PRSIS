using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Linq.Expressions;
using System.Diagnostics;
using System.Reflection;
using System.Collections;
using Owl.Util;
namespace Owl.Domain
{
    /// <summary>
    /// 聚合根基类
    /// </summary>
    public abstract class AggRoot : Entity, IRoot
    {
        /// <summary>
        /// 创建时间
        /// </summary>
        [DomainField(FieldType.datetime, Label = "创建时间", IgnoreLogModify = true, Resource = "fieldlabel.common.created")]
        public virtual DateTime? Created { get; set; }
        /// <summary>
        /// 创建者
        /// </summary>
        [DomainField(IgnoreLogModify = true, Label = "创建人", Resource = "fieldlabel.common.createdby")]
        public virtual string CreatedBy { get; set; }
        /// <summary>
        /// 修改时间
        /// </summary>
        [DomainField(FieldType.datetime, Label = "最近操作", Resource = "fieldlabel.common.modified", IgnoreLogModify = true)]
        public virtual DateTime? Modified { get; set; }

        /// <summary>
        /// 修改者
        /// </summary>
        [DomainField(Label = "操作者", Resource = "fieldlabel.common.modifiedby", IgnoreLogModify = true)]
        public virtual string ModifiedBy { get; set; } 
        /// <summary>
        /// 申请编号，在某些情况下就是内编码
        /// </summary>
        [DomainField(Label = "编号", AutoSearch = true, Readonly = ReadOnlyType.Force, Resource = "fieldlabel.common.rpwfroot.number")]
        public virtual string Number { get; set; }
        /// <summary>
        /// 外部编码
        /// </summary>
        [DomainField(Label = "外部编码", Resource = "fieldlabel.common.rpbaseroot.code")]
        public virtual string Code { get; set; }

        /// <summary>
        /// 获取编码
        /// </summary>
        /// <returns></returns>
        public override string GetNumber()
        {
            return Code != null ? Code.ToString() : "";
        }
        #region 引用相关
        /// <summary>
        /// 获取当前字段的聚合根引用集合
        /// </summary>
        /// <typeparam name="TRoot"></typeparam>
        /// <returns></returns>
        protected AggregateCollection<TRoot> GetReferences<TRoot>()
            where TRoot : AggRoot
        {
            return GetReferences<TRoot>(GetPropertyName());
        }

        /// <summary>
        /// 获取本聚合根的所属实体集合
        /// </summary>
        /// <typeparam name="TRoot"></typeparam>
        /// <param name="field"></param>
        /// <returns></returns>
        protected AggregateCollection<TRoot> GetReferences<TRoot>(string field)
            where TRoot : AggRoot
        {
            if (!m_Relateds.ContainsKey(field))
                m_Relateds[field] = new AggregateCollection<TRoot>(this, field);
            return (AggregateCollection<TRoot>)m_Relateds[field];
        }

        protected override object GetRelation(NavigatField field)
        {
            if ((field.Field_Type == FieldType.one2many && !field.IsEntityCollection) || field.Field_Type == FieldType.many2many)
                return Util.ExprHelper.GetReferences(field.RelationModelMeta.ModelType).FaseInvoke(this, field.Name);
            return base.GetRelation(field);
        }

        #endregion

        Dictionary<string, HashSet<Guid>> m2mkeys;
        /// <summary>
        /// 多对多字段的key集合
        /// </summary>
        public Dictionary<string, HashSet<Guid>> M2mKeys
        {
            get
            {
                if (m2mkeys == null)
                    m2mkeys = new Dictionary<string, HashSet<Guid>>();
                return m2mkeys;
            }
        }

        internal override void SyncTime(string modifiedby = null, DateTime? time = null)
        {
            if (time.HasValue)
            {
                if (Created == null)
                {
                    CreatedBy = modifiedby;
                    Created = time;
                }
                ModifiedBy = modifiedby;
                Modified = time;
            }
            else
            {
                modifiedby = ModifiedBy;
                time = Modified;
            }
            base.SyncTime(modifiedby, time);
        }

        /// <summary>
        /// 过滤用于跨表查询
        /// </summary>
        /// <typeparam name="TRoot"></typeparam>
        /// <param name="expression"></param>
        /// <returns></returns>
        public bool Filter<TRoot>(Expression<Func<TRoot, bool>> expression)
            where TRoot : AggRoot
        {
            return Repository<TRoot>.Exists(expression);
        }
        protected override TransferObject _Read(bool fordisplay)
        {
            var dto = base._Read(fordisplay);
            if (m2mkeys != null)
                dto["M2mKeys"] = m2mkeys;
            return dto;
        }
        public override void Write(Object2 dto)
        {
            base.Write(dto);
            TransferObject ckeys = null;
            if (dto is AggRoot)
            {
                if ((dto as AggRoot).m2mkeys != null)
                    ckeys = new TransferObject((dto as AggRoot).m2mkeys);
            }
            else if (dto.ContainsKey("M2mKeys") && (dto["M2mKeys"] is IDictionary || dto["M2mKeys"] is TransferObject))
            {
                var tkeys = dto["M2mKeys"];
                if (tkeys is TransferObject)
                    ckeys = tkeys as TransferObject;
                else
                    ckeys = new TransferObject(tkeys as IDictionary);
            }
            if (ckeys != null)
            {
                foreach (var key in ckeys.Keys)
                {
                    dynamic end = this[key];
                    if (end != null)
                    {
                        var m2m = new List<Guid>();
                        foreach (var obj in (IEnumerable)ckeys[key])
                        {
                            m2m.Add(Convert2.ChangeType<Guid>(obj));
                        }
                        end.SyncM2M(m2m);
                    }
                }
            }
        }
        string m_key;
        /// <summary>
        /// 聚合根的唯一键
        /// </summary>
        [IgnoreField]
        public string RootKey
        {
            get
            {
                if (m_key == null)
                    m_key = string.Format("{0}:{1}", Metadata.Name, Id);
                return m_key;
            }
        }

        protected string MetaName { get { return Metadata.Name; } }
        public virtual bool Valid
        {
            get { return true; }
            set { }
        }

        public T Cast<T>()
            where T : AggRoot
        {
            T t = DomainFactory.Create<T>(Read());
            ((IDataTracer)t).Original = new TransferObject((this as IDataTracer).Original);
            return t;
        }

        /// <summary>
        /// 将对象送入上下文中以进行更新或添加
        /// </summary>
        public void Push()
        {
            DomainContext.Current.Phsh(this);
        }

        /// <summary>
        /// 通知上下文删除本对象
        /// </summary>
        public void Remove()
        {
            if (this.ContainsKey("Deleted"))
            {
                this["Deleted"] = true;
                this.Push();
            }
            else
                DomainContext.Current.Remove(this);
        }
        /// <summary>
        /// 判断对象是否将要删除
        /// </summary>
        public bool Removing()
        {
            return DomainContext.Current.InRemove(this);
        }
    }

    public class AggRootEventHandler : RootEventHandler<AggRoot>
    {
        protected override bool CanHandle(string modelname)
        {
            var metadata = ModelMetadata.GetModel(modelname);
            if (metadata != null)
            {
                foreach (var field in metadata.GetEntityRelated())
                {
                    if (field.GetDomainField().MinLength > 0)
                        return true;
                    if (field.RelationModelMeta.ModelType.IsSubclassOf(typeof(OrderedEntity)))
                        return true;
                }
            }
            return false;
        }
        /// <summary>
        /// 处理行项目
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="field"></param>
        protected virtual void HandleEntityfield(Entity obj, One2ManyField field)
        {
            var entities = obj[field.Name] as EntityCollection;
            if (field.GetDomainField().MinLength > 0 && entities.Count < field.GetDomainField().MinLength)
                throw new AlertException("error.common.object.entity.loss", "行项目 {0} 数量不可少于{1}！", field.Name, field.GetDomainField().MinLength);
            if (entities.Count > 0)
            {
                int maxno = 0;
                var isordered = field.RelationModelMeta.ModelType.IsSubclassOf(typeof(OrderedEntity));
                if (isordered)
                {
                    maxno = entities.Cast<OrderedEntity>().Max(s => s.ItemNo ?? 0);
                }
                foreach (Entity line in entities)
                {
                    if (isordered)
                    {
                        var tmp = line as OrderedEntity;
                        if ((tmp.ItemNo ?? 0) == 0)
                        {
                            maxno = maxno + 10;
                            tmp.ItemNo = maxno;
                        }
                    }
                    HandleObject(line);
                }
            }
        }
        /// <summary>
        /// 处理对象
        /// </summary>
        /// <param name="obj"></param>
        void HandleObject(Entity obj)
        {
            foreach (var field in obj.Metadata.GetEntityRelated())
            {
                HandleEntityfield(obj, field);
            }
        }

        protected override void OnRootAdding(AggRoot root)
        {
            base.OnRootAdding(root);
            HandleObject(root);
        }
        protected override void OnRootUpdating(AggRoot root)
        {
            base.OnRootUpdating(root);
            HandleObject(root);
        }
        protected override void OnRootRemoveFailed(AggRoot root)
        {
            base.OnRootRemoveFailed(root);
        }
    }

    /// <summary>
    /// 聚合根范型基类
    /// </summary>
    /// <typeparam name="TRoot"></typeparam>
    public abstract class AggRoot<TRoot> : AggRoot
        where TRoot : AggRoot<TRoot>
    {
        static ModelMetadata metadata = ModelMetadataEngine.GetModel(typeof(TRoot));
        protected override ModelMetadata GetMeta()
        {
            return metadata;
        }

        /// <summary>
        /// 根据Id获取对象
        /// </summary>
        /// <param name="Id"></param>
        /// <param name="selector"></param>
        /// <returns></returns>
        public static TRoot Get(Guid Id, params string[] selector)
        {
            return Repository<TRoot>.FindById(Id, selector);
        }
        /// <summary>
        /// 查询数据
        /// </summary>
        /// <param name="exp"></param>
        /// <returns></returns>
        public static IQueryable<TRoot> All(Expression<Func<TRoot, bool>> exp = null)
        {
            return Repository<TRoot>.Where(exp);
        }

        /// <summary>
        /// 是否存在符合条件的数据
        /// </summary>
        /// <param name="exp"></param>
        /// <returns></returns>
        public static bool Exists(Expression<Func<TRoot, bool>> exp)
        {
            return Repository<TRoot>.Exists(exp);
        }
    }

    /// <summary>
    /// 空白根 不包含 Id等字段
    /// </summary>
    public abstract class ClearRoot : AggRoot
    {
        public override Guid Id
        {
            get
            {
                return base.Id;
            }
            set
            {
                base.Id = value;
            }
        }
        public override DateTime? Created
        {
            get
            {
                return base.Created;
            }
            set
            {
                base.Created = value;
            }
        }
        public override string CreatedBy
        {
            get
            {
                return base.CreatedBy;
            }
            set
            {
                base.CreatedBy = value;
            }
        }
        public override DateTime? Modified
        {
            get
            {
                return base.Modified;
            }
            set
            {
                base.Modified = value;
            }
        }
        public override string ModifiedBy
        {
            get
            {
                return base.ModifiedBy;
            }
            set
            {
                base.ModifiedBy = value;
            }
        }
    }

    public interface IExtraRoot<TRoot>
        where TRoot : AggRoot
    {

    }
}
