using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Collections;
using Owl.Util;
using System.Diagnostics;

namespace Owl.Domain
{
    /// <summary>
    /// 领域对象基类
    /// </summary>
    [DomainModel]
    public abstract class DomainObject : PropertyIndexer
    {
        #region
        ModelMetadata m_metadata;

        protected virtual ModelMetadata GetMeta()
        {
            return ModelMetadataEngine.GetModel(GetType().MetaName());
        }
        /// <summary>
        /// 对象元数据
        /// </summary>
        [IgnoreField]
        public virtual ModelMetadata Metadata
        {
            get
            {
                if (m_metadata == null)
                    m_metadata = GetMeta();
                return m_metadata;
            }
            internal set
            {
                m_metadata = value;
            }
        }


        [IgnoreField]
        public override IEnumerable<string> Keys
        {
            get { return m_metadata.GetFields().Select(s => s.GetFieldname()); }
        }

        public override bool ContainsKey(string key)
        {
            if (m_metadata.ContainField(key))
                return true;
            return m_metadata.GetFields().Any(s => s.GetFieldname() == key);
        }
        /// <summary>
        /// 判断字段的值是否为默认值
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool IsDefault(string key)
        {
            var field = m_metadata.GetField(key);
            if (field == null)
                throw new Exception2("字段{0}不存在", key);
            return ObjectExt.Compare(this[key], TypeHelper.Default(field.PropertyType)) == 0;
        }
        #endregion


