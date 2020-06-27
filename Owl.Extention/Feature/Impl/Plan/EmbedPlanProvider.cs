using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Owl.Util;

namespace Owl.Feature.iPlan
{
    /// <summary>
    /// 定义计划
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class PlanAttribute : Attribute
    {
        /// <summary>
        /// 计划名称
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// 显示名称
        /// </summary>
        public string Display { get; private set; }

        /// <summary>
        /// 计划
        /// </summary>
        /// <param name="name">名称</param>
        /// <param name="display">显示名称</param>
        public PlanAttribute(string name, string display)
        {
            Name = name;
            Display = display;
        }
    }



    internal class EmbedPlanDescription : PlanDescription
    {
        internal Type Type { get; set; }

        protected override PlanHandler _Create()
        {
            return Activator.CreateInstance(Type) as PlanHandler;
        }
    }

    public class EmbedPlanProvider : PlanProvider
    {
        public EmbedPlanProvider()
        {
            AsmHelper.RegisterResource(LoadAsm, UnLoad);
        }
        Dictionary<string, EmbedPlanDescription> Descriptions = new Dictionary<string, EmbedPlanDescription>();
        Dictionary<string, IEnumerable<Trigger>> Triggers = new Dictionary<string, IEnumerable<Trigger>>();
        void LoadAsm(string name, Assembly asm)
        {
            foreach (var type in TypeHelper.LoadTypeFromAsm<PlanHandler>(asm))
            {
                var attrs = type.GetCustomAttributes(false);
                var desattr = attrs.OfType<PlanAttribute>().FirstOrDefault();
                if (desattr == null)
                    continue;
                //desattr = new PlanAttribute(type.Name.Replace("PlanHandler", ""), type.Name);
                var triggers = attrs.OfType<Trigger>();
                Triggers[desattr.Name] = triggers;
                var description = new EmbedPlanDescription() { Name = desattr.Name, Display = desattr.Display, Type = type };
                Descriptions[description.Name] = description;
            }
        }
        void UnLoad(string name, Assembly asm)
        {
            foreach (var key in Descriptions.Where(s => s.Value.GetType().Assembly == asm).Select(s => s.Key))
            {
                Descriptions.Remove(key);
                Triggers.Remove(key);
            }
        }


        public override IEnumerable<PlanDescription> GetDescription()
        {
            return Descriptions.Values;
        }
        public override PlanDescription GetDescription(string name)
        {
            return Descriptions.ContainsKey(name) ? Descriptions[name] : null;
        }
        public override IEnumerable<Trigger> GetTriggers(string name)
        {
            return Triggers.ContainsKey(name) ? Triggers[name] : new List<Trigger>();
        }
        public override int Priority
        {
            get { return 1; }
        }
    }
}
