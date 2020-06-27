using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
namespace Owl.Util
{
    public class FileHelper
    {
        public static readonly string UploadFolder = Path.Combine(AppConfig.Section.ResPath, "Upload") + Path.DirectorySeparatorChar;
        public static readonly string TemplateFolder = Path.Combine(AppConfig.Section.ResPath, "Template") + Path.DirectorySeparatorChar;
        public static readonly string MetaFolder = Path.Combine(AppConfig.Section.ResPath, "Meta") + Path.DirectorySeparatorChar;
        /// <summary>
        /// 生成文件路径
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static string GetFilePath(string filename)
        {
            if (!string.IsNullOrEmpty(Path.GetPathRoot(filename)))
            {
                return filename;
            }
            string uploadFolder = Path.Combine(UploadFolder, DateTime.Today.ToString("yyyy-MM-dd"), "");
            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }
            return Path.Combine(uploadFolder, filename);
        }

        public static string GetDirectoryPath(string directoryname)
        {
            string uploadFolder = Path.Combine(UploadFolder, DateTime.Today.ToString("yyyy-MM-dd"), "");
            //if (!Directory.Exists(uploadFolder))
            //{
            //    Directory.CreateDirectory(uploadFolder);
            //}
            var directory = Path.Combine(uploadFolder, directoryname);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            return directory;
        }

        /// <summary>
        /// 获取文件的逻辑路径
        /// </summary>
        /// <param name="fielpath"></param>
        /// <returns></returns>
        public static string GetLogicPath(string filepath)
        {
            if (!string.IsNullOrEmpty(Path.GetPathRoot(filepath)))
                return filepath;
            if (!filepath.StartsWith(UploadFolder))
                filepath = GetFilePath(filepath);
            return filepath.Replace(UploadFolder, "");
        }
        /// <summary>
        /// 创建文件并返回文件的逻辑路径
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="sourceStream"></param>
        /// <returns></returns>
        public static string Create(string filename, Stream sourceStream, bool closestream = true)
        {
            try
            {
                FileStream targetStream = null;
                if (!sourceStream.CanRead)
                {
                    throw new Exception2("数据流不可读!");
                }
                string filePath = GetFilePath(filename);
                using (targetStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    //read from the input stream in 4K chunks
                    //and save to output stream
                    const int bufferLen = 4096;
                    byte[] buffer = new byte[bufferLen];
                    int count = 0;
                    while ((count = sourceStream.Read(buffer, 0, bufferLen)) > 0)
                    {
                        targetStream.Write(buffer, 0, count);
                    }
                }
                return filePath.Substring(UploadFolder.Length);
            }
            finally
            {
                if (closestream)
                    sourceStream.Close();
            }
        }

        /// <summary>
        /// 创建文件并返回文件的逻辑路径
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="sourceStream"></param>
        /// <returns></returns>
        public static string Create(string filename, byte[] buffer)
        {
            FileStream targetStream = null;
            string filePath = GetFilePath(filename);
            using (targetStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                targetStream.Write(buffer, 0, buffer.Length);
            }
            return filePath.Substring(UploadFolder.Length);
        }
        static void CreateDiretory(string filename)
        {
            var directory = Path.GetDirectoryName(filename);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
        /// <summary>
        /// 写入文件并返回绝对路径
        /// </summary>
        /// <param name="filename">文件名，可以绝对路径和仅文件名</param>
        /// <param name="content">写入的内容</param>
        /// <param name="append">是否附加内容到文件中</param>
        /// <param name="encoding">文件编码 默认为UTF8</param>
        /// <returns></returns>
        public static string WriteAllText(string filename, string content, bool append = false, Encoding encoding = null)
        {
            if (encoding == null)
                encoding = Encoding.UTF8;
            string filepath = GetFilePath(filename);
            CreateDiretory(filepath);
            using (var writer = new StreamWriter(filepath, append, encoding))
            {
                writer.Write(content);
                writer.Flush();
                writer.Close();
            }
            return GetLogicPath(filepath);
        }
        /// <summary>
        /// 读取文件
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="encoding">默认为unicode</param>
        /// <returns></returns>
        public static string ReadAllText(string filename, Encoding encoding = null)
        {
            if (encoding == null)
                encoding = Encoding.Unicode;
            using (var reader = new StreamReader(GetFilePath(filename), encoding, true))
            {
                var content = reader.ReadToEnd();
                content.Clone();
                return content;
            }
        }
        /// <summary>
        /// 获取绝对路径
        /// </summary>
        /// <param name="logicpath">逻辑路径</param>
        /// <returns></returns>
        public static string GetAbsolutePath(string logicpath)
        {
            if (string.IsNullOrEmpty(Path.GetPathRoot(logicpath)))
            {
                return Path.Combine(UploadFolder, logicpath);
            }
            return logicpath;
        }

        /// <summary>
        /// 加载文件
        /// </summary>
        /// <param name="filepath">文件路径</param>
        /// <param name="delemiter">分隔符</param>
        /// <returns></returns>
        public static IEnumerable<Dictionary<string, string>> Load(string filepath, char delemiter)
        {
            List<Dictionary<string, string>> result = new List<Dictionary<string, string>>();
            List<string[]> lines = null;
            if (delemiter == 'e' || filepath.EndsWith(".xlsx") || filepath.EndsWith(".xls"))
            {
                using (ExcelHelper helper = ExcelHelper.CreateHelper(filepath, OpenType.OpenRead))
                {
                    lines = helper.ReadSheet();
                }
            }
            else
            {
                var text = ReadAllText(GetAbsolutePath(filepath), Encoding.GetEncoding("gb2312"));
                lines = text.SplitCSV(delemiter);
            }
            var header = lines[0];
            for (int i = 1; i < lines.Count; i++)
            {
                var record = lines[i];
                Dictionary<string, string> dict = new Dictionary<string, string>();
                for (int j = 0; j < header.Length; j++)
                {
                    dict[header[j]] = record.Length <= j ? null : record[j];
                }
                result.Add(dict);
            }
            return result;
        }

        /// <summary>
        /// 获取模板文件的路径
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static string GetTemplatePath(string file)
        {
            return Path.Combine(TemplateFolder, file);
        }
        /// <summary>
        /// 获取元数据的配置路径
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static string GetMetaPath(string file)
        {
            return Path.Combine(MetaFolder, file);
        }

        /// <summary>
        /// 根据扩展名获取内容类型
        /// </summary>
        /// <param name="extention"></param>
        /// <returns></returns>
        public static string GetContentType(string extention)
        {
            return Impl.FileHelper.ContentType.GetType(extention);
        }

        public static void CopyFile(string sources, string dest)
        {
            DirectoryInfo dinfo = new DirectoryInfo(sources);
            foreach (FileSystemInfo f in dinfo.GetFileSystemInfos())
            {
                String destName = Path.Combine(dest, f.Name);
                if (f is FileInfo)
                {
                    File.Copy(f.FullName, destName, true);
                }
                else
                {
                    if (!Directory.Exists(destName))
                        Directory.CreateDirectory(destName);
                    CopyFile(f.FullName, destName);
                }
            }
        }
    }
}
