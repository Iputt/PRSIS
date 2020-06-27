using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Domain;
using System.Reflection;
using Owl.Domain.Driver;
using Owl.Util;
namespace Owl.Domain.Msg.Impl
{
    public class ModelReflactorMessageProvider : MessageProvider
    {
        #region 反射元数据
        static Type basehandler = typeof(MsgHandler);
        static Type attrtype = typeof(MsgRegisterAttribute);
        static readonly Dictionary<string, Dictionary<string, MsgDescrip>> descrips = new Dictionary<string, Dictionary<string, MsgDescrip>>(200);
        static void loadfromasm(string name, Assembly asm)
        {
            var _allow = new Allow();
            foreach (var type in asm.GetExportedTypes())
            {
                if (type.IsSubclassOf(basehandler) && !type.IsAbstract)
                {
                    var attrs = type.GetCustomAttributes(false);
                    foreach (var attr in attrs.OfType<MsgRegisterAttribute>())
                    {
                        var msgname = attr.Name.ToLower();
                        var modelname = attr.Model.ToLower();
                        var paramodel = type.MetaName();
                        var gtype = TypeHelper.GetBaseGenericType(type, basehandler);
                        if (gtype != null)
                        {
                            modelname = gtype.MetaName();
                            paramodel = string.Format("{0}.{1}", modelname, msgname);
                        }
                        List<IMessageBehavior> behaviors = new List<IMessageBehavior>(attrs.OfType<IMessageBehavior>());
                        if (attr.NeedLog && behaviors.OfType<LogActionAttribute>().Count() == 0)
                            behaviors.Add(new LogActionAttribute());
                        var handler = new MsgDescrip()
                        {
                            Name = msgname,
                            Alias = attr.Alias,
                            Model = modelname,
                            ParamModel = paramodel,
                            Label2 = attr.Label,
                            Resource = attr.Resource,
                            Source = MetaMode.Base,
                            AutoShow = attr.AutoShow,
                            Ordinal = attr.Ordinal,
                            Restrict = attr.Restrict,
                            PermissionConfig = attr.PermissionConfig || gtype != null,
                            Relations = string.IsNullOrEmpty(attr.Relations) ? new string[0] : attr.Relations.Split(';'),
                            Type = attr.Type,
                            Condition = attr.Specific,
                            Confirm = attr.Confirm,
                            Behaviors = behaviors,
                            Singleton = attr.Singleton,
                            SingleTimeout = attr.SingleTimeout,
                            SingleNotify = attr.SingleNotify,
                            SingleNotifyResource = attr.SingleNotifyResource
                        };
                        handler.AuthObj = (attrs.OfType<Allow>().FirstOrDefault() ?? _allow).ToAuthObj(msgname, AuthTarget.Message);
                        if (!descrips.ContainsKey(modelname))
                            descrips[modelname] = new Dictionary<string, MsgDescrip>(20);
                        descrips[modelname][msgname] = handler;
                    }
                }
            }
        }
        static void unloadasm(string name, Assembly asm)
        {
            foreach (var desp in descrips.Values.SelectMany(s => s.Values).Where(s => s.ParamMetadata.ModelType.Assembly == asm).ToList())
            {
                descrips[desp.Model].Remove(desp.Name);
            }

        }
        static ModelReflactorMessageProvider()
        {
            Util.AsmHelper.RegisterResource(loadfromasm, unloadasm);
        }
        #endregion

        string GetTemplateName(string modelname)
        {
            var meta = MetaEngine.GetModel(modelname);
            if (meta.State == MetaMode.Custom)
            {
                return meta.ModelType.MetaName();
            }
            return null;
        }
        MsgDescrip getdescrip(string name, string modelname)
        {
            if (descrips.ContainsKey(modelname) && descrips[modelname].ContainsKey(name))
                return descrips[modelname][name];
            var templatename = GetTemplateName(modelname);
            if (!string.IsNullOrEmpty(templatename) && descrips.ContainsKey(templatename) && descrips[templatename].ContainsKey(name))
                return descrips[templatename][name];
            if (descrips.ContainsKey("*") && descrips["*"].ContainsKey(name))
                return descrips["*"][name];
            return null;
        }
        public override MsgHandler GetHandler(string name, string modelname)
        {
            MsgDescrip hdescrip = getdescrip(name, modelname);
            if (hdescrip != null)
            {
                var handler = DomainFactory.Create<MsgHandler>(hdescrip.ParamMetadata);
                handler.Descrip = hdescrip;
                return handler;
            }
            return null;
        }


        public override MsgDescrip GetDescrip(string name, string modelname)
        {
            return getdescrip(name, modelname);
        }

        public override IEnumerable<MsgDescrip> GetDescrips(string modelname)
        {
            List<MsgDescrip> descips = new List<MsgDescrip>();
            if (descrips.ContainsKey("*"))
                descips.AddRange(descrips["*"].Values);
            var templatename = GetTemplateName(modelname);
            if (descrips.ContainsKey(modelname))
                descips.AddRange(descrips[modelname].Values);
            if (descrips.ContainsKey(modelname))
                descips.AddRange(descrips[modelname].Values);
            return descips;
        }

        
        public override int Priority
        {
            get { return 1000; }
        }
    }
}
