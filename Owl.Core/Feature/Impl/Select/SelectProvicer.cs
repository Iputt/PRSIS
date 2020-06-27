using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Domain;
using Owl.Const;
namespace Owl.Feature.Impl.Select
{
    /// <summary>
    /// 选择项提供者
    /// </summary>
    public abstract class SelectProvicer : Provider
    {
        string StripName(string name)
        {
            if (name.StartsWith("@"))
                return name.Substring(1);
            return name;
        }

        Dictionary<string, Func<string, ListOptionCollection>> m_funcs = new Dictionary<string, Func<string, ListOptionCollection>>();
        Dictionary<string, Func<string[], ListOptionCollection>> m_func2s = new Dictionary<string, Func<string[], ListOptionCollection>>();
        Dictionary<string, Func<string, string, ListOptionCollection>> m_func3s = new Dictionary<string, Func<string, string, ListOptionCollection>>();
        Dictionary<string, Func<string, string[], ListOptionCollection>> m_func4s = new Dictionary<string, Func<string, string[], ListOptionCollection>>();
        /// <summary>
        /// 注册不过滤内容列表处理函数
        /// </summary>
        /// <param name="selectname"></param>
        /// <param name="func"></param>
        protected void Register(string selectname, Func<string, ListOptionCollection> func)
        {
            if (!string.IsNullOrEmpty(selectname))
            {
                selectname = StripName(selectname);
                m_funcs[selectname] = func;
            }
        }
        /// <summary>
        /// 注册不过滤内容列表处理函数
        /// </summary>
        /// <param name="selectname"></param>
        /// <param name="func"></param>
        protected void Register2(string selectname, Func<string[], ListOptionCollection> func)
        {
            if (!string.IsNullOrEmpty(selectname))
            {
                selectname = StripName(selectname);
                m_func2s[selectname] = func;
            }
        }
        /// <summary>
        /// 注册过滤内容列表处理函数
        /// </summary>
        /// <param name="selectname"></param>
        /// <param name="func">第一个参数为内容过滤关键词，第二个参数为上级值</param>
        public void Register3(string selectname, Func<string, string, ListOptionCollection> func)
        {
            if (!string.IsNullOrEmpty(selectname))
            {
                selectname = StripName(selectname);
                m_func3s[selectname] = func;
            }
        }
        /// <summary>
        /// 注册过滤内容列表处理函数
        /// </summary>
        /// <param name="selectname"></param>
        /// <param name="func">第一个参数为内容过滤关键词，第二个参数为上级值</param>
        public void Register4(string selectname, Func<string, string[], ListOptionCollection> func)
        {
            if (!string.IsNullOrEmpty(selectname))
            {
                selectname = StripName(selectname);
                m_func4s[selectname] = func;
            }
        }
        public SelectProvicer()
        {
            Init();
        }

        protected virtual void Init() { }

        /// <summary>
        /// 获取选择项
        /// </summary>
        /// <param name="name">选择项名称</param>
        /// <param name="term"></param>
        /// <param name="topvalue">上级值</param>
        /// <returns></returns>
        public virtual ListOptionCollection GetSelect(string name, string term, string[] topvalue, bool all = false)
        {
            if (m_func4s.ContainsKey(name))
                return m_func4s[name](term, topvalue);
            if (m_func3s.ContainsKey(name))
                return m_func3s[name](term, topvalue.Length > 0 ? topvalue[0] : null);
            ListOptionCollection collection = null;
            if (m_func2s.ContainsKey(name))
                collection = m_func2s[name](topvalue);
            else if (m_funcs.ContainsKey(name))
                collection = m_funcs[name](topvalue.Length > 0 ? topvalue[0] : null);
            if (collection != null && !string.IsNullOrEmpty(term))
            {
                foreach (var item in collection.GetItems().ToList())
                {
                    if (!all && !item.Text.ToLower().Contains(term.ToLower()))
                        collection.RemoveOption(item);
                }
            }
            //if (collection == null)
            //    collection = new ListOptionCollection();
            return collection;
        }
    }

    internal class ModelSelectProvider : SelectProvicer
    {
        public override int Priority
        {
            get { return 1; }
        }
        static ListOptionCollection DoGet(bool hascommon, Func<DomainModel, bool> filter = null, params DomainType[] types)
        {
            var models = MetaEngine.GetModels().Where(s => !s.ModelType.IsAbstract);
            if (filter != null)
                models = models.Where(filter);
            if (types.Length > 0)
                models = models.Where(s => s.ObjType.In(types));
            var collection = new ListOptionCollection();
            if (hascommon)
                collection.AddOption(new ListItem("*", "common"));
            collection.AddOption(models.OrderBy(s => s.Name).Select(s => new ListItem(s.Name, string.Format("{0}:{1}", s.Name, s.Label))).ToArray());
            return collection;
        }
        static ListOptionCollection DoGet(params DomainType[] types)
        {
            return DoGet(true, null, types);
        }

