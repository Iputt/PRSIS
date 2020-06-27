using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Feature;
using System.Collections;
using Owl.Util;
namespace Owl.Domain
{
    /// <summary>
    /// 列表项
    /// </summary>
    public abstract class ListOption
    {
        public abstract IEnumerable<ListItem> GetItems();

        /// <summary>
        /// 第一个元素
        /// </summary>
        public abstract ListItem First { get; }

        /// <summary>
        /// 所属组
        /// </summary>
        public ListItemGroup Group { get; set; }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public abstract TransferObject ToDict();
    }

    /// <summary>
    /// 列表元素
    /// </summary>
    public class ListItem : ListOption
    {
        /// <summary>
        /// 获取或设置一个值，该值指示是否选择此 SelectListItem。
        ///  如果选定此项，则为 true；否则为 false。
        /// </summary>
        public bool selected { get; set; }

        /// <summary>
        /// 获取或设置选定项的文本
        /// </summary>
        public string Text { get; private set; }

        /// <summary>
        /// 获取或设置选定项的值。
        /// </summary>
        public string Value { get; private set; }

        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; set; }

        TransferObject _extra;
        /// <summary>
        /// 额外的信息
        /// </summary>
        public TransferObject Extra
        {
            get
            {
                if (_extra == null)
                    _extra = new TransferObject(); return _extra;
            }
        }

        //public ListItem() { }

        public ListItem(string value, string text)
        {
            Text = text;
            Value = value;
        }
        public ListItem(Guid value, string text)
        {
            Text = text;
            Value = value.ToString().ToLower();
        }

        public override string ToString()
        {
            return string.Format("Value:\"{0}\",Text:\"{1}\",selected:{2}", Value, Text, selected ? "true" : "false");
        }

