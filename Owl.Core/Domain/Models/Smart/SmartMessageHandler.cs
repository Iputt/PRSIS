//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using Owl.Feature;
//using Owl.Util;

//namespace Owl.Domain
//{
//    public class SmartMessageHandler : RootMessageHandler
//    {
//        HandlerExtension m_extension;
//        HandlerExtension extension
//        {
//            get
//            {
//                if (m_extension == null)
//                {
//                    m_extension = Metadata.GetExtension<HandlerExtension>();
//                    if (m_extension == null)
//                        m_extension = new HandlerExtension();
//                }
//                return m_extension;
//            }
//        }

//        protected override void Prepare()
//        {
//            base.Prepare();
//            if (!string.IsNullOrEmpty(extension.PreparedScript))
//            {
//                var param = new Dictionary<string, object>();
//                param["self"] = this;
//                Script.Execute(string.Format("{0}_prepared", Metadata.Name.Replace(".", "_")), extension.PreparedScript, param);
//            }
//        }

//        protected override object _Execute(AggRoot root)
//        {
//            if (!string.IsNullOrEmpty(extension.ExecuteScript))
//            {
//                var param = new Dictionary<string, object>();
//                param["self"] = this;
//                param["root"] = root;
//                var result = Script.Execute(string.Format("{0}_execute", Metadata.Name.Replace(".", "_")), extension.ExecuteScript, param);
//                root.Push();
//                return result;
//            }
//            return null;
//        }
//    }
//}
