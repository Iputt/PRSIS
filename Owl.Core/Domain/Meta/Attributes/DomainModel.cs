using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Owl.Util;
using Owl.Common;

namespace Owl.Domain
{
    /// <summary>
    /// 字段变化参数
    /// </summary>
    public class FieldChangeArgs : EventArgs
    {
        /// <summary>
        /// 发生变化的字段
        /// </summary>
        public DomainField Field { get; set; }

        public FieldChangeArgs(DomainField field)
        {
            Field = field;
        }
    }

    public class MetaChangeArgs : EventArgs
    {
        public ChangeMode Mode { get; private set; }

        public MetaChangeArgs(ChangeMode mode)
        {
            Mode = mode;
        }
    }

    public class ModelChangeArgs : EventArgs
    {
        public DomainModel Model { get; set; }
        public ModelChangeArgs(DomainModel model)
        {
            Model = model;
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true)]
    public abstract class MetaExtension : Attribute
    {
        /// <summary>
        /// 关联的对象元数据
        /// </summary>
        public DomainModel ModelMeta { get; internal set; }
    }
    /// <summary>
    /// 元数据注入，用于对基类进行扩展
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public abstract class MetaInject : Attribute
    {
        List<DomainField> Fields = new List<DomainField>();

        protected void InjectField(DomainField field)
        {
            Fields.Add(field);
        }

        /// <summary>
        /// 注入字段
        /// </summary>
        protected abstract void InjectFields();

        public MetaInject()
        {
            InjectFields();
        }

        /// <summary>
        /// 获取要注入的字段
        /// </summary>
        /// <returns></returns>
        public IEnumerable<DomainField> GetInjects()
        {
            return Fields.AsReadOnly();
        }

    }

