using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Collections;

namespace Owl.Domain
{

    /// <summary>
    /// 跟踪器
    /// </summary>
    public interface IDataTracer
    {
        /// <summary>
        /// 原始数据
        /// </summary>
        TransferObject Original { get; set; }

        /// <summary>
        /// 是否已加载
        /// </summary>
        bool IsLoaded { get; }
    }

    /// <summary>
    /// 可持久化的领域对象基类
    /// </summary>
    public abstract class PersistObject : DomainObject, IDataTracer
    {
        #region
        TransferObject m_orginal;
        [IgnoreField]
        TransferObject IDataTracer.Original
        {
            get
            {
                if (m_orginal == null)
                    m_orginal = new TransferObject();
                return m_orginal;
            }
            set { m_orginal = value; }
        }

        /// <summary>
        /// 是否已加载
        /// </summary>
        [IgnoreField]
        public bool IsLoaded
        {
            get
            {
                IDataTracer tracer = (IDataTracer)this;
                return tracer.Original != null && tracer.Original.Count > 0;
            }
        }

        /// <summary>
        /// 最近一次变更集
        /// </summary>
        [IgnoreField]
        internal ModelChange LastChanges { get; set; }
        #endregion

        /// <summary>
        /// 从数据库的结果初始化对象
        /// </summary>
        /// <param name="dto"></param>
        public virtual void FromDb(TransferObject dto)
        {
            Dictionary<string, object> dbdict = new Dictionary<string, object>();
            foreach (var pair in dto)
            {
                if (Instance.ContainsKey(pair.Key))
                    dbdict[pair.Key] = pair.Value;
                else
                    InnerDict[pair.Key] = pair.Value;
            }
            Instance.ParsefromDb(this, dbdict);
            ((IDataTracer)this).Original = dto;
            foreach (var navfield in Metadata.GetEntityRelated())
            {
                if (!dto.ContainsKey(navfield.Name))
                    continue;
                var v = dto[navfield.Name];
                dynamic mv = this[navfield.Name];
                foreach (TransferObject d in (IEnumerable)v)
                {
                    var eobj = DomainFactory.Create<PersistObject>(navfield.RelationModelMeta);
                    eobj.FromDb(d);
                    mv.Add((dynamic)eobj);
                }
            }
        }
    }

}
