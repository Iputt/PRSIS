using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;

namespace Owl.Util.Impl.FileHelper
{

    public abstract class ContentTypeProvider : Provider
    {
        public abstract string GetType(string extention);
    }

    public class InnerContentTypeProvider : ContentTypeProvider
    {
        Dictionary<string, string> contenttypes;
        public InnerContentTypeProvider()
        {
            using (var reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("Owl.Util.Impl.FileHelper.content-type.txt")))
            {
                var lines = reader.ReadToEnd().Split('\n');
                contenttypes = new Dictionary<string, string>(lines.Length);
                foreach (var line in lines)
                {
                    var pair = line.Split('=');
                    contenttypes[pair[0].ToLower().Replace("\"", "")] = pair[1].Replace("\"", "");
                }
            }
        }
        public override string GetType(string extention)
        {
            extention = extention.ToLower();
            if (contenttypes.ContainsKey(extention))
                return contenttypes[extention];
            return null;
        }
        public override int Priority
        {
            get { return 100; }
        }
    }
}
