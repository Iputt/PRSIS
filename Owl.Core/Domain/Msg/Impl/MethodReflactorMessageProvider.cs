using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Domain;
using System.Reflection;
using Owl.Domain.Driver;
using Owl.Util;
using System.Collections;
namespace Owl.Domain.Msg.Impl
{
    internal class MethodHandlerDesrip : MsgDescrip
    {
        public Type ModelType { get; set; }

        /// <summary>
        /// 方法
        /// </summary>
        public MethodInfo Method { get; set; }

        /// <summary>
        /// 准备工作
        /// </summary>
        public MethodInfo Prepare { get; set; }

        /// <summary>
        /// 是否拼装对象
        /// </summary>
        public bool ManulForm { get; set; }

        /// <summary>
        /// 是否需要根
        /// </summary>
        public bool NeedRoot { get; set; }

        /// <summary>
        /// 参数是否是一组对象
        /// </summary>
        public bool IsArray { get; set; }
    }
    public class MethodMessageHandler : RootMessageHandler
    {
        MethodHandlerDesrip m_Descript { get { return (MethodHandlerDesrip)Descrip; } }
        protected override void Prepare()
        {
            base.Prepare();
            if (m_Descript.Prepare != null)
                m_Descript.Prepare.FaseInvoke(null, this);
        }

        object BuildArray()
        {
            var ptype = m_Descript.Method.GetParameters()[0].ParameterType;
            var elementtype = TypeHelper.GetElementType(ptype);
            ArrayList tmparray = new ArrayList();
            foreach (var root in Roots)
            {
                tmparray.Add(root);
            }
            return Convert2.ChangeType(tmparray, ptype, elementtype);
            //var array = tmparray.ToArray(elementtype);
            //if (ptype.IsArray || ptype.Name == "IEnumerable`1")
            //    return array;
            //return Activator.CreateInstance(typeof(List<>).MakeGenericType(elementtype), array);
        }

        object BuildEntityArray(AggRoot root)
        {
            var ptype = m_Descript.Method.GetParameters()[0].ParameterType;
            var elementtype = TypeHelper.GetElementType(ptype);
            var entities = Message.Param.GetRealValue<string[]>("entities");
            Entity tentity = root;
            ArrayList argentity = new ArrayList();
            foreach (var pair in entities)
            {
                var tmp = pair.Split('|');
                var field = tmp[0];
                var ids = tmp[1].Split(',').Select(s => Guid.Parse(s));
                argentity.Clear();
                foreach (Entity entity in tentity[field] as IEnumerable)
                {
                    if (entity.Id.In(ids))
                    {
                        argentity.Add(entity);
                        tentity = entity;
                    }
                }
                if (ids.Count() > 1)
                    break;
            }
            var array = argentity.ToArray(elementtype);
            if (ptype.IsArray || ptype.Name == "IEnumerable`1")
                return array;
            return Activator.CreateInstance(typeof(List<>).MakeGenericType(elementtype), array);
        }

        object execute(AggRoot root)
        {
            List<object> args = new List<object>();
            var method = m_Descript.Method;
            if (method.IsStatic)
            {
                if (root != null)
                    args.Add(root);
                if (m_Descript.IsArray)
                    args.Add(BuildArray());
            }
            if (!string.IsNullOrEmpty(m_Descript.EntityModel))
            {
                args.Add(BuildEntityArray(root));
            }
            if (FormObj != null)
            {
                if (m_Descript.ManulForm)
                {
                    foreach (var field in m_Descript.ParamMetadata.GetFieldNames())
                    {
                        args.Add(FormObj[field]);
                    }
                }
                else
                    args.Add(FormObj);
            }

            object target = method.IsStatic ? null : root;
            var result = method.FaseInvoke(target, args.ToArray());
            if (root != null)
                root.Push();
            return result;
        }
        protected override object _Execute()
        {
            if (!m_Descript.NeedRoot || m_Descript.IsArray)
            {
                return execute(null);
            }
            return base._Execute();
        }

        protected override object _Execute(AggRoot root)
        {
            return execute(root);
        }
    }

