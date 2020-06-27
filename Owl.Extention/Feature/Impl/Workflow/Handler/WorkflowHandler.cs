using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Domain;
using Owl.Domain.Driver;
namespace Owl.Feature.Workflows.Handler
{
    public class WorkflowHanlder : RootMessageHandler
    {
        protected override object _Execute(AggRoot root)
        {
            var instance = WorkflowInstance.Load(root);
            return instance.Resume(Message.Name, Dto);
        }

        protected TransferObject Dto;
        protected override void _Write(TransferObject dto)
        {
            base._Write(dto);
            Dto = dto;
        }
    }
}
