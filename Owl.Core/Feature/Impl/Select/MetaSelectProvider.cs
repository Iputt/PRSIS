using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Owl.Domain;
using Owl.Feature;
using Owl.Util;
namespace Owl.Domain
{
    /// <summary>
    /// 列表定义
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
    public sealed class SelectOptionAttribute : Attribute
    {
        /// <summary>
        /// 列表名称
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// 值
        /// </summary>
        public string Value { get; private set; }

        /// <summary>
        /// 文本
        /// </summary>
        public string Text { get; private set; }

        /// <summary>
        /// 序号
        /// </summary>
        public int Ordinal { get; private set; }

        /// <summary>
        /// 上级值
        /// </summary>
        public string[] TopValue { get; private set; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name">列表名称</param>
        /// <param name="value">值</param>
        /// <param name="text">文本</param>
        /// <param name="ordinal">序号</param>
        /// <param name="topvalue">上级值</param>
        public SelectOptionAttribute(string name, string value, string text, int ordinal, params string[] topvalue)
        {
            Name = name.Replace("@", "");
            Value = value;
            Text = text;
            Ordinal = ordinal;
            TopValue = topvalue;
        }
    }
    /// <summary>
    /// 列表包含
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
    public sealed class SelectContainAttribute : Attribute
    {
        /// <summary>
        /// 列表名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 包含的列表
        /// </summary>
        public IEnumerable<string> Part { get; set; }

        public SelectContainAttribute(string name, params string[] part)
        {
            Name = name.Replace("@", "");
            Part = part.Select(s => s.Replace("@", ""));
        }
    }
}
namespace Owl.Feature.Impl.Select
{

    public class MetaSelectProvider : SelectProvicer
    {
        public MetaSelectProvider()
        {
            AsmHelper.RegisterResource(LoadAsm, UnLoadAsm);
        }
        Dictionary<string, List<SelectOptionAttribute>> collections = new Dictionary<string, List<SelectOptionAttribute>>();
        Dictionary<string, List<string>> contains = new Dictionary<string, List<string>>();
        void LoadAsm(string asmname, Assembly assembly)
        {
            foreach (var attr in assembly.GetCustomAttributes(typeof(SelectOptionAttribute), false).Cast<SelectOptionAttribute>())
            {
                if (!collections.ContainsKey(attr.Name))
                    collections[attr.Name] = new List<SelectOptionAttribute>();
                collections[attr.Name].Add(attr);
            }
            foreach (var attr in assembly.GetCustomAttributes(typeof(SelectContainAttribute), false).Cast<SelectContainAttribute>())
            {
                if (!contains.ContainsKey(attr.Name))
                    contains[attr.Name] = new List<string>();
                contains[attr.Name].AddRange(attr.Part);
            }
        }
        void UnLoadAsm(string asmname, Assembly assembly)
        {
            foreach (var collect in collections.Values)
            {
                collect.RemoveAll(s => s.GetType().Assembly == assembly);
            }
        }
        protected override void Init()
        {
            base.Init();
        }
        bool IsEqu(string[] from, string[] to)
        {
            if (from == null)
                from = new string[0];
            if (to == null || to.Length == 1 && to[0] == "")
                to = new string[0];
            if (from.Length == 0 && to.Length == 0)
                return true;
            if (from.Length == to.Length)
            {
                for (var i = 0; i < from.Length; i++)
                {
                    if (from[i] != to[i])
                        return false;
                }
                return true;
            }
            return false;
        }
        public override ListOptionCollection GetSelect(string name, string term, string[] topvalue, bool all = false)
        {
            List<string> names = new List<string>();
            if (!string.IsNullOrEmpty(name))
                names.Add(name);
            if (contains.ContainsKey(name))
            {
                names.AddRange(contains[name].Distinct());
            }
            var list = new ListOptionCollection();
            foreach (var tmp in names)
            {
                if (collections.ContainsKey(tmp))
                {
                    foreach (var option in collections[tmp].Where(s => IsEqu(s.TopValue, topvalue)).OrderBy(s => s.Ordinal))
                    {
                        if (!string.IsNullOrEmpty(term) && !option.Text.ToLower().Contains(term.ToLower()))
                            continue;
                        list.AddOption(new ListItem(option.Value, option.Text));
                    }
                }
            }
            return list.Count == 0 ? null : list;
        }
        public override int Priority
        {
            get { return 10000; }
        }
    }
}
