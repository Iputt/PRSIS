using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Policy;

namespace Owl.Domain
{
    /// <summary>
    /// 功能型值对象，为行为提供数据的值对象
    /// </summary>
    public abstract class BehaviorObject : DomainObject
    {
        /// <summary>
        /// 本行为对象的主体对象
        /// </summary>
        /// <value>The source model.</value>
        public string SourceModel { get; set; }

        #region load default
        /// <summary>
        /// 操作关联对象的ID
        /// </summary>
        protected Guid[] ObjectKeys { get; set; }

        /// <summary>
        /// Gets the root.
        /// </summary>
        /// <returns>The root.</returns>
        /// <param name="selector">Selector.</param>
        public AggRoot GetRoot(params string[] selector)
        {
            return Repository.FindById(SourceModel, ObjectKeys.FirstOrDefault(), selector);
        }
        /// <summary>
        /// Gets the root.
        /// </summary>
        /// <returns>The root.</returns>
        /// <param name="selector">Selector.</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public T GetRoot<T>(params string[] selector)
            where T : AggRoot
        {
            return Repository<T>.FindById(ObjectKeys.FirstOrDefault(), selector);
        }
        /// <summary>
        /// Gets the roots.
        /// </summary>
        /// <returns>The root.</returns>
        /// <param name="selector">Selector.</param>
        public IEnumerable<AggRoot> GetRoots(params string[] selector)
        {
            var meta = ModelMetadataEngine.GetModel(SourceModel);
            var spec = Specification.Create("Id", CmpCode.IN, ObjectKeys).GetExpression(meta);
            return Repository.FindAll(meta, spec, null, 0, 0, selector);
        }

        public IEnumerable<T> GetRoots<T>(params string[] selector)
            where T : AggRoot
        {
            return Repository<T>.Where(s => s.Id.In(ObjectKeys)).Select(selector).ToList();
        }

        public TransferObject GetRto(params string[] selector)
        {
            return Repository.Read(SourceModel, new Guid[] { ObjectKeys.FirstOrDefault() }, true, selector).FirstOrDefault();
        }
        public IEnumerable<TransferObject> GetRtos(params string[] selector)
        {
            return Repository.Read(SourceModel, ObjectKeys, true, selector);
        }

        protected TransferObject defaultvalues;
        /// <summary>
        /// 获取默认值
        /// </summary>
        /// <param name="id">关联对象的Id</param>
        /// <returns></returns>
        public TransferObject GetDefault(params Guid[] id)
        {
            ObjectKeys = id;
            defaultvalues = new TransferObject();
            OnLoad();
            BuildDefault();
            return defaultvalues;
        }
        #region 过时的方法
        [Obsolete("本方法即将废弃,请用 AddDefault 代替", false)]
        public void AppendResult(string key, object value)
        {
            defaultvalues[key] = value;
        }
        [Obsolete("本方法即将废弃,请用 AddDefault 代替", false)]
        public void AppendResult(TransferObject dto)
        {
            foreach (var pair in dto)
            {
                defaultvalues[pair.Key] = pair.Value;
            }
        }

        [Obsolete("本方法即将废弃,请用 BuildDefault 代替", false)]
        protected virtual void OnLoad()
        {

        }
        #endregion


        /// <summary>
        /// 添加字段默认值
        /// </summary>
        /// <param name="field">字段名称</param>
        /// <param name="value">默认值</param>
        public void AddDefault(string field, object value)
        {
            defaultvalues[field] = value;
        }
        /// <summary>
        /// 添加默认值
        /// </summary>
        /// <param name="dto"></param>
        public void AddDefault(TransferObject dto)
        {
            foreach (var pair in dto)
            {
                defaultvalues[pair.Key] = pair.Value;
            }
        }
        /// <summary>
        /// 构建默认值，继承方法中可通过 AddDefault添加默认值
        /// </summary>
        protected virtual void BuildDefault()
        {

        }

        
        #endregion

        

        public sealed override void Write(Object2 dto)
        {
            base.Write(dto);
            SourceModel = (string)dto["outermodel"];
            _Write(dto.Read());
        }
        protected virtual void _Write(TransferObject dto) { }
    }
}
