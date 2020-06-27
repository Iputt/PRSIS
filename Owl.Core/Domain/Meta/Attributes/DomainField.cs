using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl;
using Owl.Domain.Driver;
using System.Reflection;
using Owl.Util;
using Owl.Domain.Validation;
using Owl.Feature;
namespace Owl.Domain
{
    /// <summary>
    /// 只读类型
    /// </summary>
    public enum ReadOnlyType
    {
        /// <summary>
        /// 不只读
        /// </summary>
        [DomainLabel("非只读")]
        None = 0,
        /// <summary>
        /// 选择性只读，新增时默认值可更改
        /// </summary>
        [DomainLabel("编辑时只读")]
        Optional = 1,
        /// <summary>
        /// 强制只读，任何时候都不可修改
        /// </summary>
        [DomainLabel("强制只读")]
        Force = 2
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
    public class DomainLabel : Attribute
    {
        public DomainLabel(string label)
        {
            Label = label;
        }
        public DomainLabel()
        {
        }
        /// <summary>
        /// 标签
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// 资源名称
        /// </summary>
        public string Resource { get; set; }
        /// <summary>
        /// 获取标签多语言支持
        /// </summary>
        /// <returns></returns>
        public string GetLabel()
        {
            return Translation.Get(Resource, Label, true);
        }
    }

