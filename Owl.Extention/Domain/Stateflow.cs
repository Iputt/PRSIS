using Owl.Domain.Msg.Impl;
using Owl.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Owl.Domain.Stateflow
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class StateflowAttribute : Attribute
    {
        /// <summary>
        /// 状态字段
        /// </summary>
        public string StateField { get; }

        /// <summary>
        /// 可退回的状态,默认为所有状态
        /// </summary>
        public string[] CanBack { get; }
        public StateflowAttribute()
        {
            StateField = "Status";
            CanBack = new string[0];
        }
    }

    public interface IStateflow
    {
        /// <summary>
        /// 当执行退回时
        /// </summary>
        /// <param name="status"></param>
        object OnBack(string from, string to, string summary);
    }

    public enum BackType
    {
        [DomainLabel("退回开始")]
        ToStart,
        [DomainLabel("退回上一步")]
        ToLast
    }
    [DomainModel(Label = "退回")]
    public class StatusBack : FormObject
    {
        [DomainField(Label = "退回类型")]
        public BackType BackType { get; set; }

        [DomainField(FieldType.text, Label = "退回原因")]
        public string Summary { get; set; }

    }
    public class StateflowBackTo : RootMessageHandler
    {
        protected override object _Execute(AggRoot root)
        {

            var attr = StateflowFactory.Instance.GetAttr(root.Metadata.Name);
            var statefield = attr.StateField;

            var modelname = root.Metadata.Name;

            var fromdisplay = root.GetDisplay(statefield);
            var from = root.GetRealValue<string>(statefield);

            var fieldmeta = (root.Metadata.GetField(statefield) as SelectField);
            var items = fieldmeta.GetItems(modelname, statefield);
            int startindex = 0;
            if (!fieldmeta.Required && !fieldmeta.Multiple)
                startindex = 1;
            var index = startindex;
            BackType backType = BackType.ToLast;
            var summary = "";
            if (Descrip.Name == "backto")
            {
                var formobj = FormObj as StatusBack;
                backType = formobj.BackType;
                summary = formobj.Summary;
            }
            else
            {
                summary = "重启流程";
            }

            if (backType == BackType.ToLast)
            {
                var fromindex = items.GetIndex(from);
                if (fromindex > 0)
                    index = fromindex - 1;
            }

            var backto = (items.ElementAt(index) as ListItem).Value;
            root[statefield] = backto;

            var result = ((IStateflow)root).OnBack(from, backto, summary);
            root.Push();
            AppendLog(root, "", "退回{0}-{1}，原因：{2}", fromdisplay, root.GetDisplay(attr.StateField), summary);
            return result;
        }
    }
    public class StateflowFactory : TypeLoader<IStateflow, StateflowFactory>
    {
        Dictionary<string, StateflowAttribute> stateflows = new Dictionary<string, StateflowAttribute>();
        Dictionary<string, Dictionary<string, MsgDescrip>> descriptions = new Dictionary<string, Dictionary<string, MsgDescrip>>();
        protected override void OnTypeLoad(Type type)
        {
            var modelname = type.MetaName();
            var sfattr = type.GetCustomAttributes(typeof(StateflowAttribute), true).Cast<StateflowAttribute>().FirstOrDefault();
            if (sfattr == null)
                sfattr = new StateflowAttribute();
            stateflows[modelname] = sfattr;
            var meta = ModelMetadata.GetModel(type);

            var fieldmeta = (meta.GetField(sfattr.StateField) as SelectField);
            var items = fieldmeta.GetItems(modelname, sfattr.StateField).GetItems();

            var start = items.FirstOrDefault().Value;
            var end = items.LastOrDefault().Value;

            Specification condition = null;
            if (sfattr.CanBack == null || sfattr.CanBack.Length == 0)
                condition = Specification.Create(sfattr.StateField, CmpCode.NE, start);
            else
                condition = Specification.Create(sfattr.StateField, CmpCode.IN, sfattr.CanBack.Distinct());
            descriptions[modelname] = new Dictionary<string, MsgDescrip>();
            descriptions[modelname]["backto"] = new MsgDescrip()
            {
                Model = modelname,
                Name = "backto",
                Label2 = "退回",
                Source = MetaMode.Custom,
                Restrict = RootRestrict.Special,
                Type = MsgType.General,
                Condition = condition,
                ParamModel = "owl.domain.stateflow.statusback",
                PermissionConfig = true,
                Behaviors = new List<IMessageBehavior>() { new LogActionAttribute() },
                Resource = "owl.domain.stateflow.status.back",
            };
            descriptions[modelname]["restart"] = new MsgDescrip()
            {
                Model = modelname,
                Name = "restart",
                Label2 = "退回",
                Source = MetaMode.Custom,
                Confirm = "执行本操作将退回到初始状态，是否继续！",
                Restrict = RootRestrict.Special,
                Type = MsgType.General,
                Condition = Specification.Create(sfattr.StateField, CmpCode.EQ, end),
                PermissionConfig = true,
                Behaviors = new List<IMessageBehavior>() { new LogActionAttribute() },
                Resource = "owl.domain.stateflow.status.restart",
            };
        }

        public StateflowAttribute GetAttr(string modelname)
        {
            if (stateflows.ContainsKey(modelname))
                return stateflows[modelname];
            return null;
        }

        public Dictionary<string, MsgDescrip> GetDescrip(string modelname)
        {
            return descriptions.ContainsKey(modelname) ? descriptions[modelname] : null;
        }
    }
    public class StateflowMessageProvider : MessageProvider
    {
        public override int Priority => 1000;

        public override MsgDescrip GetDescrip(string name, string modelname)
        {
            if (name == "backto" || name == "restart")
            {
                var descrips = StateflowFactory.Instance.GetDescrip(modelname);
                if (descrips != null)
                    return descrips[name];
            }
            return null;
        }

        public override MsgHandler GetHandler(string name, string modelname)
        {
            if (name == "backto" || name == "restart")
            {
                var descrips = StateflowFactory.Instance.GetDescrip(modelname);
                if (descrips != null)
                {
                    var des = descrips[name];
                    return new StateflowBackTo() { Descrip = des };
                }
            }
            return null;
        }
        public override IEnumerable<MsgDescrip> GetDescrips(string modelname)
        {
            var des = StateflowFactory.Instance.GetDescrip(modelname);
            if (des != null)
                return new List<MsgDescrip>() { des["restart"] };
            return base.GetDescrips(modelname);
        }
    }
}
