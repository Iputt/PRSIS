using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Owl.Domain;
namespace Owl.Feature.iTemplate
{
    public abstract class TemplateProvider : Provider
    {
        /// <summary>
        /// 默认起始分隔符
        /// </summary>
        protected abstract string DelimiterStart { get; }

        /// <summary>
        /// 默认结束分隔符
        /// </summary>
        protected abstract string DelimiterStop { get; }

        /// <summary>
        /// 根据文件名称判断是否可以通过本模板解析
        /// </summary>
        /// <param name="filename">文件名称</param>
        /// <returns></returns>
        public abstract bool IsMatch(string filename);
        /// <summary>
        /// 使用默认分隔符格式化模板
        /// </summary>
        /// <param name="template">模板内容</param>
        /// <param name="parameters">参数列表</param>
        /// <returns></returns>
        public virtual string Format(string template, IDictionary<string, object> parameters)
        {
            return Format(template, parameters, DelimiterStart, DelimiterStop);
        }

        /// <summary>
        /// 使用指定分隔符格式化模板
        /// </summary>
        /// <param name="template">模板内容</param>
        /// <param name="parameters">参数列表</param>
        /// <param name="delimiterstart">起始分隔符</param>
        /// <param name="delimiterstop">结束分割符</param>
        /// <returns></returns>
        public abstract string Format(string template, IDictionary<string, object> parameters, string delimiterstart, string delimiterstop);

        public virtual string ParseView(string template, object model, TransferObject viewdata)
        {
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            parameters["Model"] = model;
            parameters["ViewData"] = viewdata;
            return Format(template, parameters);
        }
    }

    internal class CommonTemplateProvider : TemplateProvider
    {
        public override int Priority => 1;

        protected override string InnerName => "Common";

        protected override string DelimiterStart => "{{";

        protected override string DelimiterStop => "}}";

        public override bool IsMatch(string filepath)
        {
            var filename = System.IO.Path.GetFileNameWithoutExtension(filepath);
            return filename.EndsWith(".ct");
        }

        static Regex Regex = new Regex(@"^(\w+)(\[(\d{1,3})\])");
        public override string Format(string template, IDictionary<string, object> obj, string delimiterstart, string delimiterstop)
        {
            var pattern = string.Format(@"{0}([\w|\[|\]]+){1}", delimiterstart, delimiterstop);
            return Regex.Replace(template, pattern, s =>
            {
                var key = s.Groups[1].Value;
                if (obj != null)
                {
                    if (obj.ContainsKey(key))
                    {
                        if (obj[key] != null)
                            return obj[key].ToString();
                    }
                    else if (key.Contains("["))
                    {
                        var match = Regex.Match(key);
                        if (match.Success)
                        {
                            key = match.Groups[1].Value;
                            var index = int.Parse(match.Groups[3].Value);
                            if (obj.ContainsKey(key) && obj[key] is Array)
                            {
                                var tmp = obj[key] as Array;
                                if (tmp.Length > index)
                                    return tmp.GetValue(index).ToString();
                            }
                        }
                    }
                }
                return "";
            });
        }
    }
}
