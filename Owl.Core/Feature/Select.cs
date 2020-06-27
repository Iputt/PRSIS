using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Owl.Domain;
using Owl.Util;
using Owl.Feature.Impl.Select;

namespace Owl.Feature
{
    public abstract class SelectContext
    {
        /// <summary>
        /// 对象名
        /// </summary>
        public string ModelName { get; set; }

        /// <summary>
        /// 是否隐藏 code
        /// </summary>
        public bool? HideCode { get; set; }

        protected static readonly string currentkey = "owl.feature.selectcontext.current";
        /// <summary>
        /// 当前select上下文
        /// </summary>
        public static SelectContext Current
        {
            get { return Cache.Thread<SelectContext>(currentkey, () => new GeneralSelectContext()); }
            set { Cache.Thread(currentkey, value); }
        }
    }
    internal class GeneralSelectContext : SelectContext
    {

    }
    public abstract class SelectContext<T> : SelectContext
        where T : SelectContext<T>, new()
    {
        /// <summary>
        /// 当前select上下文
        /// </summary>
        public static new T Current
        {
            get
            {
                var current = Cache.Thread<SelectContext>(currentkey);
                if (current == null || current is GeneralSelectContext)
                {
                    var tmp = new T();
                    if (current != null)
                    {
                        tmp.ModelName = current.ModelName;
                        tmp.HideCode = current.HideCode;
                    }
                    Cache.Thread(currentkey, tmp);
                    return tmp;
                }
                return current as T;
            }
            set { Cache.Thread(currentkey, value); }
        }
    }



    /// <summary>
    /// 选择项帮助
    /// </summary>
    public class Select
    {
        string StripName(string name)
        {
            if (name.StartsWith("@"))
                return name.Substring(1);
            return name;
        }
        /// <summary>
        /// 获取选择项
        /// </summary>
        /// <param name="modelname">当前上下文的对象名称</param>
        /// <param name="name">名称</param>
        /// <param name="term">显示文本中包含</param>
        /// <param name="topvalue">上级值</param>
        /// <returns></returns>
        public static ListOptionCollection GetSelect(string modelname, string name, string[] topvalue = null, string term = null, bool all = false)
        {
            if (!string.IsNullOrEmpty(modelname))
                SelectContext.Current.ModelName = modelname;
            return SelectEngine.GetSelect(name, term, topvalue ?? new string[0], all);
        }
        static readonly string key = "owl.feature.select.options.key";
        protected static Dictionary<string, string> Options
        {
            get
            {
                return Cache.Thread<Dictionary<string, string>>(key, () => new Dictionary<string, string>());
            }
        }
        /// <summary>
        /// 根据值获取显示名称
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="topvalue"></param>
        /// <returns></returns>
        public static string GetText(string modelname, string name, string value, params string[] topvalue)
        {
            if (string.IsNullOrEmpty(value))
                return value;
            string key = string.Format("{0}___{1}___{2}", name, value, string.Join(",", topvalue));
            var cache = Options;
            string result = value;
            if (cache.ContainsKey(key))
                result = cache[key];
            else
            {
                var list = GetSelect(modelname, name, topvalue);
                if (list != null)
                {
                    foreach (var item in list.GetItems())
                    {
                        if (!string.IsNullOrEmpty(item.Value))
                        {
                            key = string.Format("{0}___{1}___{2}", name, item.Value, string.Join(",", topvalue));
                            var text = item.Text;
                            if (item.Group != null)
                                text = item.Group.Name + "-" + text;
                            cache[key] = text;
                            if (value == item.Value)
                                result = text;
                        }
                    }
                }
            }
            return result.Coalesce(value);

        }
        /// <summary>
        /// 根据显示名称获取值
        /// </summary>
        /// <param name="name"></param>
        /// <param name="text"></param>
        /// <param name="topvalue"></param>
        /// <returns></returns>
        public static string GetValue(string modelname, string name, string text, params string[] topvalue)
        {
            if (string.IsNullOrEmpty(text))
                return text;
            string key = string.Format("{0}___{1}___{2}", name, text, string.Join(",", topvalue));
            var cache = Options;
            string result = text;
            if (cache.ContainsKey(key))
                result = cache[key];
            else
            {
                var list = GetSelect(modelname, name, topvalue);
                if (list != null)
                {
                    foreach (var item in list.GetItems())
                    {
                        if (!string.IsNullOrEmpty(item.Value))
                        {
                            key = string.Format("{0}___{1}___{2}", name, item.Text, string.Join(",", topvalue));
                            cache[key] = item.Value;
                            if (text == item.Text)
                                result = item.Value;
                        }
                    }
                }
            }
            return result.Coalesce(text);
        }

        /// <summary>
        /// 获取上一个选择项的值
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="topvalue"></param>
        /// <returns></returns>
        public static string GetLast(string modelname, string name, string value, params string[] topvalue)
        {
            var last = value;
            var list = GetSelect(modelname, name, topvalue);
            if (list != null)
            {
                foreach (var item in list.GetItems())
                {
                    if (value == item.Value)
                        break;
                    last = item.Value;
                }
            }
            return last;
        }

        public static int GetIndex(string modelname, string name, string value, params string[] topvalue)
        {
            var index = 0;
            var list = GetSelect(modelname, name, topvalue);
            if (list != null)
            {
                foreach (var item in list.GetItems())
                {
                    if (value == item.Value)
                        break;
                    index = index + 1;
                }
            }
            return index;
        }

    }
}
