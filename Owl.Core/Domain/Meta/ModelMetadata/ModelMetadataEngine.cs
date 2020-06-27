using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Domain;
using Owl.Util;
namespace Owl.Domain
{
    public class ModelRelation
    {
        public string Relation { get; set; }
        public string Primary { get; set; }
        public ModelMetadata RelationMeta { get; set; }
        public RelationMode RelationMode { get; set; }

        public ModelRelation(string relation, string primary, ModelMetadata meta, RelationMode mode)
        {
            Relation = relation;
            Primary = primary;
            RelationMeta = meta;
            RelationMode = mode;
        }
    }

    public class ModelMetadataEngine
    {
        static Dictionary<string, ModelMetadata> m_metadatas = new Dictionary<string, ModelMetadata>(150);

        static ModelMetadata ResoleModel(DomainModel model)
        {
            ModelMetadata metadata = new ModelMetadata(model);
            m_metadatas[metadata.Name] = metadata;
            metadata.onModelChange += new EventHandler<MetaChangeArgs>(metadata_onModelChange);
            return metadata;
        }

        static void metadata_onModelChange(object sender, MetaChangeArgs e)
        {
            var meta = sender as ModelMetadata;
            if (e.Mode == ChangeMode.Remove && m_metadatas.ContainsKey(meta.Name))
                m_metadatas.Remove(meta.Name);
        }

        /// <summary>
        /// 获取模型
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static ModelMetadata GetModel(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;
            name = name.ToLower();
            if (m_metadatas.ContainsKey(name))
                return m_metadatas[name];
            var model = MetaEngine.GetModel(name);
            if (model != null)
                return ResoleModel(model);
            return null;
        }
        /// <summary>
        /// 根据 通用元数据获取 对象元数据
        /// </summary>
        /// <param name="meta"></param>
        /// <returns></returns>
        public static ModelMetadata GetModel(DomainModel meta)
        {
            if (DomainModel.GetModel(meta.Name) == null)
                DomainModel.RegisterMeta(meta);
            if (m_metadatas.ContainsKey(meta.Name))
                return m_metadatas[meta.Name];
            return ResoleModel(meta);
        }

        /// <summary>
        /// 获取模型元数据
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static ModelMetadata GetModel(Type type)
        {
            var name = type.MetaName();
            if (!m_metadatas.ContainsKey(name))
            {
                var meta = MetaEngine.GetModel(type);
                if (meta != null)
                    return ResoleModel(meta);
                return null;
            }
            else
                return m_metadatas[name];
        }

        /// <summary>
        /// 获取关系
        /// </summary>
        /// <param name="modelname"></param>
        /// <returns></returns>
        public static IEnumerable<ModelRelation> GetRelations(string modelname)
        {
            List<ModelRelation> results = new List<ModelRelation>();
            string name = modelname.ToLower();
            var modelmeta = GetModel(name);
            foreach (var field in modelmeta.GetFields<One2ManyField>(s => !s.IsEntityCollection))
            {
                if (string.IsNullOrEmpty(field.RelationField))
                    continue;
                results.Add(new ModelRelation(field.RelationField, field.PrimaryField, field.RelationModelMeta, field.RelationMode));
            }
            var entitities = modelmeta.GetEntityRelated();
            List<FieldMetadata> fields = new List<FieldMetadata>();
            Dictionary<string, ModelMetadata> metas = new Dictionary<string, ModelMetadata>();
            foreach (var model in MetaEngine.GetModels().Where(s => s.ObjType == DomainType.AggRoot && (s.ModelType == null || !s.ModelType.IsAbstract)))
            {
                var dfields = model.GetFields(s => s.Field_Type == FieldType.many2one && !s.UnInjectRelation && !s.Multiple && s.RelationModel.ToLower() == name).ToList();
                if (dfields.Count == 0)
                    continue;
                var meta = GetModel(model.Name);
                if (meta.GetDomainModel().NoTable || meta.GetDomainModel().IsObjectTemplate)
                    continue;
                foreach (var field in dfields)
                {
                    var mfield = meta.GetField(field.Name) as Many2OneField;
                    if (entitities.Any(s => s.RelationField == mfield.GetFieldname() && s.RelationModel == mfield.Metadata.Name))
                        continue;
                    if (results.Any(s => s.Relation == mfield.GetFieldname() && s.Primary == mfield.RelationField && s.RelationMeta.Name == mfield.Metadata.Name))
                        continue;
                    results.Add(new ModelRelation(mfield.GetFieldname(), mfield.RelationField, meta, RelationMode.General));
                }
            }
            return results;
        }
    }
}
