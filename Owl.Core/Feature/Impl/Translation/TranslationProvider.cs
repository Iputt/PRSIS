using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Domain;
namespace Owl.Feature.Impl.Translation
{
    public abstract class TranslationProvider : Provider
    {
        /// <summary>
        /// 翻译
        /// </summary>
        /// <param name="language">语言名称</param>
        /// <param name="reskey">资源名称</param>
        /// <param name="value">需翻译的资源值</param>
        /// <returns></returns>
        public abstract string Get(string language, string reskey, string value = "");

        public abstract TransferObject GetPL(string language);
    }
}
