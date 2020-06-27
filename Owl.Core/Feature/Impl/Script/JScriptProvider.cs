using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Owl.Util;
using System.Collections.Concurrent;
namespace Owl.Feature.iScript
{
    public enum ContextRenewType
    {
        /// <summary>
        /// 不关闭
        /// </summary>
        None,
        /// <summary>
        /// 执行次数
        /// </summary>
        Times,
        /// <summary>
        /// 执行分钟数
        /// </summary>
        Miniutes,
    }

    public class ScriptContext<TContext>
        where TContext : class
    {
        /// <summary>
        /// 上下文重建类型
        /// </summary>
        /// <value>The type.</value>
        public ContextRenewType Type { get; private set; }

        /// <summary>
        /// 最大数量限制
        /// </summary>
        /// <value>The count.</value>
        public int MaxCount { get; private set; }

        /// <summary>
        /// 开始时间
        /// </summary>
        /// <value>The start time.</value>
        public DateTime StartTime { get; set; }
        /// <summary>
        /// 实际上下文
        /// </summary>
        public TContext Context { get; private set; }

        /// <summary>
        /// 调用计数器
        /// </summary>
        protected int CounterStart { get; private set; }

        /// <summary>
        /// 完成计数器
        /// </summary>
        protected int CounterFinish { get; private set; }

        public ScriptContext(TContext context, ContextRenewType type, int? max)
        {
            Context = context;
            Type = type;
            MaxCount = max ?? 0;
            CounterStart = 0;
            CounterFinish = 0;
            StartTime = DateTime.Now;
        }
        /// <summary>
        /// 开始本次调用,若以达到最大调用次数则返回false
        /// </summary>
        public bool StartInvoke()
        {
            if (Type == ContextRenewType.Times)
            {
                lock (Context)
                {
                    if (CounterStart == MaxCount)
                    {
                        return false;
                    }
                    CounterStart++;
                }
            }
            else if (Type == ContextRenewType.Miniutes)
            {
                lock (Context)
                {
                    return (DateTime.Now - StartTime).TotalMinutes <= MaxCount;
                }
            }
            return true;
        }
    }