        public override void Write(Object2 dto)
        {
            Write(dto, true, false);
        }
        public void WriteAllowCopy(Object2 dto)
        {
            Write(dto, true, true);
        }
        public void WriteWithValidate(Object2 dto)
        {
            Write(dto, true, false, true);
        }
        public void WriteIgnoreCase(Object2 dto)
        {
            Write(dto, true, false, false, true);
        }
        protected object HandlePrimaryKey(bool renewprimary, Object2 dto)
        {
            var primaryfield = Metadata.PrimaryField;
            var keyfield = primaryfield == null ? null : primaryfield.Name;
            object key = null;
            if (keyfield != null)
            {
                var ckey = this[keyfield];
                if (renewprimary || object.Equals(ckey, TypeHelper.Default(primaryfield.PropertyType)))
                {
                    key = xObjectHelper.FitType(renewprimary ? null : dto[keyfield], primaryfield.PropertyType);
                    this[keyfield] = key;
                }
                else
                    key = ckey;
            }
            return key;
        }
        protected void Write(Object2 dto, bool istoplevel, bool renewprimary = false, bool validate = false, bool ignorecase = false)
        {
            var primaryfield = Metadata.PrimaryField;
            var keyfield = primaryfield == null ? null : primaryfield.Name;
            var key = HandlePrimaryKey(istoplevel ? false : renewprimary, dto);
            //#region 处理主键
            //var primaryfield = Metadata.PrimaryField;
            //var keyfield = primaryfield == null ? null : primaryfield.Name;
            //object key = null;
            //if (keyfield != null)
            //{
            //    var ckey = this[keyfield];
            //    if (newkey || object.Equals(ckey, TypeHelper.Default(primaryfield.PropertyType)))
            //    {
            //        key = xObjectHelper.FitType(newkey ? null : dto[keyfield], primaryfield.PropertyType);
            //        this[keyfield] = key;
            //    }
            //    else
            //        key = ckey;
            //}
            //#endregion
            foreach (var field in Metadata.GetFields())
            {
                if (field.Field_Type == FieldType.many2many || field.Name == keyfield)
                    continue;
                #region 取值
                dynamic value = null;
                var dtokey = "";
                var fieldname = field.GetFieldname();
                if (dto.ContainsKey(fieldname))
                    dtokey = fieldname;
                else if (dto.ContainsKey(field.Name))
                    dtokey = field.Name;

                else if (ignorecase)
                    dtokey = dto.Keys.FirstOrDefault(s => s.ToLower() == field.Name.ToLower() || s.ToLower() == field.GetFieldname().ToLower());

                if (!string.IsNullOrEmpty(dtokey))
                    value = dto[dtokey];
                else
                    continue;

                if (value is string)
                    value = value.Trim();
                #endregion
                if (field.Field_Type == FieldType.one2many)
                {
                    One2ManyField rfield = (One2ManyField)field;
                    if (value == null || rfield.RelationModelMeta == null || !rfield.IsEntityCollection)
                        continue;

                    object lvalues = value;
                    if (value is string)
                        lvalues = JsonHelper.DeJson<List<TransferObject>>(value);
                    if (lvalues == null)
                        continue;
                    #region 导航字段赋值
                    Dictionary<object, Object2> list = new Dictionary<object, Object2>();
                    List<Entity> removing = new List<Entity>();
                    var rpkey = rfield.RelationModelMeta.PrimaryField;
                    foreach (var rec in (IEnumerable)lvalues)
                    {
                        Object2 record = rec as Object2;
                        if (record == null)
                            record = new TransferObject(rec as IDictionary<string, object>);
                        list[xObjectHelper.FitType(record[rpkey.Name], rpkey.PropertyType)] = record;
                    }
                    EntityCollection real = this[field.Name] as EntityCollection;
                    foreach (Entity v in real)
                    {
                        var lid = v[rpkey.Name];
                        if (list.ContainsKey(lid))
                        {
                            v.Write(list[lid]);
                            v[rfield.RelationField] = key;
                            list.Remove(lid);
                        }
                        else
                        {
                            removing.Add(v);
                        }
                    }
                    foreach (var entity in removing)
                    {
                        real.Remove(entity);
                    }
                    foreach (Object2 dict3 in list.Values)
                    {
                        Entity obj = DomainFactory.Create<Entity>(rfield.RelationModelMeta);
                        obj.Write(dict3, false, renewprimary, validate);
                        obj[rfield.RelationField] = key;
                        real.Add(obj);
                    }
                    #endregion
                }
                else
                {
                    #region 非导航字段赋值
                    object tmp = value;
                    if (value is IEnumerable && !(value is string) && field.Field_Type != FieldType.binary)
                    {
                        if (field is Many2OneField || field is SelectField || field.GetDomainField().TranslateValue)
                            tmp = value[0];
                        else
                            tmp = string.Join(",", value);
                    }
                    if (validate)
                        field.Validate(tmp, dto);
                    this[fieldname] = tmp;
                    #endregion
                }
            }
        }
        protected override TransferObject _Read(bool fordisplay)
        {
            TransferObject dto = new TransferObject();
            dto.__ModelName__ = Metadata.Name;
            foreach (var field in Metadata.GetFields())
            {
                if (field.Field_Type == FieldType.many2many)
                    continue;
                string fieldname = field.GetFieldname();
                object value = this[fieldname];
                if (field.Field_Type == FieldType.one2many)
                {
                    var rfield = (NavigatField)field;
                    if (rfield.IsEntityCollection)
                    {
                        List<TransferObject> tdtos = new List<TransferObject>();
                        foreach (DomainObject v in (IEnumerable)value)
                        {
                            if (v != null)
                            {
                                var rdto = v.Read(fordisplay);
                                if (!string.IsNullOrEmpty(rfield.RelationField))
                                    rdto[rfield.RelationField] = this["Id"];
                                tdtos.Add(rdto);
                            }
                        }
                        dto[fieldname] = tdtos;
                    }
                }
                else
                {
                    if (field.Field_Type == FieldType.select)
                        value = string.Format("{0}", value);
                    dto[fieldname] = value;
                    if (fordisplay)
                    {
                        if (field.Field_Type == FieldType.select || field.Field_Type == FieldType.many2one)
                            dto[field.Name] = new object[] { value, GetDisplay(field.Name) };
                        else
                            dto[field.Name] = GetDisplay(field.Name);
                    }
                }
            }
            return dto;
        }

        public void Validate()
        {
            Metadata.Validate(this);
        }
        #region 引用相关
        /// <summary>
        /// 获取调用当前方法的字段名称
        /// </summary>
        /// <returns></returns>
        protected string GetPropertyName()
        {
            var fname = new StackFrame(2).GetMethod().Name;
            return fname.Substring(4);
        }

        Dictionary<string, object> relateds;
        /// <summary>
        /// 关系
        /// </summary>
        [IgnoreField]
        protected Dictionary<string, object> m_Relateds
        {
            get
            {
                if (relateds == null)
                    relateds = new Dictionary<string, object>(15);
                return relateds;
            }
        }


