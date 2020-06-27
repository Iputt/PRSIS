using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Owl.Feature.iScript;
namespace Owl.Feature
{
    /// <summary>
    /// 内置脚本类型
    /// </summary>
    public enum ScriptType
    {
        /// <summary>
        /// Python
        /// </summary>
        Python,
        /// <summary>
        /// javascript
        /// </summary>
        JavaScript,
        /// <summary>
        /// Lua
        /// </summary>
        Lua
    }
    /// <summary>
    /// 嵌入式脚本
    /// </summary>
    public class Script : Engine<ScriptProvider, Script>
    {
        protected override bool SkipException
        {
            get
            {
                return false;
            }
        }
        protected override EngineMode Mode
        {
            get
            {
                return EngineMode.Single;
            }
        }
        /// <summary>
        /// 当前脚本类型
        /// </summary>
        public static ScriptType ScriptType
        {
            get { return Cache.Thread<ScriptType>("owl.feature.script.type.current", () => ScriptType.JavaScript); }
            set { Cache.Thread("owl.feature.script.type.current", value); }
        }


        /// <summary>
        /// 将指定代码集合编译为一个模块,目前JS有效
        /// </summary>
        /// <param name="name">模块名称</param>
        /// <param name="protectcode">受保护的代码，可被公开的函数调用部分</param>
        /// <param name="functions">公开的方法列表,编译后的方法名为 $.fn. + 原方法名称</param>
        public static void Compile(string name, string protectcode, params Function[] functions)
        {
            if (functions.Length == 0)
                return;
            try
            {
                var provider = Provider;
                if (provider != null)
                    provider.Compile(name, protectcode, functions);
            }
            catch (Exception e)
            {

            }
        }

        public static void CompilePublic(string name, string protectcode, params Function[] functions)
        {
            Compile(string.Format("public|{0}", name), protectcode, functions);
        }

        /// <summary>
        /// 将方法编译进默认模块中
        /// </summary>
        /// <param name="function"></param>
        public static void Compile(Function function)
        {
            Compile("", "", function);
        }
        /// <summary>
        /// 调用指定模块中的方法
        /// </summary>
        /// <param name="name">模块名称</param>
        /// <param name="function">方法名称</param>
        /// <param name="parameters">参数列表</param>
        /// <returns></returns>
        public static object Invoke(string name, string function, params KeyValuePair[] parameters)
        {
            if (string.IsNullOrEmpty(function))
                throw new ArgumentNullException(nameof(function));
            var provider = Provider;
            if (provider != null)
            {
                return provider.Invoke(name, function, parameters.ToDictionary(s => s.Key, s => s.Value));
            }
            return null;
        }

        /// <summary>
        /// 调用默认模块中的方法
        /// </summary>
        /// <param name="function">方法</param>
        /// <param name="parameters">参数列表</param>
        /// <returns></returns>
        public static object Invoke(string function, params KeyValuePair[] parameters)
        {
            return Invoke("", function, parameters);
        }

        /// <summary>
        /// 执行脚本
        /// </summary>
        /// <param name="key">脚本标记、不可重复、可使用GUID</param>
        /// <param name="code">脚本代码</param>
        /// <param name="parameters">外部参数</param>
        /// <returns></returns>
        public static object Execute(string key, string code, IDictionary<string, object> parameters)
        {
            try
            {
                var provider = Provider;
                if (provider != null)
                    return provider.Execute(key, code, parameters);
            }
            catch(Exception ex)
            {
                throw new AlertException(ex.Message, ex);
            }
            return null;
        }

        public static object Execute(string key, string code, params KeyValuePair[] parameters)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));
            var dict = new Dictionary<string, object>();
            foreach (var param in parameters)
                dict[param.Key] = param.Value;
            return Execute(key, code, dict);
        }
    }
}