        void BuildOptionWithEntity(ListOptionCollection collection, ModelMetadata modelmeta, string prefix)
        {
            foreach (var field in modelmeta.GetFields(t => t.Field_Type != FieldType.one2many && t.Field_Type != FieldType.many2many).OrderBy(t => t.Name))
            {
                var value = string.IsNullOrEmpty(prefix) ? field.Name : string.Format("{0}.{1}", prefix, field.Name);
                collection.AddOption(new ListItem(value, CoreConst.BuildOptionDisplay(value, field.Label)));
            }
            foreach (var field in modelmeta.GetEntityRelated())
            {
                BuildOptionWithEntity(collection, field.RelationModelMeta, string.IsNullOrEmpty(prefix) ? field.Name : string.Format("{0}.{1}", prefix, field.Name));
            }
        }

        protected override void Init()
        {
            Register(CoreConst.ModelList, s => DoGet());
            Register2(CoreConst.ModelForFieldTypeList, top =>
            {
                var fieldType = Util.EnumHelper.Parse<FieldType>(top[0]);
                var isentity = bool.Parse(top[1]);
                if (fieldType == FieldType.many2one || fieldType == FieldType.many2many || (fieldType == FieldType.one2many && !isentity))
                {
                    return DoGet(false, null, DomainType.AggRoot);
                }
                else if (isentity && fieldType == FieldType.one2many)
                {
                    return DoGet(false, s => s.State == MetaMode.Custom && s.TopModel == null, DomainType.AggRoot, DomainType.Entity);
                }
                return new ListOptionCollection();
            });
            Register(CoreConst.RootList, s => DoGet(DomainType.AggRoot));
            Register(CoreConst.CustomObjectTemplateList, t =>
            {
                var models = MetaEngine.GetModels().Where(s => s.Attrs != null && s.Attrs.OfType<CustomObjectTemplateAttribute>().Count() == 1);
                var collection = new ListOptionCollection();
                collection.AddOption(models.OrderBy(s => s.Attrs.OfType<CustomObjectTemplateAttribute>().FirstOrDefault().Ordinal).Select(s => new ListItem(s.Name, s.Attrs.OfType<CustomObjectTemplateAttribute>().FirstOrDefault().GetLabel())).ToArray());
                return collection;
            });
            Register(CoreConst.FormList, s => DoGet(DomainType.Form));
            Register(CoreConst.EventList, s => DoGet(DomainType.Handler));
            Register2(CoreConst.FieldList, s =>
            {
                var modelmeta = ModelMetadataEngine.GetModel(s[0]);
                if (modelmeta != null && s.Length > 1)
                {
                    var field = modelmeta.GetField(s[1]);
                    if (field is NavigatField)
                        modelmeta = (field as NavigatField).RelationModelMeta;
                    else
                        modelmeta = null;
                }
                if (modelmeta == null)
                    return new ListOptionCollection();

                //过滤不需要显示的字符串
                //foreach();
                return new ListOptionCollection(modelmeta.GetFields(t => !t.GetDomainField().IsInject).OrderBy(t => t.Name).Select(t => new ListItem(t.Name, CoreConst.BuildOptionDisplay(t.Label, t.Name))));
            });
            Register(CoreConst.FieldListWithEntity, s =>
            {
                var collection = new ListOptionCollection();
                var modelmeta = ModelMetadataEngine.GetModel(s);
                if (modelmeta != null)
                {
                    BuildOptionWithEntity(collection, modelmeta, "");
                }
                return collection;

            });
            Register("fieldlist2", s =>
            {
                var modelmeta = ModelMetadataEngine.GetModel(s);
                if (modelmeta == null)
                    return new ListOptionCollection();
                //过滤不需要显示的字符串
                //foreach();
                return new ListOptionCollection(modelmeta.GetFields().Select(t => new ListItem(string.Format("{0}.{1}", s, t.Name), CoreConst.BuildOptionDisplay(t.Label, t.Name))));
            });
            Register2(CoreConst.FieldValue, top =>
            {
                var result = new ListOptionCollection();
                var modelmeta = ModelMetadataEngine.GetModel(top[0]);
                if (modelmeta == null)
                    return result;
                var field = modelmeta.GetField(top[1]);
                if (field is SelectField)
                {
                    return (field as SelectField).GetItems();
                }
                else if (field is Many2OneField)
                {
                    var navfield = field as Many2OneField;
                    var selectors = new List<string>() { navfield.RelationField };
                    selectors.AddRange(navfield.RelationDisField);
                    var sortby = new SortBy();
                    foreach (var rsf in navfield.RelationDisField)
                    {
                        sortby[rsf] = SortOrder.Ascending;
                    }
                    var datas = Repository.FindAll(navfield.RelationModelMeta, navfield.Specific, sortby, 0, 0, selectors.ToArray());
                    result.AddOption(datas.Select(t => new ListItem(
                        string.Format("{0}", t[navfield.RelationField]),
                        string.Join(",", navfield.RelationDisField.Select(k => string.Format("{0}", t[k])).ToArray()))).ToArray());
                }
                return result;
            });
            Register(CoreConst.Status, s =>
            {
                if (string.IsNullOrEmpty(s))
                    return new ListOptionCollection();
                var meta = MetaEngine.GetModel(s);
                var field = meta.GetField("Status");
                if (field != null && field.Field_Type == FieldType.select)
                    return field.ListOptions;
                return null;
            });
        }
    }
}
