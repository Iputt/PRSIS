using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Web;
using Owl.Domain;
using Owl.Feature;
using Owl.Util;

namespace Om.Sys.Log
{
    /// <summary>
    /// 创建一个对象，在数据库中存储 日志
    /// </summary>
    [DomainModel(Label = "系统日志", LogModify = false)]
    public class LogFile : AggRoot
    {
        /// <summary>
        /// 数据来源
        /// </summary>
        [DomainField(Label = "数据来源", Size = int.MaxValue)]
        public string Source { get; set; }
        /// <summary>
        /// 文件存储路径
        /// </summary>
        [DomainField(Label = "文件路径", Size = int.MaxValue)]
        public string FilePath { get; set; }
        /// <summary>
        /// 文本文件
        /// </summary>
        [DomainField(Field_Type = FieldType.text, Label = "文本文件", Size = int.MaxValue)]
        public string DataText { get; set; }
        static List<LogFile> LogFiles = new List<LogFile>();
        /// <summary>
        /// 写入Log日志文件
        /// </summary>
        /// <param name="dataText">文本数据</param>
        static public void Write(string dataText)
        {
            Write("", dataText, "");
        }
        /// <summary>
        /// 写入Log日志文件
        /// </summary>
        /// <param name="fileName">自定义文件名</param>
        /// <param name="dataText">文本数据</param>
        static public void Write(string fileName, string dataText)
        {
            Write(fileName, dataText, "");
        }
        static Mutex m_WriteMutex = new Mutex();
        /// <summary>
        /// 写入Log日志文件
        /// </summary>
        /// <param name="fileName">自定义文件名</param>
        /// <param name="dataText">文本数据</param>
        /// <param name="source">来源：手动填写，方便以后查询时查看</param> 
        static public void Write(string fileName, string dataText, string source)
        {
            m_WriteMutex.WaitOne();
            DateTime dt = DateTime.Now;
            FileStream fs = null;
            StreamWriter sw = null;
            string filePath = System.AppDomain.CurrentDomain.BaseDirectory;
            try
            {
                if (filePath[filePath.Length - 1] == '\\')
                    filePath += "Logs\\";
                else
                    filePath += "\\Logs\\";

                //CHECK文件目录是否存在
                if (!Directory.Exists(filePath))
                    Directory.CreateDirectory(filePath);
                filePath += fileName + dt.ToString("yyyMMdd") + ".log";
                //CHECK文件是否存在
                if (!File.Exists(filePath))
                {
                    FileStream tempfs = File.Create(filePath);
                    tempfs.Close();
                }
                fs = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.None);
                fs.Seek(0, System.IO.SeekOrigin.End);
                sw = new StreamWriter(fs, System.Text.Encoding.UTF8);
                sw.WriteLine(dt.ToString("yyyy-MM-dd HH:mm:ss ffff :") + dataText);
                sw.WriteLine("------------------------------------------------------------------------------------------");
            }
            finally
            {
                try
                {
                    if (sw != null)
                    {
                        sw.Close();
                        sw = null;
                    }

                    if (fs != null)
                    {
                        fs.Close();
                        fs = null;
                    }
                }
                catch { }
            }
            if (AppConfig.GetSetting("LogWriteToDb", "false") == "true")
            {
                try
                {
                    LogFile log = new LogFile();
                    //将日志写入数据库 
                    log.Id = Guid.NewGuid();
                    log.Created = dt;
                    if (OwlContext.Current != null && !string.IsNullOrEmpty(OwlContext.Current.UserName))
                        log.CreatedBy = OwlContext.Current.UserName;
                    else
                        log.CreatedBy = "system";
                    log.DataText = dataText;
                    log.FilePath = filePath;
                    log.Source = source;
                    var logs = new List<LogFile>();
                    try
                    {
                        logs.Add(log);
                    }
                    catch
                    {
                        logs.Clear();
                    }
                    if (logs.Count == 0)
                        return;
                    PushLog(logs);
                }
                catch { }
            }
            m_WriteMutex.ReleaseMutex();
        }
        static void PushLog(List<LogFile> logs)
        {
            bool bnewtask = false;
            lock (LogFiles)
            {
                if (LogFiles.Count == 0)
                    bnewtask = true;
                LogFiles.AddRange(logs);
            }
            if (bnewtask)
                TaskMgr.AddTask(CommitLog, 0, 2);
        }
        static void CommitLog()
        {
            var logs = new List<LogFile>();
            lock (LogFiles)
            {
                logs.AddRange(LogFiles);
                LogFiles.Clear();
            }
            using (var transaction = DomainContext.StartTransaction())
            {
                try
                {
                    foreach (var log in logs)
                    {
                        log.Push();
                    }
                    transaction.Commit();
                }
                catch { }
            }
        }
    }
}
