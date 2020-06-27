using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Util;
using Owl.Domain;
namespace Owl.Feature.iPlan
{
    /// <summary>
    /// 计划描述
    /// </summary>
    public abstract class PlanDescription : SmartObject
    {
        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 显示名称
        /// </summary>
        public string Display { get; set; }

        /// <summary>
        /// 创建处理器
        /// </summary>
        /// <returns></returns>
        public PlanHandler Create()
        {
            var handler = _Create();
            if (handler != null)
                handler.Description = this;
            return handler;
        }

        protected abstract PlanHandler _Create();
    }

    /// <summary>
    /// 计划处理器
    /// </summary>
    public abstract class PlanHandler
    {
        /// <summary>
        /// 计划描述
        /// </summary>
        public PlanDescription Description { get; set; }

        protected abstract string _Execute();

        /// <summary>
        /// 执行计划
        /// </summary>
        public string Execute()
        {
            return _Execute();
        }
        /// <summary>
        /// 任务执行错误时
        /// </summary>
        /// <param name="ex"></param>
        public virtual void OnError(Exception ex)
        {
        }
    }
}
