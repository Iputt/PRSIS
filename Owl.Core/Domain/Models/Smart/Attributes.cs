using Owl.Feature;
using Owl.Feature.iScript;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Owl.Domain
{
    public abstract class SmartBehaviorExtention : MetaExtension
    {

    }

    /// <summary>
    /// 表单元数据扩展
    /// </summary>
    public class FormExtension : SmartBehaviorExtention
    {
        /// <summary>
        /// 加载数据时 内置脚本 (self 当前对象,result为返回结果)
        /// </summary>
        public string DefaultScript { get; private set; }

        /// <summary>
        /// 返回状态描述 内置脚本
        /// </summary>
        public string SummaryScript { get; private set; }

        protected string ScriptName
        {
            get
            {
                return string.Format("{0}.form", ModelMeta.Name);
            }
        }
        /// <summary>
        /// 设置脚本
        /// </summary>
        /// <param name="summary"></param>
        /// <param name="default"></param>
        public void SetScript(string summary, string @default)
        {
            SummaryScript = summary;
            DefaultScript = @default;
            List<Function> functions = new List<Function>();
            if (!string.IsNullOrWhiteSpace(summary))
                functions.Add(new Function("summary", summary, "self"));
            if (!string.IsNullOrWhiteSpace(@default))
                functions.Add(new Function("default", @default, "self"));
            Script.Compile(ScriptName, "", functions.ToArray());
        }

        /// <summary>
        /// 加载数据时
        /// </summary>
        public MethodInfo LoadAction { get; set; }


        public string GetSummary(FormObject @object)
        {
            if (!string.IsNullOrWhiteSpace(SummaryScript))
            {
                return Script.Invoke(ScriptName, "summary", new KeyValuePair("self", @object)) as string;
            }
            return "";
        }

        public void BuildDefault(FormObject @object)
        {
            if (LoadAction != null)
                LoadAction.FaseInvoke(null, @object);
            else if (!string.IsNullOrWhiteSpace(DefaultScript))
            {
                Script.Invoke(ScriptName, "default", new KeyValuePair("self", @object));
            }

        }
    }
}
