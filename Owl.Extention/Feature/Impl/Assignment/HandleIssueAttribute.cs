using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Feature;
namespace Owl.Domain
{
    /// <summary>
    /// 消息执行后 创建任务
    /// </summary>
    public sealed class HandleIssueAttribute : MessageBehaviorAttribute
    {
        public override void OnSuccess(IEnumerable<AggRoot> roots)
        {
            AssignmentEngine.HandleIssue(roots.ToArray());
        }
    }
}
