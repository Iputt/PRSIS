using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Util;
using Owl.Domain;
namespace Owl.Feature.Impl.Approval
{
    public class ApprovalEngine : Engine<ApprovalProvider, ApprovalEngine>
    {
        static ApprovalConfig GetConfig(string modelname, string msgname)
        {
            return Execute2<string, string, ApprovalConfig>(s => s.GetConfig, modelname, msgname);
        }

        /// <summary>
        /// 创建审批 并返回可持久化的审批对象
        /// </summary>
        /// <param name="approval">审批对象</param>
        /// <returns></returns>
        public static AggRoot CreateApproval(ApprovalObj approval)
        {
            var config = GetConfig(approval.ModelName, approval.MsgName);
            if (config == null)
                throw new Exception2("对象 {0} 的 审批 {1} 未配置", approval.ModelName, approval.MsgName);
            approval.Mode = config.Mode;
            approval.Approvers = config.Approvers;

            return Execute2<ApprovalObj, AggRoot>(s => s.CreateApproval, approval);
        }
    }
}
