using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Linq.Expressions;
using Owl.Util;
using Owl.Feature;

namespace Owl.Domain
{
    public abstract class FieldMetadata
    {
        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get { return m_metadata.Name; } }

        /// <summary>
        /// 标签
        /// </summary>
        public string Label { get { return m_metadata.GetLabel(); } }
        /// <summary>
        /// 字段类型
        /// </summary>
        public Type PropertyType { get { return m_metadata.PropertyType; } }

        /// <summary>
        /// 属性
        /// </summary>
        public PropertyInfo PropertyInfo { get { return m_metadata.PropertyInfo; } }

        /// <summary>
        /// 是否为根的实体集合
        /// </summary>
        public bool IsEntityCollection { get { return m_metadata.IsEntityCollection; } }
        /// <summary>
        /// 必填
        /// </summary>
        public bool Required { get { return m_metadata.Required; } }
        /// <summary>
        /// 自动搜索
        /// </summary>
        public bool AutoSearch { get { return m_metadata.AutoSearch; } }
        /// <summary>
        /// 字段类型
        /// </summary>
        public FieldType Field_Type { get { return m_metadata.Field_Type; } }

        /// <summary>
        /// 增量更新
        /// </summary>
        /// <value><c>true</c> if inc upate; otherwise, <c>false</c>.</value>
        public bool IncUpate { get { return m_metadata.IncUpdate; } }
        /// <summary>
        /// 数据格式化
        /// </summary>
        public string Format { get { return m_metadata.Format; } }

        /// <summary>
        /// 是否多选
        /// </summary>
        public bool Multiple { get { return m_metadata.Multiple; } }
        /// <summary>
        /// 文件保存到数据库
        /// </summary>
        public bool FileSaveDataBase { get { return m_metadata.FileSaveDataBase; } }


        /// <summary>
        /// 缺省值
        /// </summary>
        public object Default { get { return Variable.GetValue(m_metadata.Default); } }

        //public string[] Dependence { get { return m_metadata.Dependence; } }
        /// <summary>
        /// 字段元数据
        /// </summary>
        protected DomainField m_metadata { get; private set; }

        public DomainField GetDomainField() { return m_metadata; }

        /// <summary>
        /// 是否是导航字段
        /// </summary>
        public bool IsNavigate
        {
            get
            {
                return Field_Type == FieldType.many2one || Field_Type == FieldType.many2many || Field_Type == FieldType.one2many;
            }
        }
        /// <summary>
        /// 验证
        /// </summary>
        public virtual void Validate(object value, Object2 obj)
        {
            m_metadata.Validate(value, obj);
        }
        /// <summary>
        /// 字段的对象元数据
        /// </summary>
        public ModelMetadata Metadata { get; private set; }
        /// <summary>
        /// 获取有效字段名称
        /// </summary>
        /// <returns></returns>
        public virtual string GetFieldname()
        {
            return Name;
        }
        /// <summary>
        /// 当前字段类是否与字段类型匹配
        /// </summary>
        public abstract bool IsMatch { get; }

        /// <summary>
        /// 是否可以忽略当前字段
        /// </summary>
        public virtual bool CanIgnore { get { return false; } }

        /// <summary>
        /// 是否忽略本字段的变更日志
        /// </summary>
        public virtual bool IgnoreLogModify { get { return m_metadata.IgnoreLogModify; } }

        public static FieldMetadata Create(ModelMetadata modelmeta, DomainField fieldmeta)
        {
            FieldMetadata field = null;
            switch (fieldmeta.Field_Type)
            {
                case FieldType.str:
                case FieldType.text:
                case FieldType.richtext:
                case FieldType.password:
                    field = new StringField(); break;
                case FieldType.select: field = new SelectField(); break;
                case FieldType.one2many: field = new One2ManyField(); break;
                case FieldType.many2one: field = new Many2OneField(); break;
                case FieldType.many2many: field = new Many2ManyField(); break;
                default: field = new ScalerField(); break;
            }
            field.m_metadata = fieldmeta;
            field.Metadata = modelmeta;
            return field;
        }