        /// <summary>
        /// 获取指定字段的聚合根引用
        /// </summary>
        /// <typeparam name="TRoot"></typeparam>
        /// <param name="field"></param>
        /// <returns></returns>
        protected TRoot GetReference<TRoot>(string field)
            where TRoot : AggRoot
        {
            AggregateReference<TRoot> aroot = null;
            if (!m_Relateds.ContainsKey(field))
            {
                aroot = new AggregateReference<TRoot>(this, field);
                m_Relateds[field] = aroot;
            }
            else
                aroot = ((AggregateReference<TRoot>)m_Relateds[field]);
            return aroot.Value;
        }
        /// <summary>
        /// 获取当前字段的聚合根引用
        /// </summary>
        /// <typeparam name="TRoot"></typeparam>
        /// <returns></returns>
        protected TRoot GetReference<TRoot>()
            where TRoot : AggRoot
        {
            return GetReference<TRoot>(GetPropertyName());
        }

        /// <summary>
        /// 设置当前字段的聚合根引用
        /// </summary>
        /// <typeparam name="TRoot"></typeparam>
        /// <param name="value"></param>
        protected void SetReference<TRoot>(TRoot value)
            where TRoot : AggRoot
        {
            SetReference<TRoot>(GetPropertyName(), value);
        }

        /// <summary>
        /// 设置制定字段的聚合根引用
        /// </summary>
        /// <typeparam name="TRoot"></typeparam>
        /// <param name="field"></param>
        /// <param name="value"></param>
        protected void SetReference<TRoot>(string field, TRoot value)
            where TRoot : AggRoot
        {
            if (!m_Relateds.ContainsKey(field))
                m_Relateds[field] = new AggregateReference<TRoot>(this, field);
            ((AggregateReference<TRoot>)m_Relateds[field]).Value = value;
        }

        /// <summary>
        /// 获取实体集合
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <returns></returns>
        protected EntityCollection<TEntity> GetEntities<TEntity>()
           where TEntity : Entity
        {
            return GetEntities<TEntity>(GetPropertyName());
        }

        /// <summary>
        /// 获取实体集合
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="field"></param>
        /// <returns></returns>
        protected EntityCollection<TEntity> GetEntities<TEntity>(string field)
            where TEntity : Entity
        {
            if (!m_Relateds.ContainsKey(field))
                m_Relateds[field] = new EntityCollection<TEntity>(this);
            return (EntityCollection<TEntity>)m_Relateds[field];
        }

        /// <summary>
        /// 设置关系的值
        /// </summary>
        /// <param name="field"></param>
        /// <param name="value"></param>
        protected virtual void SetRelation(Many2OneField field, AggRoot value)
        {
            Util.ExprHelper.SetReference(field.RelationModelMeta.ModelType).FaseInvoke(this, field.Name, value);
        }

        protected virtual object GetRelation(NavigatField field)
        {
            if (field.IsEntityCollection)
            {
                return Util.ExprHelper.GetEntities(field.RelationModelMeta.ModelType).FaseInvoke(this, field.Name);
            }
            else if (field.Field_Type == FieldType.many2one)
            {
                return Util.ExprHelper.GetReference(field.RelationModelMeta.ModelType).FaseInvoke(this, field.Name);
            }
            return null;
        }

        #endregion
        TransferObject m_innerdict;
        /// <summary>
        /// 内置字典，用于字段不存在时的赋值，或值得暂存
        /// </summary>
        protected TransferObject InnerDict
        {
            get
            {
                if (m_innerdict == null)
                {
                    m_innerdict = new TransferObject();
                }
                return m_innerdict;
            }
        }


        protected override void SetValue(string property, object value)
        {
            if (Instance.ContainsKey(property))
                base.SetValue(property, value);
            else if (Metadata.ContainField(property))
            {
                var field = Metadata.GetField(property);
                if (field == null || field.Field_Type == FieldType.many2many || field.Field_Type == FieldType.one2many)
                    throw new ArgumentOutOfRangeException();

                if (field.Field_Type == FieldType.many2one)
                {
                    Many2OneField navfield = (Many2OneField)field;
                    if (navfield.PrimaryField == property)
                        InnerDict[property] = Convert2.ChangeType(value, navfield.PrimaryType);
                    else
                        SetRelation(navfield, (AggRoot)value);
                }
                else
                    InnerDict[property] = Convert2.ChangeType(value, field.PropertyType);
            }
            else
                InnerDict[property] = value;
        }
        protected override object GetValue(string property)
        {
            if (Instance.ContainsKey(property))
                return base.GetValue(property);
            if (Metadata.ContainField(property))
            {
                var field = Metadata.GetField(property);
                if (field == null)
                    throw new ArgumentOutOfRangeException();
                if (field is NavigatField)
                {
                    var navfield = (NavigatField)field;
                    if (field.Field_Type == FieldType.many2one && field.GetFieldname() == property)
                        return InnerDict[property];
                    return GetRelation(navfield);
                }
                if (!InnerDict.ContainsKey(property))
                    InnerDict[property] = TypeHelper.Default(field.PropertyType);
            }
            return InnerDict[property];
        }

