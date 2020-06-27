using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Util;
using Owl.Domain;
namespace Owl.Feature.Impl.Approval
{
    public abstract class ApprovalProvider : Provider
    {
        /// <summary>
        /// 获取审批配置
        /// </summary>
        /// <param name="modelname">对象名称</param>
        /// <param name="msgname">消息名称</param>
        /// <returns></returns>
        public abstract ApprovalConfig GetConfig(string modelname, string msgname);

        /// <summary>
        /// 创建审批对象
        /// </summary>
        /// <param name="approval"></param>
        /// <returns></returns>
        public abstract AggRoot CreateApproval(ApprovalObj approval);
    }
}
