using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Owl.Common;

namespace Owl.Domain
{
    /// <summary>
    /// 模型元数据
    /// </summary>
    public class ModelMetadata
    {
        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get { return m_metadata.Name; } }
        /// <summary>
        /// 
        /// </summary>
        public string Label { get { return m_metadata.GetLabel(); } }
        /// <summary>
        /// 数据表名称
        /// </summary>
        public string TableName { get { return m_metadata.TableName; } }

        /// <summary>
        /// 是否要缓存数据
        /// </summary>
        public bool IsCacheEnable { get { return m_metadata.IsCache; } }

        /// <summary>
        /// 实体的类型
        /// </summary>
        public Type ModelType { get { return m_metadata.ModelType; } }

        /// <summary>
        /// 领域对象类型
        /// </summary>
        public DomainType ObjType { get { return m_metadata.ObjType; } }

        /// <summary>
        /// 维护修改日志
        /// </summary>
        public bool LogModify { get { return m_metadata.LogModify; } }

        /// <summary>
        /// 索引方法
        /// </summary>
        public MethodInfo GetItem { get { return m_metadata.GetItem; } }

        public string SortBy { get { return m_metadata.SortBy; } }

        public SortOrder SortOrder { get { return m_metadata.SortOrder; } }

        /// <summary>
        /// 获取指定的元数据扩展
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetExtension<T>()
            where T : MetaExtension
        {
            return m_metadata.GetExtension<T>();
        }

        #region
        /// <summary>
        /// 字段
        /// </summary>
        protected IDictionary<string, FieldMetadata> Fields { get; private set; }

        FieldMetadata primaryfield;
        bool primarycomplete = false;
        /// <summary>
        /// 对象的主键
        /// </summary>
        public FieldMetadata PrimaryField
        {
            get
            {
                if (!primarycomplete)
                {
                    primarycomplete = true;
                    if (Fields.ContainsKey("Id"))
                        primaryfield = Fields["Id"];
                }
                return primaryfield;
            }
        }
        /// <summary>
        /// 获取所有字段名称
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetFieldNames()
        {
            return Fields.Keys;
        }
        /// <summary>
        /// 是否包含字段
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool ContainField(string name)
        {
            return Fields.ContainsKey(name);
        }
        /// <summary>
        /// 根据名称获取字段
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public FieldMetadata GetField(string name)
        {
            if (Fields.ContainsKey(name))
                return Fields[name];
            return Fields.Values.FirstOrDefault(s => s.GetFieldname() == name);
        }

        protected FieldMetadata GetField(ModelMetadata metadata, string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;
            var fields = name.Split(new char[] { '.' }, 2);
            var fieldmeta = metadata.GetField(fields[0]);
            if (fieldmeta == null)
                return null;
            if (fields.Length == 2)
            {
                return GetField((fieldmeta as NavigatField).RelationModelMeta, fields[1]);
            }
            return fieldmeta;
        }
        /// <summary>
        /// 获取对象的字段
        /// </summary>
        /// <param name="name"></param>
        /// <param name="prefix">关联字段的前置信息</param>
        /// <returns></returns>
        public FieldMetadata GetField(string name, out string prefix)
        {
            var lastindex = name.LastIndexOf('.');
            if (lastindex > 0)
                prefix = name.Substring(0, lastindex);
            else
                prefix = "";
            return GetField(this, name);
        }

        /// <summary>
        ///获取所有符合条件的类型为T的
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filter"></param>
        /// <returns></returns>
        public IEnumerable<T> GetFields<T>(Func<T, bool> filter = null)
            where T : FieldMetadata
        {
            var values = Fields.Values.Where(s => s is T).Cast<T>();
            if (filter != null)
                values = values.Where(filter);
            return values.ToList();
        }

        /// <summary>
        /// 获取所有符合条件的字段
        /// </summary>
        /// <returns></returns>
        public IEnumerable<FieldMetadata> GetFields(Func<FieldMetadata, bool> filter = null)
        {
            var values = Fields.Values.AsEnumerable();
            if (filter != null)
                values = values.Where(filter);
            return values.ToList();
        }
        /// <summary>
        /// 包含的字段数量
        /// </summary>
        public int FieldCount
        {
            get { return Fields.Count; }
        }
        #endregion
        /// <summary>
        /// 基础模型元数据
        /// </summary>
        protected DomainModel m_metadata;

