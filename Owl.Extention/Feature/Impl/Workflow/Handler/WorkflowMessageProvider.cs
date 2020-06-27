using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Domain.Msg.Impl;
using Owl.Domain;
namespace Owl.Feature.Workflows.Handler
{
    public class WorkflowMessageProvider : MessageProvider
    {
        Transition gettransition(string name, string modelname)
        {
            var workflow = WorkflowEngine.GetWorkflow(modelname);
            if (workflow == null)
                return null;
            return workflow.GetTransition(name);
        }

        public override MsgHandler GetHandler(string name, string modelname)
        {
            var des = GetDescrip(name, modelname);
            return des == null ? null : new WorkflowHanlder() { Descrip = des };
        }

        public override MsgDescrip GetDescrip(string name, string modelname)
        {
            var transition = gettransition(name, modelname);
            return transition == null ? null : new MsgDescrip()
            {
                Name = name,
                Label2 = transition.Name,
                Model = modelname,
                NeedLog = true,
                Source = MetaMode.Custom,
                Condition = transition.Specific,
                Restrict = RootRestrict.All,
                AuthObj = new AuthObject(name, AuthTarget.Message, transition.Roles),
                ParamMetadata = transition.Destination.ViewMeta,
                Behaviors = new List<IMessageBehavior>() { new LogActionAttribute() }
            };
        }
        public override IEnumerable<MsgDescrip> GetDescrips(string modelname)
        {
            List<MsgDescrip> descrips = new List<MsgDescrip>();
            var workflow = WorkflowEngine.GetWorkflow(modelname);
            if (workflow != null)
            {
                foreach (var tran in workflow.Transitions)
                {
                    if (string.IsNullOrEmpty(tran.Signal))
                        continue;
                    descrips.Add(new MsgDescrip() { Name = tran.Signal, Label2 = tran.Name, Model = workflow.ModelName, NeedLog = false, Source = MetaMode.Custom, Condition = tran.Specific });
                }
            }
            return descrips;
        }
        public override IEnumerable<MsgDescrip> GetDescrips()
        {
            List<MsgDescrip> descrips = new List<MsgDescrip>();
            foreach (var workflow in WorkflowEngine.GetWorkflows())
            {
                foreach (var tran in workflow.Transitions)
                {
                    if (string.IsNullOrEmpty(tran.Signal))
                        continue;
                    descrips.Add(new MsgDescrip() { Name = tran.Signal, Label2 = tran.Name, Model = workflow.ModelName, NeedLog = false, Source = MetaMode.Custom, Condition = tran.Specific });
                }
            }
            return descrips;
        }
        public override int Priority
        {
            get { return 200; }
        }
    }
}
