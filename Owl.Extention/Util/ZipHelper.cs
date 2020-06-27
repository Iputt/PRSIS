using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using SharpCompress.Reader;
using SharpCompress.Common;
using SharpCompress.Archive.Zip;
using SharpCompress.Writer;

namespace Owl.Util
{
    public static class ZipHelper
    {
        /// <summary>
        /// 解压缩文件
        /// </summary>
        /// <param name="path"></param>
        /// <param name="bytes"></param>
        public static void Extract(string path, byte[] bytes)
        {
            using (var stream = new MemoryStream(bytes))
            {
                var reader = ReaderFactory.Open(stream);
                while (reader.MoveToNextEntry())
                {
                    if (!reader.Entry.IsDirectory)
                    {
                        reader.WriteEntryToDirectory(path, ExtractOptions.ExtractFullPath | ExtractOptions.Overwrite);
                    }
                }
            }
        }
        /// <summary>
        /// 压缩文件
        /// </summary>
        /// <param name="destfile">压缩目标文件</param>
        /// <param name="entries">参与压缩项</param>
        public static void Compress(string destfile, params string[] entries)
        {
            using (Stream stream = File.OpenWrite(destfile))
            {
                using (var writer = WriterFactory.Open(stream, ArchiveType.Zip, CompressionType.BZip2))
                {
                    foreach (var entry in entries)
                    {
                        writer.Write(entry, new FileInfo(entry));
                    }
                }
            }
        }

        public static void CompressAll(string destfile, string directory)
        {
            using (Stream stream = File.OpenWrite(destfile))
            {
                using (var writer = WriterFactory.Open(stream, ArchiveType.Zip, CompressionType.BZip2))
                {
                    writer.WriteAll(directory);
                }
            }
        }
    }
}
