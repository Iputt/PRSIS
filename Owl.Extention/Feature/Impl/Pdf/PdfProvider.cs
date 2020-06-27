using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Domain;
using System.IO;
namespace Owl.Feature.iPdf
{
    public abstract class PdfProvider : Provider
    {
        /// <summary>
        /// 从url生成pdf并保存为指定的文件
        /// </summary>
        /// <param name="url">url</param>
        /// <param name="savepath">文件路径</param>
        public abstract void FromUrl(string url, string savepath);

        /// <summary>
        /// 从url生成pdf并保存在内存中
        /// </summary>
        /// <param name="url">url</param>
        /// <param name="stream">流</param>
        public abstract void FromUrl(string url, Stream stream);
    }

    public sealed class NonePdfProvider : PdfProvider
    {

        public override void FromUrl(string url, string savepath)
        {

        }

        public override void FromUrl(string url, Stream stream)
        {

        }

        public override int Priority
        {
            get { return 1; }
        }
    }
}
