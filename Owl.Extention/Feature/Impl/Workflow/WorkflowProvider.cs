using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Util;
namespace Owl.Feature.Workflows
{
    public abstract class WorkflowProvider : Provider
    {
        /// <summary>
        /// 获取工作流
        /// </summary>
        /// <param name="modelname"></param>
        /// <returns></returns>
        public abstract Workflow GetWorkflow(string modelname);

        /// <summary>
        /// 获取所有工作流
        /// </summary>
        /// <returns></returns>
        public abstract IEnumerable<Workflow> GetWorkflows();
        /// <summary>
        /// 加载流程实例
        /// </summary>
        /// <param name="modelname"></param>
        /// <param name="rootid"></param>
        /// <returns></returns>
        public abstract WorkflowInstance LoadInstance(string modelname,Guid rootid);
        /// <summary>
        /// 保存流程实例
        /// </summary>
        /// <param name="instance"></param>
        public abstract void SaveInstance(WorkflowInstance instance);
    }
}
