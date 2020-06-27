using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Util;
using Owl.Domain.Driver;
using Owl.Feature.Workflows;
namespace Owl.Feature
{

    public class WorkflowEngine : Engine<WorkflowProvider,WorkflowEngine>
    {
        public static Workflow GetWorkflow(string model)
        {
            var workflow = Execute2<string, Workflow>(s => s.GetWorkflow, model);
            return workflow;
        }

        public static IEnumerable<Workflow> GetWorkflows()
        {
            return Execute3<Workflow>(s => s.GetWorkflows);
        }

        internal static WorkflowInstance LoadInstance(string model, Guid rootid)
        {
            return Execute2<string, Guid, WorkflowInstance>(s => s.LoadInstance, model, rootid);
        }

        internal static void SaveInstance(WorkflowInstance instance)
        {
            Execute(s => s.SaveInstance, instance);
        }
    }
}
