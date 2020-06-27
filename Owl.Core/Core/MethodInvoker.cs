using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Linq.Expressions;
using System.Globalization;
using Owl.Feature;

namespace System
{
    public static class MethodInvoker
    {
        public delegate object FastInvokeHandler(object target, object[] paramters);

        static Dictionary<MethodInfo, FastInvokeHandler> handlers = new Dictionary<MethodInfo, FastInvokeHandler>(100);

        /// <summary>
        /// 方法快速执行
        /// </summary>
        /// <param name="method"></param>
        /// <param name="target"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static object FaseInvoke(this MethodInfo method, object target, params object[] args)
        {
            if (!handlers.ContainsKey(method))
            {
                lock (method)
                {
                    if (!handlers.ContainsKey(method))
                    {
                        handlers[method] = GetMethodInvoker(method);
                    }
                }
            }
            var invoker = handlers[method];
            return invoker(target, args);
        }

        private static FastInvokeHandler GetMethodInvoker(MethodInfo method)
        {
            Expression instance = null;
            List<ParameterExpression> parameters = new List<ParameterExpression>();
            var param = Expression.Parameter(typeof(object));
            parameters.Add(param);
            if (!method.IsStatic)
            {
                instance = Expression.Convert(param, method.DeclaringType);
            }

            var arg = Expression.Parameter(typeof(object[]));
            parameters.Add(arg);
            var mps = method.GetParameters();
            List<Expression> mparameters = new List<Expression>();
            for (int i = 0; i < mps.Length; i++)
            {
                var mp = Expression.Convert(Expression.ArrayAccess(arg, Expression.Constant(i)), mps[i].ParameterType);
                mparameters.Add(mp);
            }
            Expression body = null;
            MethodCallExpression call = Expression.Call(instance, method, mparameters);
            if (method.ReturnType == typeof(void))
                body = Expression.Block(call, Expression.Constant(null));
            else if (method.ReturnType.IsValueType)
                body = Expression.Convert(call, typeof(object));
            else
                body = call;

            return Expression.Lambda<FastInvokeHandler>(body, parameters).Compile();
        }
    }
}