    /// <summary>
    /// 领域字段
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, Inherited = true)]
    public class DomainField : DomainLabel
    {
        #region 属性
        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// 帮助
        /// </summary>
        public string Help { get; set; }
        /// <summary>
        /// 字段类型
        /// </summary>
        public FieldType Field_Type { get; set; }

        int? m_size;
        /// <summary>
        /// 字段大小 整形 16，32，64 浮点32，64，128 字符串表示字符串长度,字符串默认长度为128
        /// </summary>
        public int Size
        {
            get
            {
                if (m_size == null)
                    m_size = DomainHelper.GetSize(Field_Type, PropertyType);
                return m_size.Value;
            }
            set { m_size = value == 0 ? null : (int?)value; }
        }

        int? m_precision;
        /// <summary>
        /// 精度，小数位数
        /// </summary>
        public int Precision
        {
            get
            {
                if (m_precision == null)
                    m_precision = DomainHelper.GetPrecision(Field_Type, PropertyType);
                return m_precision.Value;
            }
            set { m_precision = value; }
        }

        /// <summary>
        /// 数据格式化
        /// </summary>
        public string Format { get; set; }

        /// <summary>
        /// 是否必填
        /// </summary>
        public bool Required { get; set; }


        /// <summary>
        /// 只读
        /// </summary>
        public ReadOnlyType Readonly { get; set; }

        /// <summary>
        /// 增量更新
        /// </summary>
        /// <value><c>true</c> if inc update; otherwise, <c>false</c>.</value>
        public bool IncUpdate { get; set; }

        /// <summary>
        /// 是否公用，为false则自动创建视图时不会生成
        /// </summary>
        public bool NonPublic { get; set; }

        /// <summary>
        /// 无权限时隐藏本字段，否则不生成
        /// </summary>
        public bool HideNoPower { get; set; }

        object _default;
        /// <summary>
        /// 自动计算缺省值
        /// </summary>
        public object Default2
        {
            get
            {
                if (_default == null && Required)
                {
                    switch (Field_Type)
                    {
                        case FieldType.bollean: _default = false; break;
                        //case FieldType.digits: _default = 0; break;
                        //case FieldType.number: _default = 0; break;
                        case FieldType.select:
                            var item = ListOptions.First;
                            if (item != null)
                                _default = item.Value;
                            break;
                        default: break;
                    }
                }
                return _default;
            }
        }


        /// <summary>
        /// 默认值
        /// </summary>
        public object Default { get; set; }

        /// <summary>
        /// 忽略搜索默认值
        /// </summary>
        public bool IgnoreSearchDefault { get; set; }

        bool? _ignorelogmofidy;
        /// <summary>
        /// 忽略本字段的变更日志
        /// </summary>
        public bool IgnoreLogModify
        {
            get
            {
                if (_ignorelogmofidy == null && Model.TopModel != null)
                {
                    if (Name == "TopId" || Name == Model.TopModel.Name + "Id")
                        _ignorelogmofidy = true;
                }
                return _ignorelogmofidy ?? false;
            }
            set { _ignorelogmofidy = value; }
        }

        /// <summary>
        /// 不需要做权限验证 
        /// </summary>
        public bool NotVerify { get; set; }
        /// <summary>
        /// 是否自动搜索
        /// </summary>
        public bool AutoSearch { get; set; }

        /// <summary>
        /// 字段类型
        /// </summary>
        public Type PropertyType { get; protected set; }

        /// <summary>
        /// 需要翻译内容
        /// </summary>
        public bool TranslateValue { get; set; }

        string m_tvrk;
        /// <summary>
        /// 内容翻译的resource
        /// </summary>
        public string TranslateValueResKey
        {
            get
            {
                if (m_tvrk == null)
                    m_tvrk = string.Format("fieldvalue.{0}.{1}", Model.Name, Name.ToLower());
                return m_tvrk;
            }
            set { m_tvrk = value; }
        }
        /// <summary>
        /// 属性
        /// </summary>
        public PropertyInfo PropertyInfo { get; private set; }

        int? minlength;
        /// <summary>
        /// 字符串的最小长度
        /// </summary>
        public int MinLength
        {
            get
            {
                if (minlength == null)
                {
                    switch (Field_Type)
                    {
                        case FieldType.datemonth: minlength = 6; break;
                        case FieldType.date: minlength = 10; break;
                        default: minlength = 0; break;
                    }
                }
                return minlength.Value;
            }
            set { minlength = value; }
        }

        /// <summary>
        /// 验证规则
        /// </summary>
        public string Pattern { get; set; }

        /// <summary>
        /// 最小值
        /// </summary>
        public object Min { get; set; }

        /// <summary>
        /// 最大值
        /// </summary>
        public object Max { get; set; }

        public object GetMin()
        {
            return Min == null ? null : Convert2.ChangeType(Min, PropertyType);
        }

        public object GetMax()
        {
            return Max == null ? null : Convert2.ChangeType(Max, PropertyType);
        }
        #region 主从关系
        /// <summary>
        /// 关系模型
        /// </summary>
        public string RelationModel { get; set; }

        /// <summary>
        /// 是否不向关系对象注入相关的关联字段
        /// </summary>
        public bool UnInjectRelation { get; set; }

        /// <summary>
        /// 是否是通过其他关系对象创建的字段
        /// </summary>
        public bool IsInject { get; set; }
        /// <summary>
        /// 关系对象类型
        /// </summary>
        public Type RelationType { get; private set; }
        /// <summary>
        /// 本端字段
        /// </summary>
        public string PrimaryField { get; set; }
        /// <summary>
        /// 关联字段
        /// </summary>
        public string RelationField { get; set; }
        /// <summary>
        /// 多对多关系的中间表
        /// </summary>
        public string MiddleTable { get; set; }
        /// <summary>
        /// 多对一关系中对方的展示字段 多个字段之间用 "," 分隔
        /// </summary>
        public string RelationDisplay { get; set; }

        string[] m_relationdisfield;
        /// <summary>
        /// 多对一关系中对方的展示字段
        /// </summary>
        public string[] RelationDisField
        {
            get
            {
                if (m_relationdisfield == null && !string.IsNullOrEmpty(RelationDisplay))
                {
                    m_relationdisfield = RelationDisplay.Split(',');
                }
                return m_relationdisfield;
            }
            set { m_relationdisfield = value; }
        }

        string domain;
        /// <summary>
        /// 过滤，用于关系和枚举
        /// </summary>
        public string Domain
        {
            get { return domain; }
            set
            {
                domain = value;
                m_specific = null;
                dependence = null;
            }
        }
        void ParseDepence()
        {
            if (Field_Type == FieldType.select)// && RelationType == null)
            {
                var depfields = new List<string>();
                var alldep = new List<string>();
                if (!string.IsNullOrEmpty(Domain))
                {
                    foreach (var v in Domain.Split(','))
                    {
                        var tmp = v.Trim();
                        if (string.IsNullOrEmpty(tmp))
                            continue;
                        if (tmp.StartsWith("@"))
                        {
                            var field = tmp.Substring(1);
                            alldep.Add(field);
                            if (Model.ValidDepend(field))
                                depfields.Add(field);
                        }
                        else
                            alldep.Add(tmp);
                    }
                }
                dependence = depfields.ToArray();
                alldependence = alldep.ToArray();
            }
            else if (Specific != null)
            {
                dependence = Specific.Parameters.Where(s => Model.ValidDepend(s)).ToArray();
                alldependence = new string[0];
            }
            else
            {
                dependence = new string[0];
                alldependence = new string[0];
            }
        }
        string[] dependence;

        /// <summary>
        /// 依赖字段
        /// </summary>
        public string[] Dependence
        {
            get
            {
                if (dependence == null)
                {
                    ParseDepence();
                }
                return dependence;
            }
        }
        string[] alldependence;
        /// <summary>
        /// select所有依赖项
        /// </summary>
        protected string[] AllSelectDependence
        {
            get
            {
                if (alldependence == null)
                    ParseDepence();
                return alldependence;
            }
        }
        /// <summary>
        /// 转换select的上级项为完整的
        /// </summary>
        /// <param name="topvalues"></param>
        /// <returns></returns>
        public string[] TransformTopvlues(string[] topvalues)
        {
            //增加topvalues为空判断，防止依赖项里面有null值占位，
            //导致AllSelectDependence数量大于Dependence数量
            if (AllSelectDependence.Length > Dependence.Length && topvalues != null)
            {
                List<string> arr = new List<string>();
                var index = 0;
                foreach (var dep in alldependence)
                {
                    if (Dependence.Contains(dep))
                    {
                        arr.Add(topvalues.Length > index ? topvalues[index] : null);
                        index++;
                    }
                    else
                    {
                        if (dep.StartsWith("'") && dep.EndsWith("'"))
                            arr.Add(dep.Substring(1, dep.Length - 2));
                        else
                            arr.Add(null);
                    }
                }
                return arr.ToArray();
            }
            return topvalues;
        }

        Specification m_specific;
        /// <summary>
        /// 
        /// </summary>
        public Specification Specific
        {
            get
            {
                if (m_specific == null)
                {
                    if (string.IsNullOrEmpty(Domain) || (Field_Type == FieldType.select && RelationType == null))
                        return null;
                    m_specific = Specification.Create(Domain);
                }
                return m_specific;
            }
        }

        /// <summary>
        /// 关系是否以本端对象为主,多对多关系需自定义
        /// </summary>
        public bool IsPrimary { get; set; }

        /// <summary>
        /// 关系模式
        /// </summary>
        public RelationMode RelationMode { get; set; }

        bool? isentitycollection;
        /// <summary>
        /// 是否实体集合
        /// </summary>
        public bool IsEntityCollection
        {
            get
            {
                if (PropertyType == null)
                    return isentitycollection ?? false;
                return PropertyType != null && PropertyType.Name == "EntityCollection`1";
            }
            set { isentitycollection = value; }
        }
        #endregion

        /// <summary>
        /// 默认显示文本，当many2one，select为空时
        /// </summary>
        public string DefaultDisplayText { get; set; }
        /// <summary>
        /// 选择项
        /// </summary>
        public string Selection { get; set; }
        /// <summary>
        /// 选择项
        /// </summary>
        public ListOptionCollection ListOptions
        {
            get
            {
                if (Field_Type == FieldType.select)
                {
                    if (!string.IsNullOrEmpty(Selection) && Selection.StartsWith("@"))
                    {
                        var topvalue = TransformTopvlues(Dependence.Select(s => string.Format("{0}", Variable.GetValue(s))).ToArray());
                        var selname = Selection.Substring(1);
                        var selkey = string.Format("owl.domain.domainfield.select.{0}.{1}", selname, string.Format(",", topvalue));
                        var tmpitems = Cache.Thread<ListOptionCollection>(selkey);
                        if (tmpitems == null)
                        {
                            tmpitems = new ListOptionCollection();
                            if (!Required && !Multiple)
                                tmpitems.AddOption(new ListItem("", ""));
                            tmpitems.Merge(Select.GetSelect(Model.Name, Selection.Substring(1), topvalue));
                            Cache.Thread(selkey, tmpitems);
                        }
                        return tmpitems;
                    }
                    else
                    {
                        var lokey = string.Format("{0}.{1}.listoption", Model.Name, Name);
                        var tmpitems = Cache.Thread<ListOptionCollection>(lokey);
                        if (tmpitems == null)
                        {
                            tmpitems = new ListOptionCollection();
                            if (!Required && !Multiple)
                                tmpitems.AddOption(new ListItem("", ""));

                            if (!string.IsNullOrEmpty(Selection) && Selection.StartsWith("{"))
                                tmpitems.Merge(ListOptionCollection.FromJson(Selection));
                            else if ((Nullable.GetUnderlyingType(PropertyType) ?? PropertyType).IsEnum)
                                tmpitems.Merge(ListOptionCollection.FromEnum(PropertyType));
                            Cache.Thread(lokey, tmpitems);
                        }
                        return tmpitems;
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// 是否多选 用于 many2one 或 selection
        /// </summary>
        public bool Multiple { get; set; }
        /// <summary>
        /// 文件保存到数据库
        /// </summary>
        public bool FileSaveDataBase { get; set; }

        /// <summary>
        /// 授权对象
        /// </summary>
        public AuthObject AuthObj { get; set; }

        #endregion

        #region 验证器

        bool validatorinit = false;
        void initvalidator()
        {
            if (!validatorinit)
            {
                validatorinit = true;
                List<Validator> vals = new List<Validator>();
                if (Required)
                    vals.Add(new RequiredValidator());
                switch (Field_Type)
                {
                    case FieldType.digits: vals.Add(new DigitsValidator()); break;
                    case FieldType.number: vals.Add(new NumberValidator(Precision)); break;
                    case FieldType.date: vals.Add(new DateValidator()); break;
                    case FieldType.time:
                    case FieldType.datemonth:
                    case FieldType.str:
                        vals.Add(new LengthValidator(MinLength, Size));
                        break;
                    case FieldType.richtext:
                    case FieldType.text: break;
                }
                if (!string.IsNullOrEmpty(Pattern))
                    vals.Add(new RegexValidator("", "", Pattern));
                if (Min != null || Max != null)
                    vals.Add(new RangeValidator(GetMin(), GetMax()));
                doaddvalidators(vals);
            }
        }
        Dictionary<string, Validator> m_validator = new Dictionary<string, Validator>();
        void doaddvalidators(IEnumerable<Validator> validators)
        {
            if (validators == null)
                return;
            foreach (var validator in validators)
                m_validator[validator.Type] = validator;
        }
        /// <summary>
        /// 添加验证器
        /// </summary>
        /// <param name="validators"></param>
        public void AddValidators(IEnumerable<Validator> validators)
        {
            initvalidator();
            doaddvalidators(validators);
        }

        /// <summary>
        /// 验证器集合
        /// </summary>
        public IEnumerable<Validator> Validators
        {
            get
            {
                initvalidator();
                return m_validator.Values;
            }
        }
        #endregion

        #region 元数据
        /// <summary>
        /// 对象元数据
        /// </summary>
        public DomainModel Model { get; internal set; }
        public void SetName(string name)
        {
            Name = name;
            if (string.IsNullOrEmpty(Label))
                Label = name;
        }
        public void SetType(Type propertytype, bool fitrelation = true)
        {
            if (Multiple)
            {
                if (propertytype.IsArray)
                    propertytype = propertytype.GetElementType();
                if (propertytype.IsGenericType && propertytype.Name != "Nullable`1")
                    propertytype = propertytype.GetGenericArguments().FirstOrDefault();
            }

            PropertyType = propertytype;
            var ptype = Nullable.GetUnderlyingType(propertytype) ?? propertytype;
            if (!Required && propertytype.IsValueType && ptype == propertytype)
                Required = true;
            #region
            if (Field_Type == FieldType.none)
            {
                Field_Type = DomainHelper.GetFieldType(ptype);
            }
            if (Field_Type == FieldType.many2one || Field_Type == FieldType.many2many || Field_Type == FieldType.one2many)
            {
                if (RelationType == null)
                    RelationType = ptype.IsGenericType ? ptype.GetGenericArguments()[0] : ptype;
                if (string.IsNullOrEmpty(RelationModel))
                    RelationModel = RelationType.MetaName();

                if (RelationField == null)
                {
                    string mname = Model.ModelType.Name;
                    if (PropertyInfo != null && PropertyInfo.DeclaringType != Model.ModelType && !PropertyInfo.DeclaringType.IsAbstract)
                        mname = PropertyInfo.DeclaringType.Name;
                    switch (Field_Type)
                    {
                        case FieldType.many2many: RelationField = mname + "s"; break;
                        case FieldType.many2one: RelationField = "Id"; break;
                        case FieldType.one2many:
                            RelationField = mname + "Id";
                            if (RelationType.GetProperty(RelationField, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) == null)
                            {
                                if (Model.IsExtra)
                                {
                                    RelationField = Model.ModelType.BaseType.Name + "Id";
                                    if (RelationType.GetProperty(RelationField, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) == null)
                                        RelationField = null;
                                }
                                else
                                    RelationField = null;
                            }
                            break;
                    }
                }
                if (RelationDisField == null && Field_Type != FieldType.one2many)
                {
                    // if (RelationType.GetProperty("Name", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) != null)
                    RelationDisField = new string[] { "Name" };
                }
                if (string.IsNullOrEmpty(PrimaryField))
                {
                    switch (Field_Type)
                    {
                        case FieldType.many2many:
                        case FieldType.one2many: PrimaryField = "Id"; break;
                        case FieldType.many2one: PrimaryField = PropertyType != RelationType ? Name : Name + "Id"; break;
                    }
                }
                if (Model != null && !Required && Field_Type == FieldType.many2one)
                {
                    var rfield = Model.ModelType.GetProperty(PrimaryField, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (rfield != null)
                        Required = rfield.PropertyType.IsValueType && !rfield.PropertyType.IsGenericType;
                }

                if (fitrelation && !UnInjectRelation && (Model == null || (!Model.IsObjectTemplate && !Model.ModelType.IsAbstract)))
                    DomainModel.FixRelation(this);
            }
            #endregion
        }

        public void SetProperty(PropertyInfo info)
        {
            PropertyInfo = info;
            SetName(info.Name);
            SetType(info.PropertyType);
            if (info.GetGetMethod(false) == null)
                NonPublic = true;
        }
        /// <summary>
        /// 验证字段
        /// </summary>
        /// <param name="value"></param>
        public void Validate(object value, Object2 obj)
        {
            foreach (var validator in Validators)
            {
                if (!validator.IsValid(value, obj))
                    throw new AlertException(validator.GetError(GetLabel()));
            }
        }

        public string GetHelp()
        {
            return Translation.Get(string.Format("{0}.{1}.help", Model.Name, Name.ToLower())) ?? Help;
        }
        #endregion

        #region 构造函数
        /// <summary>
        /// 基础构造函数
        /// </summary>
        public DomainField()
        {

        }

        /// <summary>
        /// 基础构造函数
        /// </summary>
        /// <param name="label">字段标签</param>
        /// <param name="fieldtype">字段类型</param>
        public DomainField(FieldType fieldtype)
            : this()
        {
            Field_Type = fieldtype;
            switch (fieldtype)
            {
                case FieldType.datemonth: MinLength = 6; Size = 6; break;
            }
        }

        /// <summary>
        /// 选择框
        /// </summary>
        /// <param name="selection">选择项json字符串 如：{'dreft':'草稿','new','新建'}</param>
        /// <param name="topvalue">上级值</param>
        /// <param name="poptype">弹窗关联类</param>
        public DomainField(string selection, string topvalue, Type poptype = null)
            : this(FieldType.select)
        {
            Selection = selection;
            Domain = topvalue;
            if (!string.IsNullOrEmpty(selection) && selection.StartsWith("@"))
            {
                if (poptype != null && !poptype.IsSubclassOf(typeof(Temp.TmpSelectOption)))
                    throw new Exception2("弹窗关联类必须继承自TmpSelectOption");
                if (poptype == null)
                    RelationType = typeof(Temp.TmpSelectOption);
                else
                    RelationType = poptype;
                RelationModel = RelationType.MetaName();
                RelationField = "Code";
                RelationDisplay = "Name";
                List<string> domainstr = new List<string>();
                domainstr.Add(string.Format("(Select,=,'{0}')", Selection.Substring(1)));

                if (!string.IsNullOrEmpty(topvalue))
                {
                    var index = 1;
                    foreach (var top in topvalue.Split(','))
                    {
                        domainstr.Add(string.Format("(TopCode{0},=,{1})", index == 1 ? "" : index.ToString(), top.StartsWith("@") ? top : ("'" + top + "'")));
                        index++;
                    }
                }
                m_specific = Specification.Create(domainstr.Count > 1 ? string.Format("[&,{0}]", string.Join(",", domainstr)) : domainstr.FirstOrDefault());
                //if (domainstr.Count > 1)
                //    Domain = string.Format("[&,{0}]", string.Join(",", domainstr));
                //else
                //    Domain = domainstr.FirstOrDefault();
            }
        }



        /// <summary>
        /// 关系
        /// </summary>
        /// <param name="relation"></param>
        /// <param name="relationmodel"></param>
        /// <param name="relationfield">设为Null自动设置，""表示没有</param>
        /// <param name="domain">过滤</param>
        public DomainField(FieldType relation, Type relationmodel, string relationfield, string domain)
        {
            Field_Type = relation;
            RelationModel = relationmodel.MetaName();
            RelationType = relationmodel;
            RelationField = relationfield;
            Domain = domain;
            if (relation == FieldType.one2many)
                IsPrimary = true;
        }
        public DomainField(FieldType relation, Type relationmodel, string domain)
            : this(relation, relationmodel, null, domain)
        {
        }
        public DomainField(FieldType relation, Type relationmodel)
            : this(relation, relationmodel, null, "")
        {
        }

        #endregion

        public void UpdateFrom(DomainField field)
        {
            Field_Type = field.Field_Type;
            Multiple = field.Multiple;
            FileSaveDataBase = field.FileSaveDataBase;
            HideNoPower = field.HideNoPower;
            IncUpdate = field.IncUpdate;
            Format = field.Format;
            TranslateValue = field.TranslateValue;
            TranslateValueResKey = field.TranslateValueResKey;
            UnInjectRelation = field.UnInjectRelation;
            IgnoreLogModify = field.IgnoreLogModify;
            Label = field.Label;
            AutoSearch = field.AutoSearch;
            Default = field.Default;
            Domain = field.Domain;
            Help = field.Help;
            Min = field.Min;
            Max = field.Max;
            dependence = field.dependence;
            MinLength = field.MinLength;
            Pattern = field.Pattern;
            Precision = field.Precision;

            NonPublic = field.NonPublic;
            Readonly = field.Readonly;
            Required = field.Required;
            Resource = field.Resource;
            AuthObj = field.AuthObj;
            Selection = field.Selection;
            Size = field.Size;
            m_validator = new Dictionary<string, Validator>(field.m_validator);
            DefaultDisplayText = field.DefaultDisplayText;


            RelationDisplay = field.RelationDisplay;
            RelationDisField = field.RelationDisField;
            RelationField = field.RelationField;
            RelationMode = field.RelationMode;
            RelationModel = field.RelationModel;
            RelationType = field.RelationType;
            MiddleTable = field.MiddleTable;
        }

        public DomainField Clone(bool fullcopy = false)
        {
            var field = new DomainField()
            {
                Name = Name,
                Field_Type = Field_Type,
                Multiple = Multiple,
                FileSaveDataBase = FileSaveDataBase,
                HideNoPower = HideNoPower,
                IncUpdate = IncUpdate,
                Format = Format,
                TranslateValue = TranslateValue,
                TranslateValueResKey = TranslateValueResKey,
                UnInjectRelation = UnInjectRelation,
                IgnoreLogModify = IgnoreLogModify,
                Label = Label,
                AutoSearch = AutoSearch,
                Default = Default,
                IgnoreSearchDefault = IgnoreSearchDefault,
                Domain = Domain,
                Help = Help,
                Min = Min,
                Max = Max,
                m_specific = m_specific,
                dependence = dependence,
                alldependence = alldependence,
                MinLength = MinLength,
                Pattern = Pattern,
                Precision = Precision,
                PropertyType = PropertyType,
                NonPublic = NonPublic,
                Readonly = Readonly,
                Required = Required,
                Resource = Resource,
                AuthObj = AuthObj,
                Selection = Selection,
                Size = Size,
                m_validator = new Dictionary<string, Validator>(m_validator),
                PrimaryField = PrimaryField,
                IsPrimary = IsPrimary,
                DefaultDisplayText = DefaultDisplayText,
                RelationDisplay = RelationDisplay,
                RelationDisField = RelationDisField,
                RelationField = RelationField,
                RelationMode = RelationMode,
                RelationModel = RelationModel,
                RelationType = RelationType,
                MiddleTable = MiddleTable
            };
            if (fullcopy)
                field.PropertyInfo = PropertyInfo;
            return field;
        }
    }
}
