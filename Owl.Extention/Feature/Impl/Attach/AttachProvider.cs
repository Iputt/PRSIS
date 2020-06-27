using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Util;
using System.IO;
using System.Reflection;
using Owl.Domain;
namespace Owl.Feature.Impl.Attach
{
    /// <summary>
    /// 文件信息
    /// </summary>
    public class Attachmant : SmartObject
    {
        public object Key { get; set; }
        /// <summary>
        /// 文件名称
        /// </summary>
        public string FileName { get; set; }
        /// <summary>
        /// 扩展名
        /// </summary>
        public string Extention { get; set; }
        /// <summary>
        /// 文件内容
        /// </summary>
        public byte[] Content { get; set; }

        public string Modelname { get; set; }
        public bool FileSaveDB { get; set; }

        public object ObjectId { get; set; }
        /// <summary>
        /// contenttype
        /// </summary>
        public string ContentType
        {
            get
            {
                return FileHelper.GetContentType(Extention);
            }
        }
    }

    public abstract class AttachProvider : Provider
    {
        public virtual bool HasAttachment(object key) { return false; }

        public virtual string GetName(object key) { return null; }

        public virtual Attachmant GetAttachment(object key) { return null; }

        public virtual void CreateAttachment(Attachmant attach) { }

        public virtual void RemoveAttachment(object key) { }

        public virtual IEnumerable<Attachmant> GetAttachments(string model, object objid, bool withcontent) { return null; }

        public virtual IEnumerable<Attachmant> GetAttachments(object[] keys, bool withcontent) { return null; }
    }
}
