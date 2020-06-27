using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Domain;
using Owl.Feature.iPdf;
using System.IO;
namespace Owl.Feature
{
    public class Pdf : Engine<PdfProvider, Pdf>
    {
        protected override EngineMode Mode
        {
            get
            {
                return EngineMode.Single;
            }
        }
        protected override bool SkipException
        {
            get
            {
                return false;
            }
        }
        /// <summary>
        /// 从url生成pdf并保存为指定的文件
        /// </summary>
        /// <param name="url">url</param>
        /// <param name="savepath">文件路径</param>
        public static void FromUrl(string url, string savepath)
        {
            Execute(s => s.FromUrl, url, savepath);
        }

        /// <summary>
        /// 从url生成pdf并保存在内存中
        /// </summary>
        /// <param name="url">url</param>
        /// <param name="stream">流</param>
        public static void FromUrl(string url, Stream stream)
        {
            Execute(s => s.FromUrl, url, stream);
        }
    }
}
