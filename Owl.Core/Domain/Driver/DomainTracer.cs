using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace Owl.Domain
{
    public enum ChangeStatus
    {
        NoChange = 0,
        Insert = 1,
        Update = 2,
        Remove = 3,
    }
    /// <summary>
    /// 对象变更
    /// </summary>
    public class ModelChange
    {
        public ModelChange()
        {
            Changes = new Dictionary<string, object>();
            Children = new Dictionary<string, List<ModelChange>>();
        }
        /// <summary>
        /// 对象标识符
        /// </summary>
        public Guid Key { get; set; }
        /// <summary>
        /// 对象外键
        /// </summary>
        public Guid ForignKey { get; set; }
        /// <summary>
        /// 变更作用对象
        /// </summary>
        public Entity Entity { get; set; }
        /// <summary>
        /// 变更方式
        /// </summary>
        public ChangeStatus Status { get; set; }
        /// <summary>
        /// 变更集
        /// </summary>
        public Dictionary<string, object> Changes { get; private set; }
        /// <summary>
        /// 子对象变更
        /// </summary>
        public Dictionary<string, List<ModelChange>> Children { get; set; }

        public TransferObject ToDict()
        {
            TransferObject result = new TransferObject(Changes);
            result.Remove("Id");
            result.Remove("Created");
            result.Remove("CreatedBy");
            result.Remove("Modified");
            result.Remove("ModifiedBy");
            if (ForignKey != Guid.Empty)
            {
                var key = result.FirstOrDefault(s => s.Value is Guid && (Guid)s.Value == ForignKey).Key;
                if (key != null)
                    result.Remove(key);
            }
            if (Children == null)
                return result;
            foreach (var pair in Children)
            {
                if (pair.Value == null || pair.Value.Count == 0)
                    continue;
                List<TransferObject> children = new List<TransferObject>();
                result[pair.Key] = children;
                foreach (var child in pair.Value)
                {
                    children.Add(child.ToDict());
                }
            }
            return result;
        }
    }

    public static class EntityTracer
    {
        static ModelChange GetRemove(Entity entity)
        {
            ModelChange change = new ModelChange() { Status = ChangeStatus.Remove, Key = entity.Id };
            ModelMetadata metadata = entity.Metadata;
            change.Entity = entity;
            object primary = entity.Id;
            foreach (One2ManyField field in metadata.GetEntityRelated())
            {
                change.Children[field.Name] = new List<ModelChange>();
                dynamic value = entity[field.Name];
                foreach (Entity ent in value)
                {
                    if (((IDataTracer)ent).IsLoaded)
                        change.Children[field.Name].Add(GetRemove(ent));
                }
                foreach (Entity ent in value.GetRemoved())
                {
                    change.Children[field.Name].Add(GetRemove(ent));
                }
            }
            return change;
        }

        static bool IsChildChange(ModelChange change)
        {
            foreach (var pair in change.Children)
            {
                foreach (var child in pair.Value)
                {
                    if (child.Status != ChangeStatus.NoChange || IsChildChange(child))
                        return true;
                }
            }
            return false;
        }

        static bool IsEqual(object ovalue, object dvalue)
        {
            if (((ovalue is string && (string)ovalue == "") || ovalue == null) && ((dvalue is string && (string)dvalue == "") || dvalue == null))
                return true;
            if (ovalue == null && dvalue == null)
                return true;
            if (dvalue is Enum && ovalue is string)
                dvalue = dvalue.ToString();
            return object.Equals(dvalue, ovalue);
        }
        static ModelChange BuildChanges(Entity parent, Entity data)
        {
            var original = ((IDataTracer)data).Original;
            if (original == null)
                original = new TransferObject();
            ModelChange change = new ModelChange();
            change.Entity = data;
            data.LastChanges = change;
            if (original.Count == 0)
                change.Status = ChangeStatus.Insert;
            else
                change.Status = ChangeStatus.Update;
            change.Key = data.Id;
            if (parent != null)
                change.ForignKey = parent.Id;
            bool ischange = false;
            foreach (var field in data.Metadata.GetFields())
            {
                var fieldname = field.GetFieldname();
                object value = data[fieldname];
                if (field is ScalerField || field is Many2OneField)
                {
                    if (data.IsLoaded && !original.ContainsKey(fieldname))
                        continue;
                    if (!IsEqual(original.ContainsKey(fieldname) ? original[fieldname] : null, value))
                    {
                        change.Changes[fieldname] = value;
                        if (data.Metadata.ObjType == DomainType.Entity ||
                            (data.Metadata.ObjType == DomainType.AggRoot &&
                            fieldname != "Created" && fieldname != "CreatedBy" && fieldname != "Modified" && fieldname != "ModifiedBy"))
                            ischange = true;
                        if (field.IncUpate)
                        {
                            change.Changes[fieldname] = GetInc(data, field.Name);
                        }
                    }
                }
                else if (field.IsEntityCollection)
                {
                    if (!change.Children.ContainsKey(fieldname))
                        change.Children[fieldname] = new List<ModelChange>();

                    foreach (Entity nvalue in (IEnumerable)value)
                    {
                        change.Children[fieldname].Add(BuildChanges(data, nvalue));
                    }
                    foreach (Entity remove in ((dynamic)value).GetRemoved())
                    {
                        change.Children[fieldname].Add(GetRemove(remove));
                    }
                }
            }
            if (change.Status == ChangeStatus.Update && !ischange && !IsChildChange(change))
                change.Status = ChangeStatus.NoChange;
            return change;
        }
        /// <summary>
        /// 构建变更集
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static ModelChange BuildChanges(this Entity entity)
        {
            return BuildChanges(null, entity);
        }

        public static ModelChange GetChanges(this Entity entity, bool remove = false)
        {
            if (entity.LastChanges == null)
                return remove ? GetRemove(entity) : BuildChanges(null, entity);
            return entity.LastChanges;
        }

        /// <summary>
        /// 获取字段的原始值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="root"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        public static T GetOrg<T>(this PersistObject root, string field)
        {
            var original = ((IDataTracer)root).Original;
            if (original != null && original.ContainsKey(field))
                return (T)original[field];
            return default(T);
        }

        static object GetInc(this PersistObject entity, string field)
        {
            var meta = entity.Metadata.GetField(field);
            if (meta == null)
                return 0;
            var original = ((IDataTracer)entity).Original;
            dynamic orgvalue = null;
            if (original != null && original.ContainsKey(field))
                orgvalue = Util.Convert2.ChangeType(original[field], meta.PropertyType);
            return (dynamic)entity[field] - orgvalue;
        }

        /// <summary>
        /// 判断给定的任一字段是否发生变化
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        public static bool IsChange(this PersistObject root, params string[] fields)
        {
            var original = ((IDataTracer)root).Original;
            var meta = root.Metadata;
            foreach (var field in fields)
            {
                var mfield = meta.GetField(field);
                if (mfield is ScalerField || mfield is Many2OneField)
                {
                    var fieldname = mfield.GetFieldname();
                    if (root.IsLoaded && !original.ContainsKey(fieldname))
                        continue;
                    var value = root[fieldname];
                    if (!IsEqual(original.ContainsKey(fieldname) ? original[fieldname] : null, value))
                        return true;
                }
            }
            return false;
        }
    }
}