    public abstract class JavascriptProvider<TContext> : ScriptProvider
        where TContext : class
    {
        public sealed override ScriptType ScriptType
        {
            get { return ScriptType.JavaScript; }
        }
        /// <summary>
        /// 上下文重建类型
        /// </summary>
        /// <value>The type of the renew.</value>
        protected abstract ContextRenewType RenewType { get; }
        /// <summary>
        /// 最大数量
        /// </summary>
        /// <value>The max count.</value>
        protected virtual int MaxCount { get { return 5000; } }
        readonly object contextlocker = new object();
        ScriptContext<TContext> m_context;
        ScriptContext<TContext> _Create()
        {
            ScriptRuntime.Current.Ext["require"] = (Action<string, string>)Require;
            var ctx = CreateCtx(new KeyValuePair("$", ScriptRuntime.Current));
            Run(ctx, "var $_fn={};var owl={};var require = $.Ext.require;");
            return new ScriptContext<TContext>(ctx, RenewType, MaxCount);
        }
        /// <summary>
        /// 获取可调用的context
        /// </summary>
        /// <returns></returns>
        public ScriptContext<TContext> GetforInvoke()
        {
            var isnew = false;
            if (m_context == null)
            {
                lock (contextlocker)
                {
                    if (m_context == null)
                    {
                        m_context = _Create();
                        isnew = true;
                    }
                }
            }
            else if (!m_context.StartInvoke())
            {
                var context_last = m_context;
                lock (contextlocker)
                {
                    if (m_context == context_last)
                    {
                        isnew = true;
                        CloseCtx(m_context.Context);
                        m_context = _Create();
                    }
                }
            }
            if (isnew)
                m_context.StartInvoke();
            return m_context;
        }

        protected string FormatCode(string code)
        {
            if (string.IsNullOrEmpty(code))
                return code;
            List<string> lines = new List<string>();
            using (StringReader reader = new StringReader(code))
            {
                while (reader.Peek() > -1)
                {
                    var tline = reader.ReadLine();
                    int? forindex = null;
                    string forstat = "";
                    foreach (var stat in tline.Split(';'))
                    {
                        var tmp = stat.Trim();
                        if (tmp.StartsWith("for(", StringComparison.Ordinal))
                        {
                            forindex = 0;
                        }
                        if (forindex.HasValue)
                        {
                            forindex = forindex + 1;
                            forstat = forindex == 1 ? tmp : forstat + ";" + tmp;
                            if (forindex == 3)
                            {
                                lines.Add(forstat);
                                forindex = null;
                                forstat = "";
                            }
                        }
                        else if (!string.IsNullOrEmpty(tmp))
                            lines.Add(tmp);
                    }
                }
            }
            var last = lines[lines.Count - 1];
            string lastsec = "";
            if (lines.Count > 1)
                lastsec = lines[lines.Count - 2];
            if (!last.StartsWith("return ", StringComparison.Ordinal) &&
                !last.EndsWith("}", StringComparison.Ordinal) &&
                !last.StartsWith("if(", StringComparison.Ordinal) &&
                !last.StartsWith("else(", StringComparison.Ordinal) &&
                !last.StartsWith("for(", StringComparison.Ordinal) &&
                !last.StartsWith("var", StringComparison.Ordinal) &&
                !last.Contains("="))
            {
                if (lines.Count == 1 || (lines.Count > 1 && !lastsec.StartsWith("if(", StringComparison.Ordinal) && !lastsec.StartsWith("else(", StringComparison.Ordinal) && !lastsec.StartsWith("for(")))
                {
                    //需要返回值
                    var index = code.LastIndexOf(last, StringComparison.Ordinal);
                    return string.Format("{0} return {1}", code.Substring(0, index), last);
                }
            }
            return code;
        }
        readonly ConcurrentDictionary<string, ScriptBlock> scriptblocks = new ConcurrentDictionary<string, ScriptBlock>();
        public ScriptBlock Getblock(string name)
        {
            return scriptblocks.GetOrAdd(name, s => new ScriptBlock(name, "") { Id = scriptblocks.Count + 1 });
        }

        protected void _DoCompile(TContext ctx, ScriptBlock block)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("(function(){");
            var ispublic = block.Name.StartsWith("public|");
            var blockname = ispublic ? block.Name.Substring(7) : block.Name;
            if (block.Name.StartsWith("public|"))
                sb.AppendLine("owl." + blockname + " = {}");
            sb.AppendLine(block.Protect);
            List<Function> functions = new List<Function>();
            foreach (var func in block.Functions.Values.Where(s => !s.IsCompiled(ctx)))
            {
                if (ispublic)
                {
                    sb.AppendFormat("owl.{4}.{5} = function({2}){0}{3}{1};", "{", "}", string.Join(",", func.Parameters), FormatCode(func.Code), blockname, func.Name);
                }
                else
                {
                    var param = string.Join("", func.Parameters.Select(s => string.Format("var {0}=$.Ctx.Param.Val('{0}');", s)));
                    sb.AppendFormat("$_fn._{4}_{5} = function(){0}{2}{3}{1};", "{", "}", param, FormatCode(func.Code), block.Id, func.Id);
                }
                sb.AppendLine();
                functions.Add(func);
            }
            sb.Append("})();");
            if (!ispublic)
            {
                foreach (var func in functions)
                {
                    sb.AppendFormat("var $_fn_{0}_{1}=$_fn._{0}_{1};", block.Id, func.Id);
                }
            }

            var code = sb.ToString();
            Compile(ctx, code);
            foreach (var func in functions)
            {
                func.Compiled = true;
                func.CompiledContext = ctx;
            }

        }
        public sealed override void Compile(string name, string protectcode, params Function[] functions)
        {
            var block = Getblock(name);
            if (string.IsNullOrEmpty(name))
            {
                if (block.FunctionChange(functions))
                {
                    lock (block)
                    {
                        if (block.FunctionChange(functions))
                            block.Update("", functions);
                    }
                }
            }
            else
            {
                var tmp = new ScriptBlock(name, protectcode, functions);
                if (block.GetHashCode() != tmp.GetHashCode())
                {
                    lock (block)
                    {
                        if (block.GetHashCode() != tmp.GetHashCode())
                        {
                            block.Update(protectcode, functions.ToArray());
                        }
                    }
                }
            }
        }
        protected Tuple<ScriptBlock, Function> CheckCompile(TContext ctx, DateTime invoketime, string name, string function)
        {
            var block = Getblock(name);
            Function func;
            lock (block)
            {
                if (block.Functions.ContainsKey(function))
                {
                    func = block.Functions[function];
                    if (!func.IsCompiled(ctx) && invoketime > func.Time)
                        _DoCompile(ctx, block);
                }
                else
                {
                    throw new AlertException("调用的方法不存在！");
                }
            }
            return new Tuple<ScriptBlock, Function>(block, func);
        }
        object InnerInvoke(TContext ctx, DateTime invoketime, string name, string function, IDictionary<string, object> parameters)
        {
            var check = CheckCompile(ctx, invoketime, name, function);
            var block = check.Item1;
            var func = check.Item2;
            ScriptRuntime.Current.Ctx.Param.Clear();
            if (parameters != null)
            {
                foreach (var param in parameters)
                    ScriptRuntime.Current.Ctx.Param[param.Key] = param.Value;
            }
            ScriptRuntime.Current.Ctx.Param["__context__"] = ctx;
            return CallFunction(ctx, string.Format("$_fn_{0}_{1}", block.Id, func.Id));
            //var code = string.Format("$_fn_{0}_{1}({2})", block.Id, func.Id, string.Join(",", parameters.Select(s => string.Format("$.Ctx.Param.Val('{0}')", s.Key))));
            //return Run(ctx, code);
        }

        public void Require(string name, string function)
        {
            name = string.Format("public|{0}", name);
            var ctx = ScriptRuntime.Current.Ctx.Param.ContainsKey("__context__") ? ScriptRuntime.Current.Ctx.Param.GetRealValue<TContext>("__context__") : GetforInvoke().Context;
            CheckCompile(ctx, DateTime.Now, name, function);
        }

        public sealed override object Invoke(string name, string function, IDictionary<string, object> parameters)
        {
            var ctx = GetforInvoke();
            var invoketime = DateTime.Now;
            try
            {
                return InnerInvoke(ctx.Context, invoketime, name, function, parameters);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
        }

        public sealed override object Execute(string key, string code, IDictionary<string, object> parameters)
        {
            Compile("", "", new Function(key, code, parameters.Keys.ToArray()));
            System.Threading.Thread.Sleep(10);
            return Invoke("", key, parameters);
        }

        /// <summary>
        /// 创建上下文
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        protected abstract TContext CreateCtx(params KeyValuePair[] parameters);

        /// <summary>
        /// 关闭上下文，释放资源
        /// </summary>
        /// <param name="ctx"></param>
        protected abstract void CloseCtx(TContext ctx);

        /// <summary>
        /// 编译代码
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="code"></param>
        protected abstract void Compile(TContext ctx, string code);
        /// <summary>
        /// 执行表达式
        /// </summary>
        /// <param name="code">代码</param>
        /// <returns></returns>
        protected abstract object Run(TContext ctx, string code);

        protected abstract object CallFunction(TContext ctx, string functionName);
    }
}
