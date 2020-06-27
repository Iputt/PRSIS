using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Owl.Domain
{
    /// <summary>
    /// 审批模式
    /// </summary>
    public enum ApprovalMode
    {
        [DomainLabel("串行")]
        Serial,
        [DomainLabel("并行")]
        Parallel,
        [DomainLabel("串并行")]
        Both
    }
    /// <summary>
    /// 审批状态
    /// </summary>
    public enum ApprovalStatus
    {
        [DomainLabel("新建")]
        Draft,
        [DomainLabel("待批")]
        Pending,
        [DomainLabel("已完成")]
        Done
    }
    /// <summary>
    /// 审批状态2
    /// </summary>
    public enum ApprovalStatus2
    {
        [DomainLabel("新建")]
        Draft,
        [DomainLabel("待批")]
        Pending,
        [DomainLabel("审批中")]
        Approving,
        [DomainLabel("已完成")]
        Done
    }
}