        public override IEnumerable<ListItem> GetItems()
        {
            return new List<ListItem>() { this };
        }
        public override ListItem First
        {
            get { return this; }
        }
        public override TransferObject ToDict()
        {
            TransferObject obj = new TransferObject();
            obj["text"] = Text;
            obj["value"] = Value;
            obj["description"] = Description;
            return obj;
        }
    }
    /// <summary>
    /// 列表元素组
    /// </summary>
    public class ListItemGroup : ListOption
    {
        /// <summary>
        /// 组名称
        /// </summary>
        public string Name { get; private set; }
        /// <summary>
        /// 组的元素集合
        /// </summary>
        public IEnumerable<ListOption> Options { get; private set; }

        public ListItemGroup(string name, IEnumerable<ListOption> opts)
        {
            if (opts == null)
                throw new ArgumentNullException("opts");
            Name = name;
            var options = new List<ListOption>();
            foreach (var opt in opts)
            {
                opt.Group = this;
                options.Add(opt);
            }
            Options = options;
        }
        public override IEnumerable<ListItem> GetItems()
        {
            return new List<ListItem>(Options.SelectMany(s => s.GetItems()));
        }

        public override ListItem First
        {
            get
            {
                var first = Options.FirstOrDefault();
                return first == null ? null : first.First;
            }
        }
        public override TransferObject ToDict()
        {
            var obj = new TransferObject();
            obj["group"] = Name;
            var items = new List<TransferObject>();
            foreach (var item in Options)
            {
                items.Add(item.ToDict());
            }
            obj["items"] = items;
            return obj;
        }
    }

    /// <summary>
    /// 列表元素集合
    /// </summary>
    public class ListOptionCollection : IEnumerable<ListOption>
    {
        bool m_isempty = true;
        /// <summary>
        /// 列表是否为空
        /// </summary>
        public bool IsEmpty { get { return m_isempty; } }

        List<ListOption> m_options = new List<ListOption>();
        /// <summary>
        /// 添加列表选项
        /// </summary>
        /// <param name="opt"></param>
        public void AddOption(params ListOption[] opt)
        {
            foreach (var item in opt)
            {
                m_options.Add(item);
                if (m_isempty)
                    m_isempty = item.First == null;
            }
        }
        public void RemoveOption(ListOption opt)
        {
            m_options.Remove(opt);
        }
        public ListOptionCollection() { }

        public ListOptionCollection(IEnumerable<ListOption> opts)
        {
            AddOption(opts.ToArray());
        }


        /// <summary>
        /// 获取本集合的所有元素
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ListItem> GetItems()
        {
            return m_options.SelectMany(s => s.GetItems());
        }
        /// <summary>
        /// 第一个元素
        /// </summary>
        public ListItem First
        {
            get
            {
                if (m_options.Count == 0)
                    return null;
                return m_options[0].First;
            }
        }


        /// <summary>
        /// 列表项数量
        /// </summary>
        public int Count { get { return m_options.Count; } }

        /// <summary>
        /// 合并集合
        /// </summary>
        /// <param name="target"></param>
        public void Merge(ListOptionCollection target)
        {
            if (target != null)
                AddOption(target.m_options.ToArray());
        }
        /// <summary>
        /// 获取选项值的序号
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public int GetIndex(string value)
        {
            for (int i = 0; i < m_options.Count; i++)
            {
                var opt = m_options[i] as ListItem;
                if (opt == null)
                    return -1;
                if (opt.Value == value)
                    return i;
            }
            return -1;
        }

        public IEnumerator<ListOption> GetEnumerator()
        {
            return m_options.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public List<TransferObject> ToDict()
        {
            List<TransferObject> results = new List<TransferObject>();
            foreach (var option in m_options)
            {
                results.Add(option.ToDict());
            }
            return results;
        }
        static ListOptionCollection()
        {
            m_enums = new Dictionary<string, Dictionary<string, DomainLabel>>();
        }
        static Dictionary<string, Dictionary<string, DomainLabel>> m_enums;
        protected static Dictionary<string, Dictionary<string, DomainLabel>> EnumLabels
        {
            get
            {
                if (m_enums == null)
                    m_enums = new Dictionary<string, Dictionary<string, DomainLabel>>();
                return m_enums;
            }
        }
        /// <summary>
        /// 从枚举中获取列表元素集合
        /// </summary>
        /// <param name="enumtype"></param>
        /// <returns></returns>
        public static ListOptionCollection FromEnum(Type enumtype, string filter = null)
        {
            enumtype = Nullable.GetUnderlyingType(enumtype) ?? enumtype;
            if (!enumtype.IsEnum)
                throw new Exception2("{0} is not a enum type!", enumtype.FullName);
            var resourcename = enumtype.FullName.ToLower();
            lock (enumtype)
            {
                if (!EnumLabels.ContainsKey(resourcename))
                {
                    var dict = new Dictionary<string, DomainLabel>();
                    var names = Enum.GetNames(enumtype);
                    foreach (var field in enumtype.GetFields())
                    {
                        if (names.Contains(field.Name))
                        {
                            var attrs = field.GetCustomAttributes(false);
                            var ignore = attrs.OfType<IgnoreFieldAttribute>().FirstOrDefault();
                            if (ignore != null)
                                continue;
                            var label = attrs.OfType<DomainLabel>().FirstOrDefault();
                            if (label == null)
                                label = new DomainLabel(field.Name);
                            label.Label = label.Label.Coalesce(field.Name);
                            if (string.IsNullOrEmpty(label.Resource))
                                label.Resource = string.Format("enumlabel.{0}.{1}", resourcename, field.Name.ToLower());
                            dict[field.Name] = label;
                        }
                    }
                    EnumLabels[resourcename] = dict;
                }
            }
            var filters = string.IsNullOrEmpty(filter) ? new HashSet<string>() : new HashSet<string>(filter.Split(','));
            var result = new ListOptionCollection();
            var tdict = EnumLabels[resourcename];
            foreach (var pair in tdict)
            {
                if (filters.Count == 0 || filters.Contains(pair.Key))
                {
                    result.AddOption(new ListItem(pair.Key, pair.Value.GetLabel()));
                }
            }
            return result;
            //var filters = string.IsNullOrEmpty(filter) ? new HashSet<string>() : new HashSet<string>(filter.Split(','));
            //if (!m_enumitems.ContainsKey(resourcename))
            //{
            //    List<ListItem> items = new List<ListItem>();
            //    var names = Enum.GetNames(enumtype);
            //    foreach (var field in enumtype.GetFields())
            //    {
            //        if (names.Contains(field.Name))
            //        {
            //            var label = field.GetCustomAttributes(typeof(DomainLabel), false).Cast<DomainLabel>().FirstOrDefault();
            //            items.Add(new ListItem(field.Name, label == null ? field.Name : Translation.Get(label.Resource.Coalesce(string.Format("enumlabel.{0}.{1}", resourcename, field.Name)), label.Label)));
            //        }
            //    }
            //    m_enumitems[enumtype.FullName] = items.AsEnumerable();
            //}
            //var trans = Translation.Get(resourcename, "");
            //var result = new ListOptionCollection();
            //if (string.IsNullOrEmpty(trans))
            //{
            //    foreach (var item in m_enumitems[enumtype.FullName])
            //    {
            //        if (filters.Count == 0 || filters.Contains(item.Value))
            //            result.AddOption(item);
            //    }
            //}
            //else
            //{
            //    foreach (var pair in trans.DeJson<Dictionary<string, object>>())
            //    {
            //        if (filters.Count == 0 || filters.Contains(pair.Value.ToString()))
            //            result.AddOption(new ListItem(pair.Key, pair.Key.ToString()));
            //    }
            //}
            //return result;
        }

        static IEnumerable<ListOption> FromDict(IDictionary<string, object> dto)
        {
            var collection = new List<ListOption>();
            foreach (var pair in dto)
            {
                if (pair.Value == null)
                    continue;
                if (pair.Value is IDictionary<string, object>)
                    collection.Add(new ListItemGroup(pair.Key, FromDict(pair.Value as IDictionary<string, object>)));
                else
                    collection.Add(new ListItem(pair.Key, pair.Value.ToString()));
            }
            return collection;
        }

        /// <summary>
        /// 从json中获取列表元素集合
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static ListOptionCollection FromJson(string json)
        {
            var collection = new ListOptionCollection();
            collection.AddOption(FromDict(json.DeJson<Dictionary<string, object>>()).ToArray());
            return collection;
        }
        Dictionary<string, string> m_ValueTextPairs;

        public string GetText(string value)
        {
            if (m_ValueTextPairs == null)
                m_ValueTextPairs = GetItems().xToDictionary(s => s.Value, s => s.Text);
            return m_ValueTextPairs.ContainsKey(value) ? m_ValueTextPairs[value] : value;
        }

        Dictionary<string, string> m_TextValuePairs;
        public string GetValue(string text)
        {
            if (m_TextValuePairs == null)
                m_TextValuePairs = GetItems().xToDictionary(s => s.Text, s => s.Value);
            return m_TextValuePairs.ContainsKey(text) ? m_TextValuePairs[text] : text;
        }
    }
}
