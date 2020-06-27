using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Owl.Feature.IScript
{
    public abstract class JavascriptProvider : ScriptProvider
    {
        public override ScriptType ScriptType
        {
            get { return ScriptType.JavaScript; }
        }
        /// <summary>
        /// 全局变量名称
        /// </summary>
        protected string GlobalVariableName = "$";

        public override object Execute(int key, string code, IDictionary<string, object> parameters)
        {
            OwlFactory.Current.ClearParameters();
            if (parameters != null)
            {
                foreach (var param in parameters)
                    OwlFactory.Current.SetParameter(param.Key, param.Value);
            }
            try
            {
                return Run(code, OwlFactory.Current);
            }
            catch
            {
            }
            return null;
        }
        public override object ExecuteExpression(int key, string code, IDictionary<string, object> parameters)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 执行代码
        /// </summary>
        /// <param name="code">代码</param>
        /// <param name="globalvariable">全局变量</param>
        /// <returns></returns>
        protected abstract object Run(string code, object globalvariable);

    }
}
