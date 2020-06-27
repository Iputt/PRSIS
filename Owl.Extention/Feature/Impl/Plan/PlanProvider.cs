using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Util;
using System.Reflection;
namespace Owl.Feature.iPlan
{
    public abstract class PlanProvider : Provider
    {
        /// <summary>
        /// 获取计划列表
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerable<PlanDescription> GetDescription() { return null; }

        /// <summary>
        /// 根据名称获取计划
        /// </summary>
        /// <returns>The description.</returns>
        /// <param name="name">Name.</param>
        public virtual PlanDescription GetDescription(string name) { return null; }

        /// <summary>
        /// 获取计划的触发器
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public abstract IEnumerable<Trigger> GetTriggers(string name);
    }

}