    public class MethodReflactorMessageProvider : MessageProvider
    {
        #region 反射元数据
        static Type basehandler = typeof(AggRoot);
        static Type attrtype = typeof(MsgRegisterAttribute);
        static readonly Dictionary<string, Dictionary<string, MethodHandlerDesrip>> descrips = new Dictionary<string, Dictionary<string, MethodHandlerDesrip>>(200);
        static bool IsSmart<T>(MethodInfo method)
        {
            var type = typeof(T);
            var parameters = method.GetParameters();
            if (parameters.Length == 1 && parameters[0].ParameterType == type || parameters[0].ParameterType.IsSubclassOf(type))
                return true;
            return false;
        }

        static void loadfromasm(string name, Assembly asm)
        {
            foreach (var type in asm.GetExportedTypes())
            {
                if ((type.IsAbstract && type.IsSealed) || (!type.IsAbstract && type.IsSubclassOf(basehandler)))
                {
                    var _allow = new Allow();
                    Dictionary<Type, IEnumerable<MethodInfo>> methods = new Dictionary<Type, IEnumerable<MethodInfo>>();
                    methods[type] = type.GetMethods(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    Func<Type, IEnumerable<MethodInfo>> GetMethods = s =>
                    {
                        if (!methods.ContainsKey(s))
                            methods[s] = s.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                        return methods[s];
                    };
                    //List<MethodInfo> methods = new List<MethodInfo>();
                    //methods.AddRange(type.GetMethods(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
                    foreach (var method in methods[type])
                    {
                        try
                        {
                            #region 解析方法
                            var attrs = method.GetCustomAttributes(true);
                            foreach (var attr in attrs.OfType<MsgRegisterAttribute>())
                            {
                                string typename = type.IsSubclassOf(basehandler) ? type.MetaName() : attr.Model.ToLower();
                                var msgname = attr.Name.ToLower();
                                var param = method.GetParameters();
                                Type modeltype = type.IsSubclassOf(basehandler) ? type : null;
                                int first = 0;
                                bool ignore = method.IsStatic;
                                bool isarray = false;
                                if (method.IsStatic && param.Length > 0)
                                {
                                    var etype = TypeHelper.GetElementType(param[0].ParameterType);
                                    var barray = etype != null;
                                    var rtype = etype ?? param[0].ParameterType;
                                    if (rtype != null && rtype.IsSubclassOf(basehandler))
                                    {
                                        typename = rtype.MetaName();
                                        modeltype = rtype;
                                        ignore = false;
                                        isarray = barray;
                                        first = 1;
                                    }
                                }
                                List<IMessageBehavior> behaviors = new List<IMessageBehavior>(attrs.OfType<IMessageBehavior>());
                                if (attr.NeedLog && behaviors.OfType<LogActionAttribute>().Count() == 0)
                                    behaviors.Add(new LogActionAttribute());
                                var descript = new MethodHandlerDesrip()
                                {
                                    Name = msgname,
                                    Alias = attr.Alias,
                                    Model = typename,
                                    Method = method,
                                    Resource = attr.Resource,
                                    Label2 = attr.Label,
                                    Source = MetaMode.Base,
                                    AutoShow = attr.AutoShow,
                                    Ordinal = attr.Ordinal,
                                    NeedRoot = !ignore,
                                    IsArray = isarray,
                                    Condition = attr.Specific,
                                    PermissionConfig = true,
                                    Confirm = attr.Confirm,
                                    Singleton = attr.Singleton,
                                    SingleTimeout = attr.SingleTimeout,
                                    SingleNotify = attr.SingleNotify,
                                    SingleNotifyResource = attr.SingleNotifyResource,
                                    Type = attr.Type,
                                    Restrict = ignore ? RootRestrict.None : attr.Restrict,
                                    Relations = string.IsNullOrEmpty(attr.Relations) ? new string[0] : attr.Relations.Split(';'),
                                    ModelType = modeltype,
                                    Prepare = GetMethods(method.DeclaringType).FirstOrDefault(s => s.IsStatic && s.Name == method.Name + "_Prepare" && IsSmart<RootMessageHandler>(s)),
                                    Behaviors = behaviors.AsReadOnly()

                                };
                                descript.AuthObj = (attrs.OfType<Allow>().FirstOrDefault() ?? _allow).ToAuthObj(msgname, AuthTarget.Message);
                                if (param.Length > first)
                                {
                                    var etype = TypeHelper.GetElementType(param[first].ParameterType);
                                    if (etype != null && etype.IsSubclassOf(typeof(Entity)))
                                    {
                                        if (ModelMetadataEngine.GetModel(modeltype).GetEntityField(etype) == null)
                                            continue;
                                        first = first + 1;
                                        descript.EntityModel = etype.MetaName();
                                    }
                                }
                                if (param.Length > first)
                                {
                                    if (param[first].ParameterType.IsSubclassOf(typeof(FormObject)))
                                        descript.ParamModel = param[first].ParameterType.MetaName();
                                    else
                                    {
                                        descript.ParamModel = string.Format("{0}.{1}", typename, msgname);
                                        descript.ManulForm = true;
                                        var meta = new DomainModel();
                                        meta.Name = descript.ParamModel;
                                        meta.SetState(MetaMode.Base);
                                        meta.SetType(typeof(SmartForm));
                                        var extension = attrs.OfType<FormExtension>().FirstOrDefault();
                                        if (extension == null)
                                            extension = new FormExtension();
                                        extension.LoadAction = GetMethods(method.DeclaringType).FirstOrDefault(s => s.IsStatic && s.Name == method.Name + "_Default" && IsSmart<BehaviorObject>(s));
                                        if (extension.LoadAction == null)
                                            extension.LoadAction = GetMethods(method.DeclaringType).FirstOrDefault(s => s.IsStatic && s.Name == method.Name + "_Load" && IsSmart<BehaviorObject>(s));
                                        meta.SetExtension(extension);

                                        for (int i = first; i < param.Length; i++)
                                        {
                                            var pfield = param[i];
                                            var field = pfield.GetCustomAttributes(typeof(DomainField), false).Cast<DomainField>().FirstOrDefault();
                                            if (field == null)
                                                field = new DomainField();
                                            field.SetName(pfield.Name);
                                            if (string.IsNullOrEmpty(field.Resource))
                                                field.Resource = string.Format("fieldlabel.{0}.{1}", meta.Name, pfield.Name.ToLower());
                                            meta.SetField(field);
                                            field.SetType(pfield.ParameterType);
                                        }
                                        DomainModel.RegisterMeta(meta);
                                    }
                                }
                                if (!descrips.ContainsKey(typename))
                                    descrips[typename] = new Dictionary<string, MethodHandlerDesrip>(20);
                                descrips[typename][msgname] = descript;
                                if (!string.IsNullOrEmpty(descript.EntityModel))
                                {
                                    if (!descrips.ContainsKey(descript.EntityModel))
                                        descrips[descript.EntityModel] = new Dictionary<string, MethodHandlerDesrip>();
                                    descrips[descript.EntityModel][msgname] = descript;
                                }
                            }
                            #endregion
                        }
                        catch (Exception ex)
                        {
                            throw new Exception2("对象 {0} 的方法 {1} 的参数不符合消息定义，请检查", type.FullName, method.Name);
                        }
                    }
                }
            }
        }
        static void unloadasm(string name, Assembly asm)
        {
            foreach (var desp in descrips.Values.SelectMany(s => s.Values).Where(s => s.Method.DeclaringType.Assembly == asm).ToList())
            {
                descrips[desp.Model].Remove(desp.Name);
            }
        }
        static MethodReflactorMessageProvider()
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
        MethodHandlerDesrip getdescrip(string name, string modelname)
        {
            if (descrips.ContainsKey(modelname) && descrips[modelname].ContainsKey(name))
                return descrips[modelname][name];
            var templatename = GetTemplateName(modelname);
            if (!string.IsNullOrEmpty(templatename) && descrips.ContainsKey(templatename) && descrips[templatename].ContainsKey(name))
                return descrips[templatename][name];
            return null;
        }

        public override MsgHandler GetHandler(string name, string modelname)
        {
            var hdes = getdescrip(name, modelname);
            if (hdes == null)
                return null;
            return new MethodMessageHandler() { Descrip = hdes };
        }

        public override MsgDescrip GetDescrip(string name, string modelname)
        {
            return getdescrip(name, modelname);
        }
        public override IEnumerable<MsgDescrip> GetDescrips(string modelname)
        {
            var result = new List<MsgDescrip>();
            if (descrips.ContainsKey(modelname))
                result.AddRange(descrips[modelname].Values);
            var templatename = GetTemplateName(modelname);
            if (descrips.ContainsKey(modelname))
                result.AddRange(descrips[modelname].Values);
            return result;
        }

        public override int Priority
        {
            get { return 500; }
        }
    }
}
