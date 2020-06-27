using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Domain;
namespace Owl.Feature.Impl.Approval
{
    /// <summary>
    /// 审批配置
    /// </summary>
    public class ApprovalConfig : SmartObject
    {
        /// <summary>
        /// 对象名称
        /// </summary>
        public string ModelName { get; set; }
        /// <summary>
        /// 需审批的消息名称
        /// </summary>
        public string MsgName { get; set; }
        /// <summary>
        /// 审批模式
        /// </summary>
        public ApprovalMode Mode { get; set; }
        List<Approver> approvers;
        /// <summary>
        /// 审批人
        /// </summary>
        public List<Approver> Approvers
        {
            get
            {
                if (approvers == null)
                    approvers = new List<Approver>();
                return approvers;
            }
            set
            {
                approvers = value;
            }
        }
    }
    /// <summary>
    /// 审批人
    /// </summary>
    public class Approver : SmartObject
    {
        /// <summary>
        /// 审批人标识符
        /// </summary>
        public object Key { get; set; }

        /// <summary>
        /// 等级 最小的先行
        /// </summary>
        public int Level { get; set; }
    }

    /// <summary>
    /// 审批
    /// </summary>
    public class ApprovalObj : SmartObject
    {
        /// <summary>
        /// 对象名称
        /// </summary>
        public string ModelName { get; set; }

        /// <summary>
        /// 消息名称
        /// </summary>
        public string MsgName { get; set; }
        /// <summary>
        /// 对象Id
        /// </summary>
        public object Key { get; set; }

        /// <summary>
        /// 主题
        /// </summary>
        public string Subject { get; set; }

        /// <summary>
        /// 内容描述
        /// </summary>
        public string Summary { get; set; }

        /// <summary>
        /// 相关文档
        /// </summary>
        public string Document { get; set; }

        /// <summary>
        /// 内容 json
        /// </summary>
        public string Handler { get; set; }

        /// <summary>
        /// 审批模式
        /// </summary>
        public ApprovalMode Mode { get; set; }
        /// <summary>
        /// 审批人
        /// </summary>
        public IEnumerable<Approver> Approvers { get; set; }
    }

}
