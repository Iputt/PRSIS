using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Owl.Feature.iTemplate;
namespace Owl.Feature
{
    /// <summary>
    /// 字符串模板引擎
    /// </summary>
    public class Template : Engine<TemplateProvider, Template>
    {
        static TemplateProvider GetProviderWithFilepath(string filepath)
        {
            return Providers.FirstOrDefault(s => s.IsMatch(filepath));
        }
        /// <summary>
        /// 使用默认分隔符格式化模板
        /// </summary>
        /// <param name="template">模板内容</param>
        /// <param name="parameters">参数列表</param>
        /// <param name="providername">模板提供名称</param>
        /// <returns></returns>
        public static string Format(string template, IDictionary<string, object> parameters, string providername = "Common")
        {
            var provider = GetProvider(providername);
            if (provider == null)
                throw new Exception2("模板引擎 {0} 不存在，请使用有效的模板。", providername);
            return provider.Format(template, parameters);
        }
        /// <summary>
        /// 使用指定分隔符格式化模板
        /// </summary>
        /// <param name="template">模板内容</param>
        /// <param name="parameters">参数列表</param>
        /// <param name="providername">模板引擎名称</param>
        /// <param name="delimiterstart">起始分隔符</param>
        /// <param name="delimiterstop">结束分割符</param>
        /// <returns></returns>
        public static string Format(string template, IDictionary<string, object> parameters, string providername, string delimiterstart, string delimiterend)
        {
            var provider = GetProvider(providername);
            if (provider == null)
                throw new Exception2("模板引擎 {0} 不存在，请使用有效的模板。", providername);
            if (string.IsNullOrEmpty(delimiterstart))
                return provider.Format(template, parameters);
            return provider.Format(template, parameters, delimiterstart, delimiterend);
        }

        public static string ParseView(string viewcontent, object model, TransferObject viewdata, string filepath)
        {
            var provider = GetProviderWithFilepath(filepath);
            if (provider == null)
                throw new Exception2("文件 {0} 没有模板引擎可识别，请安装有效的模板引擎。", filepath);
            return provider.ParseView(viewcontent, model, viewdata);
        }

        /// <summary>
        /// 获取所有模板引擎
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<string> GetProviderNames()
        {
            return Providers.Select(s => s.Name);
        }
    }

}