        public DomainModel GetDomainModel()
        {
            return m_metadata;
        }
        /// <summary>
        /// 领域模型元数据
        /// </summary>
        /// <param name="metadata">基础模型元数据</param>
        public ModelMetadata(DomainModel metadata)
        {
            if (metadata == null)
                throw new ArgumentNullException("metadata");
            Initial(metadata);
        }
        public event EventHandler<MetaChangeArgs> onModelChange;

        public void Initial(DomainModel metadata)
        {
            m_metadata = metadata;
            m_metadata.onModelRemove += new EventHandler<ModelChangeArgs>(m_metadata_onModelRemove);
            m_metadata.onFieldChange += new EventHandler<FieldChangeArgs>(m_metadata_onFieldChange);
            m_metadata.onFieldRemove += new EventHandler<FieldChangeArgs>(m_metadata_onFieldRemove);
            Fields = new OrderlyDict<string, FieldMetadata>((int)(m_metadata.FieldCount * 1.2));
            foreach (var field in metadata.GetFields())
            {
                resolevfield(field);
            }
        }

        void m_metadata_onModelRemove(object sender, ModelChangeArgs e)
        {
            if (onModelChange != null)
                onModelChange(this, new MetaChangeArgs(ChangeMode.Remove));
        }

        void m_metadata_onFieldRemove(object sender, FieldChangeArgs e)
        {
            Fields.Remove(e.Field.Name);
            if (onModelChange != null)
                onModelChange(this, new MetaChangeArgs(ChangeMode.Update));
        }

        void m_metadata_onFieldChange(object sender, FieldChangeArgs e)
        {
            resolevfield(e.Field);
            if (onModelChange != null)
                onModelChange(this, new MetaChangeArgs(ChangeMode.Update));
        }
        void resolevfield(DomainField metadata)
        {
            FieldMetadata field = null;
            if (Fields.ContainsKey(metadata.Name))
            {
                field = Fields[metadata.Name];
                field.UpdateDomainMeta(metadata);
                if (!field.IsMatch)
                    Fields.Remove(metadata.Name);
            }
            if (!Fields.ContainsKey(metadata.Name))
            {
                field = FieldMetadata.Create(this, metadata);
                Fields[metadata.Name] = field;
            }


            //if (field is EmbedField)
            //{
            //    foreach (var efield in ((EmbedField)field).EmbedMetadata.Fields.Values)
            //    {
            //        EmbedFields[string.Format("{0}_{1}", field.GetFieldname(), efield.GetFieldname())] = (EmbedField)field;
            //    }
            //}
        }
        public void Validate(Object2 dto)
        {
            foreach (var field in GetFields())
            {
                if (field.Name == "Id")
                    continue;
                if (dto is PersistObject)
                {
                    var pobj = dto as PersistObject;
                    if (pobj.IsLoaded && !pobj.IsChange(field.Name))
                        continue;
                }
                field.Validate(dto.GetRealValue(field.GetFieldname()), dto);
            }
        }
        public IEnumerable<One2ManyField> GetEntityRelated()
        {
            return Fields.Values.OfType<One2ManyField>().Where(s => s.IsEntityCollection);// s.RelationModelMeta.ObjType == DomainType.Entity);
        }
        /// <summary>
        /// 获取下属引用对象的路径
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public FieldMetadata GetEntityField(Type type)
        {
            foreach (var field in GetEntityRelated())
            {
                if (field.RelationType == type)
                    return field;
                var rfield = field.RelationModelMeta.GetEntityField(type);
                if (rfield != null)
                    return rfield;
            }
            return null;
        }
        public static ModelMetadata GetModel(string name)
        {
            return ModelMetadataEngine.GetModel(name);
        }
        public static ModelMetadata GetModel(Type type)
        {
            return ModelMetadataEngine.GetModel(type);
        }
    }
}