        internal void UpdateDomainMeta(DomainField field)
        {
            m_metadata = field;
        }
        public virtual void onMetaChange()
        {
        }

        /// <summary>
        /// 获取字段值的显示文本
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public string GetDisplay(Object2 obj)
        {
            if (obj == null)
                return "";
            var fieldname = GetFieldname();
            var value = obj.GetRealValue(fieldname) ?? obj.GetRealValue(Name);
            var display = obj.GetDisplay(fieldname).Coalesce(obj.GetDisplay(Name));
            if (display != string.Format("{0}", value))
                return display;
            return GetDisplay(value, obj);
        }

        /// <summary>
        /// 获取字段值的显示文本
        /// </summary>
        /// <param name="value"></param>
        /// <param name="dto"></param>
        /// <returns></returns>
        public string GetDisplay(object value, Object2 obj)
        {
            if (value == null)
                return "";

            return _GetDisplay(value, obj).Coalesce(string.Format(Format.Coalesce("{0}"), value));
        }

        protected virtual string _GetDisplay(object value, Object2 entity)
        {
            var format = Format.Coalesce("{0}");
            return string.Format(format, value);
        }
    }

    public class ScalerField : FieldMetadata
    {
        public override bool IsMatch
        {
            get { return true; }
        }
    }
    public class StringField : ScalerField
    {
        public override bool IsMatch
        {
            get { return Field_Type == FieldType.str || Field_Type == FieldType.text || Field_Type == FieldType.richtext || Field_Type == FieldType.str; }
        }
    }
    /// <summary>
    /// 选择项字段
    /// </summary>
    public class SelectField : ScalerField
    {

        public override bool IsMatch
        {
            get
            {
                return Field_Type == FieldType.select;
            }
        }

        bool? dynamic;
        /// <summary>
        /// 选择项是否可变
        /// </summary>
        protected bool Dynamic
        {
            get
            {
                if (dynamic == null)
                    dynamic = m_metadata.Selection != null && m_metadata.Selection.StartsWith("@");
                return dynamic.Value;
            }
        }

        /// <summary>
        /// 获取所有列表项
        /// </summary>
        /// <param name="topvalue"></param>
        /// <returns></returns>
        public ListOptionCollection GetItems(params string[] topvalue)
        {
            for (var i = 0; i < m_metadata.Dependence.Length; i++)
            {
                Variable.CurrentParameters[m_metadata.Dependence[i]] = topvalue.Length > i ? topvalue[i] : "";
            }
            return m_metadata.ListOptions;
        }
        /// <summary>
        /// 获取指定select值的文本
        /// </summary>
        /// <param name="value">值</param>
        /// <param name="depvalues">依赖值</param>
        /// <returns></returns>
        public string GetText(string value, params string[] depvalues)
        {
            if (string.IsNullOrEmpty(value))
                return value;
            var text = value;
            if (!Dynamic)
            {
                text = m_metadata.ListOptions.GetText(value).Coalesce(value);
            }
            else
            {
                text = Select.GetText(Metadata.Name, m_metadata.Selection.Substring(1), value, m_metadata.TransformTopvlues(depvalues));
                text = text.Coalesce(value);
            }
            return text;
        }
        /// <summary>
        /// 获取指定select文本的值
        /// </summary>
        /// <param name="text">文本</param>
        /// <param name="depvalues">依赖值</param>
        /// <returns></returns>
        public string GetValue(string text, params string[] depvalues)
        {
            if (string.IsNullOrEmpty(text))
                return text;
            var value = text;
            if (!Dynamic)
            {
                value = m_metadata.ListOptions.GetValue(text).Coalesce(text);
            }
            else
            {
                value = Select.GetValue(Metadata.Name, m_metadata.Selection.Substring(1), text, m_metadata.TransformTopvlues(depvalues));
                value = value.Coalesce(text);
            }
            return value;
        }

        protected override string _GetDisplay(object value, Object2 dto)
        {
            return GetText(string.Format("{0}", value), GetDomainField().Dependence.Select(s => dto.GetRealValue<string>(s)).ToArray());
        }

        public override void Validate(object value, Object2 obj)
        {
            base.Validate(value, obj);

            //if (!Dynamic && value is string && !m_metadata.ListOptions.GetText Tvs.ContainsKey((string)value))
            //{
            //    throw new ArgumentOutOfRangeException();
            //}
        }
    }

    public abstract class NavigatField : FieldMetadata
    {
        /// <summary>
        /// 关联模型
        /// </summary>
        public string RelationModel { get { return m_metadata.RelationModel; } }
        /// <summary>
        /// 关联字段
        /// </summary>
        public string RelationField { get { return m_metadata.RelationField; } }

        /// <summary>
        /// 关联显示字段
        /// </summary>
        public string[] RelationDisField { get { return m_metadata.RelationDisField; } }

        /// <summary>
        /// 此关系的本方主键
        /// </summary>
        public string PrimaryField { get { return m_metadata.PrimaryField; } }
        /// <summary>
        /// 是否为主表
        /// </summary>
        public bool IsPrimary { get { return m_metadata.IsPrimary; } }

        /// <summary>
        /// 关系对象类型
        /// </summary>
        public Type RelationType { get { return m_metadata.RelationType; } }

        /// <summary>
        /// 关系模式
        /// </summary>
        public RelationMode RelationMode { get { return m_metadata.RelationMode; } }

        ParameterExpression m_modelparam;
        /// <summary>
        /// 
        /// </summary>
        public ParameterExpression ModelParamExp
        {
            get
            {
                if (m_modelparam == null)
                {
                    m_modelparam = Expression.Parameter(Metadata.ModelType);
                }
                return m_modelparam;
            }
        }

        LambdaExpression filterexp;
        /// <summary>
        /// 过滤条件
        /// </summary>
        public LambdaExpression FilterExp
        {
            get
            {
                if (filterexp == null && m_metadata.Specific != null)
                {
                    filterexp = m_metadata.Specific.GetExpression(RelationModelMeta);
                }
                return filterexp;
            }
        }

        LambdaExpression exphasparam;

        /// <summary>
        /// 包含参数的过滤条件
        /// </summary>
        public LambdaExpression ExpHasParam
        {
            get
            {
                if (exphasparam == null && m_metadata.Specific != null)
                {
                    exphasparam = m_metadata.Specific.GetExpression(RelationModelMeta, ModelParamExp);
                }
                return exphasparam;
            }
        }

        /// <summary>
        /// 过滤条件
        /// </summary>
        public Specification Specific { get { return m_metadata.Specific; } }

        public override void onMetaChange()
        {
            filterexp = null;
        }
        /// <summary>
        /// 构建过滤条件
        /// </summary>
        /// <param name="spec"></param>
        /// <returns></returns>
        public LambdaExpression buildDomain(Specification spec, ParameterExpression para = null)
        {
            if (Specific == null && spec == null)
                return null;
            return Specification.And(Specific, spec).GetExpression(RelationModelMeta, para);
        }

        ModelMetadata m_relationmetadata;
        /// <summary>
        /// 关联对象元数据
        /// </summary>
        public ModelMetadata RelationModelMeta
        {
            get
            {
                if (m_relationmetadata == null)
                {
                    m_relationmetadata = ModelMetadataEngine.GetModel(RelationModel);
                    if (m_relationmetadata == null && RelationType != null)
                        m_relationmetadata = ModelMetadataEngine.GetModel(RelationType);
                }
                return m_relationmetadata;
            }
            set { m_relationmetadata = value; }
        }

        FieldMetadata m_primarymeta;
        /// <summary>
        /// 本方主键元数据
        /// </summary>
        public virtual FieldMetadata PrimaryFieldMeta
        {
            get
            {
                if (m_primarymeta == null)
                {
                    m_primarymeta = Metadata.GetField(PrimaryField);
                }
                return m_primarymeta;
            }
        }

        PropertyInfo m_primary;
        bool bprimary;
        /// <summary>
        /// 本方主键属性
        /// </summary>
        public PropertyInfo PrimaryInfo
        {
            get
            {
                if (!bprimary)
                {
                    bprimary = true;
                    if (PrimaryFieldMeta.PropertyInfo != null)
                    {
                        m_primary = PrimaryFieldMeta.PropertyInfo;
                        if (PrimaryFieldMeta.Field_Type == FieldType.many2one && Name != PrimaryField)
                            m_primary = Metadata.ModelType.GetProperty(PrimaryField, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    }
                }
                return m_primary;
            }
        }

        Type m_primarytype;
        /// <summary>
        /// 本方主键类型
        /// </summary>
        public Type PrimaryType
        {
            get
            {
                if (m_primarytype == null)
                {
                    var type = PrimaryFieldMeta.PropertyType;
                    if (PrimaryFieldMeta.Field_Type == FieldType.many2one && Name != PrimaryField)
                    {
                        type = Nullable.GetUnderlyingType(RelationFieldMeta.PropertyType) ?? RelationFieldMeta.PropertyType;
                        if (!PrimaryFieldMeta.Required && type != typeof(string))
                            type = typeof(Nullable<>).MakeGenericType(type);
                    }
                    m_primarytype = type;
                }
                return m_primarytype;
            }
        }

        FieldMetadata m_relationmeta;
        /// <summary>
        /// 对方元数据
        /// </summary>
        public FieldMetadata RelationFieldMeta
        {
            get
            {
                if (m_relationmeta == null)
                    m_relationmeta = RelationModelMeta.GetField(RelationField);
                return m_relationmeta;
            }
        }
        PropertyInfo m_relationinfo;
        bool brelation;
        public PropertyInfo RelationInfo
        {
            get
            {
                if (!brelation)
                {
                    brelation = true;
                    if (RelationFieldMeta.PropertyInfo != null)
                    {
                        m_relationinfo = RelationFieldMeta.PropertyInfo;
                        if (RelationFieldMeta.Field_Type == FieldType.many2one && RelationFieldMeta.Name != RelationField)
                            m_relationinfo = RelationModelMeta.ModelType.GetProperty(RelationField, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    }
                }
                return m_relationinfo;
            }
        }

        Type m_relationtype;
        /// <summar
        /// 本方主键类型
        /// </summary>
        public Type RelationFieldType
        {
            get
            {
                if (m_relationtype == null)
                {
                    var type = RelationFieldMeta.PropertyType;
                    if (RelationFieldMeta.Field_Type == FieldType.many2one && RelationFieldMeta.Name != RelationField)
                    {
                        type = Nullable.GetUnderlyingType(PrimaryFieldMeta.PropertyType) ?? PrimaryFieldMeta.PropertyType;
                        if (!RelationFieldMeta.Required)
                            type = typeof(Nullable<>).MakeGenericType(type);
                    }
                    m_relationtype = type;
                }
                return m_relationtype;
            }
        }
    }


    public class One2ManyField : NavigatField
    {
        public override bool IsMatch
        {
            get { return Field_Type == FieldType.one2many; }
        }
    }
    public class Many2OneField : NavigatField
    {
        public override bool IsMatch
        {
            get { return Field_Type == FieldType.many2one; }
        }
        public override bool CanIgnore
        {
            get
            {
                return Metadata.ContainField(PrimaryField) && Name != PrimaryField;
            }
        }
        public override FieldMetadata PrimaryFieldMeta
        {
            get
            {
                return this;
            }
        }
        protected override string _GetDisplay(object value, Object2 dto)
        {
            AggRoot robj = null;
            if (RelationField == RelationModelMeta.PrimaryField.Name)
            {
                robj = Repository.FindById(RelationModelMeta, (Guid)value, RelationDisField);
            }
            else
            {
                Specification spec = null;
                if (!string.IsNullOrEmpty(RelationField))
                {
                    spec = Specification.Create(RelationField, CmpCode.EQ, value);
                }
                if (Specific != null)
                {
                    foreach (var param in Specific.Parameters)
                    {
                        Variable.CurrentParameters[param] = dto.GetRealValue(param);
                    }
                }
                robj = Repository.FindFirst(RelationModelMeta, buildDomain(spec), RelationDisField);
            }
            if (robj != null)
                return string.Join(Const.CoreConst.SelectDisplaySeparator, RelationDisField.Select(s => robj[s]));
            return base._GetDisplay(value, dto);
        }
        public override string GetFieldname()
        {
            return PrimaryField;
        }
        /// <summary>
        /// 是否为本端单边数据
        /// </summary>
        public bool IsSingle
        {
            get { return RelationDisField.Length == 1 && RelationDisField[0] == RelationField; }
        }
    }
    public class Many2ManyField : NavigatField
    {
        public override bool IsMatch
        {
            get { return Field_Type == FieldType.many2many; }
        }
        string m_middlefield;
        /// <summary>
        /// 中间表对应的本端字段
        /// </summary>
        public string MiddleField
        {
            get
            {
                if (m_middlefield == null)
                {
                    var role = RelationField;
                    if (role.ToLower().EndsWith("s"))
                        role = role.Substring(0, role.Length - 1);
                    m_middlefield = string.Format("{0}{1}", role, m_metadata.PrimaryField ?? "Id");
                }
                return m_middlefield;
            }
        }

        string m_targetfield;
        /// <summary>
        /// 中间表的对端字段
        /// </summary>
        public string TargetMiddleField
        {
            get
            {
                if (m_targetfield == null)
                    m_targetfield = (RelationModelMeta.GetField(RelationField) as Many2ManyField).MiddleField;
                return m_targetfield;
            }
        }

        string m_middletable;
        /// <summary>
        /// 中间表明称
        /// </summary>
        public string MiddleTable
        {
            get
            {
                if (!string.IsNullOrEmpty(m_metadata.MiddleTable))
                    return m_metadata.MiddleTable;

                if (m_middletable == null)
                {
                    var role1 = Metadata.ModelType.Name;
                    var table1 = Metadata.TableName;
                    if (PropertyInfo != null && PropertyInfo.DeclaringType != Metadata.ModelType && !PropertyInfo.DeclaringType.IsAbstract)
                    {
                        role1 = PropertyInfo.DeclaringType.Name;
                        table1 = MetaEngine.GetModel(PropertyInfo.DeclaringType).TableName;
                    }
                    var role2 = RelationModelMeta.ModelType.Name;
                    var table2 = RelationModelMeta.TableName;
                    var relfiled = (RelationModelMeta.GetField(RelationField) as Many2ManyField);
                    if (relfiled.PropertyInfo != null && relfiled.PropertyInfo.DeclaringType != relfiled.Metadata.ModelType && !relfiled.PropertyInfo.DeclaringType.IsAbstract)
                    {
                        role2 = relfiled.PropertyInfo.DeclaringType.Name;
                        table2 = MetaEngine.GetModel(relfiled.PropertyInfo.DeclaringType).TableName;
                    }

                    if ((!IsPrimary && relfiled.IsPrimary) || ((IsPrimary == relfiled.IsPrimary) && string.Compare(role1, role2) > 0))
                    {
                        table1 = table2;
                        role2 = role1;
                    }
                    m_middletable = string.Format("{0}_{1}", table1, role2.ToLower());
                }
                return m_middletable;
            }
        }
    }
}