    /// <summary>
    /// 模型特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class DomainModel : Attribute
    {
        #region 解析
        static Dictionary<string, List<Action<DomainModel>>> actions = new Dictionary<string, List<Action<DomainModel>>>();
        static Dictionary<string, DomainModel> models = new Dictionary<string, DomainModel>();
        public static void FixRelation(DomainField field)
        {
            if (models.ContainsKey(field.RelationModel))
            {
                doFix(field, models[field.RelationModel]);
            }
            else
            {
                if (!actions.ContainsKey(field.RelationModel))
                    actions[field.RelationModel] = new List<Action<DomainModel>>();
                actions[field.RelationModel].Add(s => doFix(field, s));
            }
        }
        static void doFix(DomainField field, DomainModel relationmetadata)
        {
            if (relationmetadata == null)
                return;
            if (field.PropertyType == null)
            {
                Type propertytype = null;
                if (field.Field_Type == FieldType.many2one && field.Name == field.PrimaryField)
                {
                    var rfield = relationmetadata.GetField(field.RelationField);
                    if (rfield == null)
                        throw new Exception2("关系{0}-{1}有误，请检查", field.Model.Name, field.Name);
                    propertytype = Nullable.GetUnderlyingType(rfield.PropertyType) ?? rfield.PropertyType;
                    if (!field.Required && propertytype.IsValueType)
                        propertytype = typeof(Nullable<>).MakeGenericType(propertytype);
                }
                else
                    propertytype = DomainHelper.GetType(field.Field_Type, 0, relationmetadata, field.IsEntityCollection);
                field.SetType(propertytype, false);
            }
            if (field.Field_Type == FieldType.many2many)
            {
                if (field.PropertyInfo == null || field.PropertyInfo.DeclaringType.IsAbstract || field.PropertyInfo.DeclaringType == field.Model.ModelType)
                {
                    //补全关系字段
                    if (!relationmetadata.ContainField(field.RelationField))
                    {
                        var dfield = new DomainField(FieldType.many2many, field.Model.ModelType, field.Name, "");
                        dfield.NonPublic = false;
                        dfield.IsInject = true;
                        dfield.Model = relationmetadata;
                        dfield.MiddleTable = field.MiddleTable;
                        dfield.SetName(field.RelationField);
                        dfield.SetType(typeof(AggregateCollection<>).MakeGenericType(field.Model.ModelType), false);
                        relationmetadata.SetField(dfield);
                    }
                    #region 补全中间表
                    var rfield = relationmetadata.GetField(field.RelationField);
                    var primary = field.Model;
                    var secondary = relationmetadata;
                    var name1 = primary.Name.Split('.').Last();
                    var name2 = secondary.Name.Split('.').Last();
                    if ((!field.IsPrimary && rfield.IsPrimary) || ((field.IsPrimary == rfield.IsPrimary) && string.Compare(name1, name2) > 0))
                    {
                        primary = relationmetadata;
                        secondary = field.Model;
                        var tmp = name1;
                        name1 = name2;
                        name2 = tmp;
                    }
                    string middlename = string.Format("{0}_to_{1}", primary.Name, name2);
                    string middletable = string.IsNullOrEmpty(field.MiddleTable) ? string.Format("{0}_{1}", primary.TableName, name2) : field.MiddleTable;
                    if (!models.ContainsKey(middlename))
                    {
                        var middlemeta = new DomainModel() { Name = middlename, TableName = middletable };
                        middlemeta.SetState(MetaMode.Base);
                        middlemeta.SetType(typeof(SmartRoot));
                        var field1 = new DomainField(FieldType.uniqueidentifier);
                        var role1 = field.RelationField;
                        if (role1.ToLower().EndsWith("s", StringComparison.Ordinal))
                            role1 = role1.Substring(0, role1.Length - 1);
                        field1.SetName(string.Format("{0}{1}", role1, field.PrimaryField ?? "Id"));
                        field1.SetType(typeof(Guid));
                        middlemeta.SetField(field1);

                        var field2 = new DomainField(FieldType.uniqueidentifier);
                        var role2 = rfield.RelationField;
                        if (role2.ToLower().EndsWith("s", StringComparison.Ordinal))
                            role2 = role2.Substring(0, role2.Length - 1);
                        field2.SetName(string.Format("{0}{1}", role2, rfield.PrimaryField ?? "Id"));
                        field2.SetType(typeof(Guid));
                        middlemeta.SetField(field2);
                        models[middlename] = middlemeta;
                    }
                    #endregion
                }
            }
            else if (field.Field_Type == FieldType.many2one && field.Model.ObjType == DomainType.AggRoot && field.PrimaryField != field.Name && !relationmetadata.Fields.Values.Any(s => s.Field_Type == FieldType.one2many && s.RelationModel == field.Model.Name && s.RelationField == field.PrimaryField))
            {
                var dfield = new DomainField(FieldType.one2many, field.Model.ModelType, field.PrimaryField, "");
                dfield.NonPublic = true;
                dfield.IsInject = true;
                dfield.Model = relationmetadata;
                dfield.SetName(field.Name + field.Model.ModelType.Name);
                dfield.SetType(typeof(AggregateCollection<>).MakeGenericType(field.Model.ModelType), false);
                dfield.RelationMode = field.RelationMode;
                relationmetadata.SetField(dfield);
            }
        }
        static void Apply(DomainModel meta)
        {
            if (!actions.ContainsKey(meta.Name))
                return;
            foreach (var action in actions[meta.Name].ToList())
            {
                action(meta);
            }
        }

        static List<PropertyInfo> getProperties(Type type)
        {
            var dproperty = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .GroupBy(s => s.DeclaringType).ToDictionary(s => s.Key);
            List<PropertyInfo> properties = new List<PropertyInfo>();
            for (int i = 0; i < dproperty.Count; i++)
            {
                properties.AddRange(dproperty.ElementAt(dproperty.Count - i - 1).Value);
            }
            return properties;
        }
        /// <summary>
        /// 解析并注册
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static DomainModel FromType(Type type)
        {
            var name = type.MetaName();
            var metadata = GetModel(name);
            if (metadata != null)
                return metadata;

            var mattrs = type.GetCustomAttributes(true);
            metadata = mattrs.OfType<DomainModel>().FirstOrDefault();
            if (metadata == null)
                return null;
            metadata.Attrs = mattrs;
            metadata.Authorization = mattrs.OfType<Authorize>().Select(s => s.ToAuthObj());
            metadata.SetType(type);
            metadata.SetState(MetaMode.Base);
            var properties = getProperties(type);
            metadata.Fields = new Dictionary<string, DomainField>(properties.Count);
            Dictionary<string, DomainField> injectfields = new Dictionary<string, DomainField>();
            foreach (var inject in mattrs.OfType<MetaInject>())
            {
                foreach (var field in inject.GetInjects())
                {
                    injectfields[field.Name] = field;
                }
            }
            foreach (var property in properties)
            {
                DomainField fieldmeta = null;
                var attrs = property.GetCustomAttributes(true);
                if (injectfields.ContainsKey(property.Name))
                {
                    fieldmeta = injectfields[property.Name];
                    injectfields.Remove(property.Name);
                }
                else
                {
                    fieldmeta = attrs.OfType<DomainField>().FirstOrDefault();
                    if (fieldmeta == null)
                        continue;
                }
                fieldmeta.Model = metadata;
                fieldmeta.SetProperty(property);
                if (string.IsNullOrEmpty(fieldmeta.Resource))
                    fieldmeta.Resource = string.Format("fieldlabel.{0}.{1}", metadata.Name, fieldmeta.Name.ToLower());
                fieldmeta.AddValidators(attrs.OfType<Validation.Validator>());
                var allow = attrs.OfType<Allow>().FirstOrDefault();
                if (allow != null)
                    fieldmeta.AuthObj = allow.ToAuthObj(fieldmeta.Name, AuthTarget.Field);
                metadata.Fields[fieldmeta.Name] = fieldmeta;
            }
            foreach (var pair in injectfields)
            {
                var fieldmeta = pair.Value;
                fieldmeta.Model = metadata;
                metadata.Fields[pair.Key] = fieldmeta;
            }
            Apply(metadata);
            models[name] = metadata;
            models[metadata.Name] = metadata;
            if (metadata.IsExtra)
            {
                var orgmeta = FromType(type.BaseType);
                metadata.TableName = orgmeta.TableName;
                orgmeta.Extra(metadata);
            }
            return metadata;
        }

        /// <summary>
        /// 注册
        /// </summary>
        /// <param name="model"></param>
        public static void RegisterMeta(DomainModel model)
        {
            if (model != null)
            {
                models[model.Name] = model;
                Apply(model);
            }
        }
        public static void RemoveMeta(string name)
        {
            if (models.ContainsKey(name))
                models[name].Remove();
        }

        internal static DomainModel GetModel(string name)
        {
            return models.ContainsKey(name) ? models[name] : null;
        }
        internal static IEnumerable<DomainModel> GetModels()
        {
            return models.Values;
        }
        #endregion

        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 标签
        /// </summary>
        public string Label { get; set; }

        public string GetLabel()
        {
            return Feature.Translation.Get(string.Format("modellabel.{0}", Name), Label, true);
        }

        /// <summary>
        /// 数据表名称
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// 是否不需要创建数据表，用于过期的对象或不需要持久化的对象
        /// </summary>
        public bool NoTable { get; set; }
        /// <summary>
        /// 是否要缓存数据
        /// </summary>
        public bool IsCache { get; set; }

        /// <summary>
        /// 类型
        /// </summary>
        public MetaMode State { get; private set; }

        /// <summary>
        /// 对象的类型
        /// </summary>
        public Type ModelType { get; private set; }

        /// <summary>
        /// 领域对象类型
        /// </summary>
        public DomainType ObjType { get; internal set; }

        /// <summary>
        /// 默认排序字段
        /// </summary>
        public string SortBy { get; set; }

        /// <summary>
        /// 默认排序方式
        /// </summary>
        public SortOrder SortOrder { get; set; }

        /// <summary>
        /// 是否为基类的扩展
        /// </summary>
        public bool IsExtra { get; set; }

        bool? _logModify = null;
        /// <summary>
        /// 维护修改日志
        /// </summary>
        public bool LogModify
        {
            get { _logModify = (_logModify == null) || _logModify.Value; return _logModify ?? true; }
            set { _logModify = value; }
        }

        private IEnumerable<AuthObject> authObjects;
        /// <summary>
        /// 角色权限
        /// </summary>
        public IEnumerable<AuthObject> Authorization
        {
            get
            {
                if (authObjects == null)
                    authObjects = new List<AuthObject>();
                return authObjects;
            }
            private set
            {
                authObjects = value;
            }
        }

        /// <summary>
        /// 对象特性集
        /// </summary>
        public IEnumerable<object> Attrs { get; private set; }

        bool? isobjecttemplate;
        /// <summary>
        /// 是否充当自定义对象模板
        /// </summary>
        public bool IsObjectTemplate
        {
            get
            {
                if (isobjecttemplate == null)
                    isobjecttemplate = Attrs != null && Attrs.OfType<CustomObjectTemplateAttribute>().Count() == 1;
                return isobjecttemplate.Value;
            }
        }
        DomainModel m_topmodel;
        /// <summary>
        /// 上级对象 用于entity
        /// </summary>
        public DomainModel TopModel
        {
            get
            {
                if (m_topmodel == null)
                    m_topmodel = DomainModel.GetModels().FirstOrDefault(s => s.GetFields(t => t.Field_Type == FieldType.one2many && t.RelationModel == Name && t.IsEntityCollection).Count() == 1);
                return m_topmodel;
            }
        }

        List<MetaExtension> extensions = new List<MetaExtension>();

        /// <summary>
        /// 设置元数据扩展
        /// </summary>
        /// <param name="extension"></param>
        public void SetExtension(MetaExtension extension)
        {
            if (extension != null)
            {
                extension.ModelMeta = this;
                extensions.RemoveAll(s => s.GetType() == extension.GetType());
                extensions.Add(extension);
            }
        }
        /// <summary>
        /// 获取扩展
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetExtension<T>()
            where T : MetaExtension
        {
            return extensions.OfType<T>().FirstOrDefault();
        }
        /// <summary>
        /// 是否行为型对象
        /// </summary>
        public bool IsBehavior
        {
            get
            {
                return ObjType == DomainType.Form || ObjType == DomainType.Handler;
            }
        }

        public MethodInfo GetItem { get; private set; }

        #region 字段相关

        public event EventHandler<ModelChangeArgs> onModelRemove;
        public event EventHandler<FieldChangeArgs> onFieldChange;
        public event EventHandler<FieldChangeArgs> onFieldRemove;

        IDictionary<string, DomainField> m_fields;
        /// <summary>
        /// 字段
        /// </summary>
        protected IDictionary<string, DomainField> Fields
        {
            get
            {
                if (m_fields == null)
                    m_fields = new OrderlyDict<string, DomainField>(10);
                return m_fields;
            }
            set { m_fields = value; }
        }

        /// <summary>
        /// 是否可以做为依赖项
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool ValidDepend(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;
            var tfield = name.Split('.')[0];
            return tfield == "TopObj" || ContainField(tfield);
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
        /// 获取字段
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public DomainField GetField(string name)
        {
            if (Fields.ContainsKey(name))
                return Fields[name];
            return null;
        }
        /// <summary>
        /// 设置字段
        /// </summary>
        /// <param name="field">字段</param>
        /// <param name="oldname">老字段名称</param>
        public void SetField(DomainField field)
        {
            field.Model = this;
            Fields[field.Name] = field;
            if (onFieldChange != null)
                onFieldChange(this, new FieldChangeArgs(field));
        }
        /// <summary>
        /// 删除字段
        /// </summary>
        /// <param name="name"></param>
        public void RemoveField(string name)
        {
            if (Fields.ContainsKey(name))
            {
                var field = Fields[name];
                Fields.Remove(name);
                if (onFieldRemove != null)
                    onFieldRemove(this, new FieldChangeArgs(field));
            }
        }
        /// <summary>
        /// 删除元数据
        /// </summary>
        public void Remove()
        {
            if (models.ContainsKey(Name))
                models.Remove(Name);
            if (actions.ContainsKey(Name))
                actions.Remove(Name);
            if (onModelRemove != null)
                onModelRemove(this, new ModelChangeArgs(this));
        }

        /// <summary>
        /// 获取所有符合条件的字段
        /// </summary>
        /// <returns></returns>
        public IEnumerable<DomainField> GetFields(Func<DomainField, bool> filter = null)
        {
            var values = Fields.Values.AsEnumerable();
            if (filter != null)
                values = values.Where(filter);
            return values;
        }

        /// <summary>
        /// 包含的字段数量
        /// </summary>
        public int FieldCount
        {
            get { return Fields.Count; }
        }
        #endregion

        public void SetType(Type modeltype)
        {
            ObjType = DomainHelper.GetDomainType(modeltype);
            if (string.IsNullOrEmpty(Name) && ObjType == DomainType.Handler)
            {
                var attr = Attrs.OfType<MsgRegisterAttribute>().FirstOrDefault();
                if (attr != null)
                {
                    var gtype = TypeHelper.GetBaseGenericType(modeltype, typeof(MsgHandler));
                    if (gtype != null)
                        Name = string.Format("{0}.{1}", gtype.MetaName(), attr.Name);
                }
            }

            if (string.IsNullOrEmpty(Name))
                Name = modeltype.MetaName();

            if (string.IsNullOrEmpty(Label))
                Label = modeltype.Name;
            ModelType = modeltype;
            if (string.IsNullOrEmpty(TableName))
                TableName = Name.Replace('.', '_');
            if (string.IsNullOrEmpty(SortBy) && modeltype.IsSubclassOf(typeof(OrderedEntity)))
            {
                SortBy = "ItemNo";
                SortOrder = SortOrder.Ascending;
            }
            GetItem = modeltype.GetMethod("get_Item", new Type[] { typeof(string) });
        }
        public void SetState(MetaMode metastate)
        {
            State = metastate;
        }

        public DomainModel()
        {
            IsCache = false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tablename"></param>
        /// <param name="iscache"></param>
        public DomainModel(bool iscache)
        {
            IsCache = iscache;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="basemodel"></param>
        public DomainModel(DomainModel basemodel)
        {
            Authorization = basemodel.Authorization;
            Attrs = basemodel.Attrs.Where(s => !(s is CustomObjectTemplateAttribute)).AsEnumerable();
        }

        /// <summary>
        /// 用于自定义对象
        /// </summary>
        /// <param name="auths"></param>
        /// <param name="attrs"></param>
        public void ResetBase(DomainModel basemodel)
        {
            Authorization = basemodel.Authorization;
            Attrs = basemodel.Attrs.Where(s => !(s is CustomObjectTemplateAttribute)).AsEnumerable();
        }

        public void Extra(DomainModel meta)
        {
            foreach (var field in meta.Fields.Values)
            {
                var orgfield = GetField(field.Name);
                //if (orgfield != null && orgfield.PropertyInfo != null && field.PropertyInfo.DeclaringType != orgfield.PropertyInfo.DeclaringType)
                //{
                //    orgfield.UpdateFrom(field);
                //    SetField(orgfield);
                //}
                //else
                if (orgfield == null || orgfield.PropertyInfo == null)
                {
                    var tmp = field.Clone();
                    if (tmp.Field_Type == FieldType.one2many)
                    {
                        foreach (var old in GetFields(s => s.Field_Type == FieldType.one2many && s.RelationModel == tmp.RelationModel && s.RelationField == tmp.RelationField).ToList())
                        {
                            RemoveField(old.Name);
                        }
                    }
                    SetField(tmp);
                }
                else if (field.PropertyInfo.DeclaringType != orgfield.PropertyInfo.DeclaringType)
                {
                    orgfield.UpdateFrom(field);
                    SetField(orgfield);
                }
            }
        }
    }
}
