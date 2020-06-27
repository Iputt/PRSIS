using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Owl.Util.Impl.FileHelper
{
    /// <summary>
    /// 内容类型
    /// </summary>
    internal class ContentType : Engine<ContentTypeProvider, ContentType>
    {
        /// <summary>
        /// 根据文件扩展名获取内容类型
        /// </summary>
        /// <param name="extention"></param>
        /// <returns></returns>
        public static string GetType(string extention)
        {
            if (string.IsNullOrEmpty(extention))
                return "text/plain";
            return Execute2<string, string>(s => s.GetType, extention, _default: "text/plain");
        }
    }
}
