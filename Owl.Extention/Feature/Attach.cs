using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Owl.Feature.Impl.Attach;

namespace Owl.Feature
{
    /// <summary>
    /// 附加文件
    /// </summary>
    public class Attach : Engine<AttachProvider, Attach>
    {
        static Dictionary<object, string> m_names = new Dictionary<object, string>();
        public static List<string> GetName(object[] keys)
        {
            List<string> results = new List<string>();
            foreach (var key in keys)
            {
                var name = "";
                if (m_names.ContainsKey(key))
                    name = m_names[key];

                if (string.IsNullOrEmpty(name))
                {
                    name = Execute2<object, string>(s => s.GetName, key) ?? "";
                    m_names[key] = name;
                }
                results.Add(name);
            }
            return results;
        }

        public static Attachmant GetAttachment(object key)
        {
            return Execute2<object, Attachmant>(s => s.GetAttachment, key);
        }

        public static void RemoveAttachment(object key)
        {
            Execute(s => s.RemoveAttachment, key);
        }

        public static bool HasAttachment(object key)
        {
            if (key == null)
                return false;
            return Execute2<object, bool>(s => s.HasAttachment, key);
        }

        public static void CreateAttachement(Attachmant attach)
        {
            Execute<Attachmant>(s => s.CreateAttachment, attach);
        }

        public static IEnumerable<Attachmant> GetAttachements(string model, object objid, bool withcontent)
        {
            return Execute2<string, object, bool, IEnumerable<Attachmant>>(s => s.GetAttachments, model, objid, withcontent);
        }

        public static IEnumerable<Attachmant> GetAttachments(object[] keys, bool withcontent = false)
        {
            if (keys == null || keys.Length == 0)
                return null;
            return Execute2<object[], bool, IEnumerable<Attachmant>>(s => s.GetAttachments, keys, withcontent);
        }
    }
}