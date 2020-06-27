using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Util;
namespace Owl.Feature.iScript
{
    /// <summary>
    /// Script 方法
    /// </summary>
    public class Function
    {
        /// <summary>
        /// 函数在脚本块中的Id
        /// </summary>
        /// <value>The identifier.</value>
        public int Id { get; set; }
        /// <summary>
        /// 方法名称
        /// </summary>
        public string Name { get; private set; }
        /// <summary>
        /// 代码
        /// </summary>
        public string Code { get; private set; }

        /// <summary>
        /// 参数表名称
        /// </summary>
        public IEnumerable<string> Parameters { get; private set; }

        /// <summary>
        /// 发生变化时间
        /// </summary>
        public DateTime Time { get; private set; }

        bool m_compiled;
        /// <summary>
        /// 是否已编译
        /// </summary>
        public bool Compiled
        {
            get { return m_compiled; }
            set
            {
                if (m_compiled && value == false)
                    Time = DateTime.Now;
                m_compiled = value;
            }
        }
        /// <summary>
        /// 编译的上下文
        /// </summary>
        /// <value>The compiled context.</value>
        public object CompiledContext { get; set; }

        /// <summary>
        /// 是否在指定的上下文中编译过
        /// </summary>
        /// <returns><c>true</c>, if compiled was ised, <c>false</c> otherwise.</returns>
        /// <param name="context">Context.</param>
        public bool IsCompiled(object context)
        {
            return context == CompiledContext && Compiled; 
        }

        public Function(string name, string code, params string[] parameter)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));
            if (string.IsNullOrEmpty(code))
                throw new ArgumentNullException(nameof(code));
            Name = name;
            Code = code.Trim();
            Parameters = parameter;
            Compiled = false;
            Time = DateTime.Now;
        }

        public void Update(string code, params string[] parameter)
        {
            if (Code != code || string.Join(",", Parameters) != string.Join(",", parameter))
                Compiled = false;
            Code = code;
            Parameters = parameter;
        }

        public bool IsEqual(Function func)
        {
            return func.Name == Name && func.Code == Code && string.Join(",", func.Parameters) == string.Join(",", Parameters);
        }
    }
    /// <summary>
    /// 脚本块，闭包
    /// </summary>
    public class ScriptBlock
    {
        /// <summary>
        /// 脚本块在上下文中Id
        /// </summary>
        /// <value>The identifier.</value>
        public int Id { get; set; }
        /// <summary>
        /// 块名称
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// 块内受保护代码
        /// </summary>
        public string Protect { get; private set; }

        /// <summary>
        /// 公开接口函数
        /// </summary>
        public Dictionary<string, Function> Functions { get; private set; }

        public ScriptBlock(string name, string protect, params Function[] functions)
        {
            Name = name;
            Protect = protect.Coalesce("").Trim();
            Functions = functions.ToDictionary(s => s.Name);
            if (string.IsNullOrEmpty(protect) && functions.Length == 0)
                hashcode = 0;
        }
        int? hashcode;
        public override int GetHashCode()
        {
            if (hashcode == null)
                hashcode = (Protect + string.Join("", Functions.Values.Select(s => s.Name + s.Code))).GetHashCode();
            return hashcode.Value;
        }
        public bool FunctionChange(Function[] functions)
        {
            foreach (var func in functions)
            {
                if (!Functions.ContainsKey(func.Name) || !Functions[func.Name].IsEqual(func))
                    return true;
            }
            return false;
        }
        int FuncIndex = 0;
        /// <summary>
        /// 更新脚本代码块,默认模块执行增量更新
        /// </summary>
        /// <param name="protect"></param>
        /// <param name="functions"></param>
        public void Update(string protect, params Function[] functions)
        {
            var recompile = false;
            protect = string.IsNullOrEmpty(Name) ? "" : protect.Coalesce("").Trim();
            if (Protect != protect)
                recompile = true;
            Protect = protect;
            hashcode = null;
            var dict = new Dictionary<string, Function>();
            foreach (var obj in functions)
            {
                Function func = obj;
                if (Functions.ContainsKey(obj.Name))
                {
                    func = Functions[obj.Name];
                    func.Update(obj.Code, obj.Parameters.ToArray());
                }
                else
                {
                    FuncIndex++;
                    obj.Id = FuncIndex;
                    Functions[obj.Name] = obj;
                }
                if (recompile)
                    func.Compiled = false;
                dict[obj.Name] = obj;
            }
            if (!string.IsNullOrEmpty(Name))
            {
                foreach (var key in Functions.Keys.ToList())
                {
                    if (!dict.ContainsKey(key))
                        Functions.Remove(key);
                }
            }
        }
        /// <summary>
        /// 重新编译
        /// </summary>
        public void ReCompile()
        {
            foreach (var pair in Functions.Values)
                pair.Compiled = false;
        }
    }

    public abstract class ScriptProvider : Provider
    {
        /// <summary>
        /// 脚本类型
        /// </summary>
        public abstract ScriptType ScriptType { get; }

        public override bool IsValid
        {
            get { return ScriptType == Script.ScriptType; }
        }

        /// <summary>
        /// 将指定代码集合编译为一个模块
        /// </summary>
        /// <param name="name">模块名称</param>
        /// <param name="protectcode">受保护的代码，可被公开的函数调用部分</param>
        /// <param name="functions">公开的方法列表</param>
        public virtual void Compile(string name, string protectcode, params Function[] functions) { }

        /// <summary>
        /// 调用编译后的方法
        /// </summary>
        /// <param name="name"></param>
        /// <param name="function"></param>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public virtual object Invoke(string name, string function, IDictionary<string, object> parameters) { return null; }

        public abstract object Execute(string key, string code, IDictionary<string, object> parameters);
    }

}