        protected override string _GetDisplay(string key)
        {
            var field = Metadata.GetField(key);
            if (field != null)
            {
                if (field.Field_Type == FieldType.many2one && field.Name != field.GetFieldname())
                {
                    Object2 obj = this[field.Name] as Object2;
                    if (obj == null)
                        return "";
                    return string.Join(",", (field as Many2OneField).RelationDisField.Select(s => obj.GetDisplay(s)));
                }
                if (field.Field_Type == FieldType.date && this[key] != null)
                    return ((DateTime)this[key]).ToString("yyyy-MM-dd");
                if (field.Field_Type == FieldType.datetime && this[key] != null)
                    return ((DateTime)this[key]).ToString("yyyy-MM-dd HH:mm:ss");
                if ((field.Field_Type == FieldType.digits || field.Field_Type == FieldType.number) && field.GetDomainField() != null)
                    return this[key] == null ? null : string.Format(field.Format.Coalesce("{0:N" + field.GetDomainField().Precision + "}"), this[key]);
                return field.GetDisplay(this[field.GetFieldname()], this);
            }
            return base._GetDisplay(key);
        }
        protected override object _GetRealValue(string key)
        {
            var field = Metadata.GetField(key);
            if (field != null)
            {
                return this[field.GetFieldname()];
            }
            return base._GetRealValue(key);
        }
    }
    /// <summary>
    /// 动态对象
    /// </summary>
    public class DynamicModel : System.Dynamic.DynamicObject
    {

        public static dynamic Create(DomainObject obj)
        {
            return new DynamicModel(obj);
        }

        DomainObject m_obj;
        private DynamicModel(DomainObject obj)
        {
            m_obj = obj;
        }

        public dynamic GetValue(string property)
        {
            object result = null;
            if (m_obj.ContainsKey(property))
            {
                var field = m_obj.Metadata.GetField(property);
                result = m_obj[property];
                if (result != null && field.IsNavigate)
                {
                    NavigatField navfield = field as NavigatField;
                    if (navfield.Field_Type != FieldType.many2one || (navfield.Field_Type == FieldType.many2one && navfield.PrimaryField != property))
                    {
                        if (result is DomainObject)
                            result = DynamicModel.Create(result as DomainObject);
                        else
                        {
                            List<dynamic> res = new List<dynamic>();
                            foreach (DomainObject obj in (IEnumerable)result)
                            {
                                res.Add(DynamicModel.Create(obj));
                            }
                            result = res;
                        }
                    }
                }
            }
            return result;
        }

        public override bool TrySetMember(System.Dynamic.SetMemberBinder binder, object value)
        {
            if (m_obj.ContainsKey(binder.Name))
            {
                m_obj[binder.Name] = value;
                return true;
            }
            return false;
        }
        public override bool TryGetMember(System.Dynamic.GetMemberBinder binder, out object result)
        {
            if (m_obj.ContainsKey(binder.Name))
            {
                result = GetValue(binder.Name);
                return true;
            }
            result = null;
            return false;
        }

        public override bool TryGetIndex(System.Dynamic.GetIndexBinder binder, object[] indexes, out object result)
        {
            result = null;
            if (binder.CallInfo.ArgumentCount == 1)
            {
                string index = indexes[0] as string;
                if (m_obj.ContainsKey(index))
                {
                    result = GetValue(index);
                    return true;
                }
            }
            return false;
        }
        public override bool TrySetIndex(System.Dynamic.SetIndexBinder binder, object[] indexes, object value)
        {
            if (binder.CallInfo.ArgumentCount == 1)
            {
                string index = indexes[0] as string;
                if (m_obj.ContainsKey(index))
                {
                    m_obj[index] = value;
                    return true;
                }
            }
            return false;
        }

        public override bool TryInvokeMember(System.Dynamic.InvokeMemberBinder binder, object[] args, out object result)
        {
            try
            {
                var method = m_obj.Metadata.ModelType.GetMethod(binder.Name);
                result = method.FaseInvoke(m_obj, args);
                return true;
            }
            catch
            {
                result = null;
                return false;
            }
        }
    }

}
