using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Feature.Impl.Translation;
namespace Owl.Feature
{
    /// <summary>
    /// 翻译
    /// </summary>
    public class Translation : Engine<TranslationProvider, Translation>
    {
        /// <summary>
        /// 获取指定语言的翻译结果
        /// </summary>
        /// <param name="language">语言</param>
        /// <param name="name">翻译名称</param>
        /// <param name="content">待翻译的内容</param>
        /// <returns>翻译结果</returns>
        static string _Get(string language, string name, string content = "", bool usecontentonfailed = false)
        {
            string result = null;
            if (!string.IsNullOrEmpty(name))
            {
                result = Execute2<string, string, string, string>(s => s.Get, language, name, content);
                if (result == content || string.IsNullOrEmpty(result))
                    result = null;
            }
            if (usecontentonfailed && result == null && !string.IsNullOrEmpty(content))
                result = content;
            return result;
        }

        /// <summary>
        /// 获取当前语言的翻译结果
        /// </summary>
        /// <param name="name">名称</param>
        /// <param name="content">待翻译的内容</param>
        /// <param name="usecontentonfailed">当翻译失败时使用未翻译的内容</param>
        /// <returns>翻译结果</returns>
        public static string Get(string name, string content = "", bool usecontentonfailed = true)
        {
            return _Get(OwlContext.Current.Language, name, content, usecontentonfailed);
        }
        /// <summary>
        /// 获取前端的翻译
        /// </summary>
        /// <returns></returns>
        public static TransferObject GetPL()
        {
            return Execute2<string, TransferObject>(s => s.GetPL, OwlContext.Current.Language);
        }
    }
}
