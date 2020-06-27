using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Reflection;
using System.Collections;
using System.Globalization;
using Owl.Domain;

namespace Owl.Util
{
    internal class ParamVisitor : ExpressionVisitor
    {
        List<ParameterExpression> parameters = new List<ParameterExpression>();
        protected override Expression VisitParameter(ParameterExpression node)
        {
            parameters.Add(node);
            return node;
        }
        /// <summary>
        /// 获取表达式数的参数列表
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public static List<ParameterExpression> GetParams(Expression node)
        {
            var pv = new ParamVisitor();
            pv.Visit(node);
            return pv.parameters;
        }
    }

    public class MemberVistor : ExpressionVisitor
    {
        Stack<string> members = new Stack<string>();
        ParameterExpression param = null;
        protected override Expression VisitParameter(ParameterExpression node)
        {
            param = node;
            return node;
        }
        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Expression.NodeType == ExpressionType.MemberAccess && node.Member.Name == "Value" && node.Member.DeclaringType.Name == "Nullable`1")
            {
                return VisitMember(node.Expression as MemberExpression);
            }
            members.Push(node.Member.Name);
            return base.VisitMember(node);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.Name == "GetReferences" || node.Method.Name == "get_Item" || node.Method.Name == "GetEntities")
            {
                string value = (string)ExprHelper.GetValue(node.Arguments[0]);
                members.Push(value);
            }
            return base.VisitMethodCall(node);
        }

        public static Tuple<ParameterExpression, Stack<string>> GetMembers(Expression node)
        {
            var mv = new MemberVistor();
            mv.Visit(node);
            return new Tuple<ParameterExpression, Stack<string>>(mv.param, mv.members);
        }
    }

    public class MemberConditionVistor : ExpressionVisitor
    {
        public ModelMetadata MetaData { get; private set; }

        public LambdaExpression Result { get; private set; }

        protected List<Expression> Instances { get; private set; }
        public MemberConditionVistor(LambdaExpression express)
        {
            Result = Visit(express) as LambdaExpression;
        }
        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            var metadata = MetaData;
            MetaData = ModelMetadata.GetModel(node.Parameters[0].Type);

            var instances = Instances;
            Instances = Instances == null ? new List<Expression>() : new List<Expression>(Instances);

            var exp = base.VisitLambda(node);
            MetaData = metadata;
            Instances = instances;
            return exp;
        }
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.Name == "Any")
            {
                Expression left = node.Arguments[0];
                Expression right = node.Arguments[1];

                var fieldname = "";

                if (left.NodeType == ExpressionType.MemberAccess)
                {
                    fieldname = (left as MemberExpression).Member.Name;
                    Instances.Add((left as MemberExpression).Expression);
                }
                else if (left.NodeType == ExpressionType.Call && (left as MethodCallExpression).Method.Name == "GetReferences")
                    fieldname = ((left as MethodCallExpression).Arguments[0] as ConstantExpression).Value as string;
                var navfield = MetaData.GetField(fieldname) as NavigatField;
                if (navfield != null && navfield.RelationField != navfield.RelationModelMeta.PrimaryField.Name && navfield.Specific != null)
                {
                    LambdaExpression lambda = right as LambdaExpression;
                    // var tmpexp = navfield.Specific.GetExpression(navfield.RelationModelMeta, lambda.Parameters.FirstOrDefault(), new ParameterExpression[] { (left as MemberExpression).Expression as ParameterExpression });
                    var tmpexp = navfield.Specific.GetExpression(navfield.RelationModelMeta, lambda.Parameters.FirstOrDefault(), Instances.AsEnumerable().Reverse().ToArray());
                    right = Expression.Lambda(Expression.AndAlso(lambda.Body, tmpexp.Body), lambda.Parameters.ToArray());
                    return node.Update(node.Object, new Expression[] { left, right });
                }
            }
            return base.VisitMethodCall(node);
        }
    }

    public static class ExprHelper
    {
        #region 常用方法
        /// <summary>
        /// 字符串的contains方法
        /// </summary>
        public static readonly MethodInfo Str_Contains = TypeHelper.GetMethod(typeof(string), "Contains");

        /// <summary>
        /// 多选字符串的contains方法
        /// </summary>
        public static readonly MethodInfo Array_Contains = ObjectExt.GetMethod("ArrayContains");
        /// <summary>
        /// in方法
        /// </summary>
        public static readonly MethodInfo Ext_In = ObjectExt.GetMethod("In");
        /// <summary>
        /// 字符串的大于方法扩展
        /// </summary>
        public static readonly MethodInfo Str_GT = ObjectExt.GetMethod("GreaterThan");
        /// <summary>
        /// 字符串的大于等于方法扩展
        /// </summary>
        public static readonly MethodInfo Str_GTE = ObjectExt.GetMethod("GreaterThanOrEqual");
        /// <summary>
        /// 字符串的小于方法扩展
        /// </summary>
        public static readonly MethodInfo Str_LT = ObjectExt.GetMethod("LessThan");
        /// <summary>
        /// 字符串的小于等于方法扩展
        /// </summary>
        public static readonly MethodInfo Str_LTE = ObjectExt.GetMethod("LessThanOrEqual");

        /// <summary>
        /// 
        /// </summary>
        public static readonly MethodInfo Str_Start = typeof(string).GetMethod("StartsWith", new Type[] { typeof(string) });
        /// <summary>
        /// 
        /// </summary>
        public static readonly MethodInfo Str_End = typeof(string).GetMethod("EndsWith", new Type[] { typeof(string) });
        /// <summary>
        /// 字符串合并方法
        /// </summary>
        public static readonly MethodInfo Str_Add = typeof(string).GetMethod("Concat", new Type[] { typeof(string), typeof(string) });

        /// <summary>
        /// 时间加法
        /// </summary>
        public static readonly MethodInfo DT_Add = typeof(DTHelper).GetMethod("Add");

        /// <summary>
        /// 时间减法
        /// </summary>
        public static readonly MethodInfo DT_Subtract = typeof(DTHelper).GetMethod("Subtract");

        static MethodInfo anymethod = typeof(Enumerable).GetMethods().FirstOrDefault(s => s.Name == "Any" && s.GetParameters().Length == 2);
        public static MethodInfo GetAnyMethod(Type modeltype)
        {
            return anymethod.MakeGenericMethod(modeltype);
        }
        public static MethodInfo GetIn(Type fieldtype)
        {
            return Ext_In.MakeGenericMethod(fieldtype);
        }

        static MethodInfo MethodGetReferences = typeof(Owl.Domain.AggRoot).GetMethod("GetReferences", BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[] { typeof(string) }, null);
        static Dictionary<Type, MethodInfo> MethodsGetReferences = new Dictionary<Type, MethodInfo>();
        /// <summary>
        /// 获取 领域对象的 GetReferences方法
        /// </summary>
        /// <param name="generictype"></param>
        /// <returns></returns>
        public static MethodInfo GetReferences(Type generictype)
        {
            if (!MethodsGetReferences.ContainsKey(generictype))
                MethodsGetReferences[generictype] = MethodGetReferences.MakeGenericMethod(generictype);
            return MethodsGetReferences[generictype];
        }

        static MethodInfo MethodGetReference = typeof(Owl.Domain.DomainObject).GetMethod("GetReference", BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[] { typeof(string) }, null);
        static Dictionary<Type, MethodInfo> MethodsGetReference = new Dictionary<Type, MethodInfo>();
        /// <summary>
        /// 获取 领域对象的GetReference 方法
        /// </summary>
        /// <param name="generictype"></param>
        /// <returns></returns>
        public static MethodInfo GetReference(Type generictype)
        {
            if (!MethodsGetReference.ContainsKey(generictype))
                MethodsGetReference[generictype] = MethodGetReference.MakeGenericMethod(generictype);
            return MethodsGetReference[generictype];
        }

        static MethodInfo MethodSetReference = typeof(Owl.Domain.DomainObject).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic).FirstOrDefault(s => s.Name == "SetReference" && s.ContainsGenericParameters && s.GetParameters().Count() == 2);
        static Dictionary<Type, MethodInfo> MethodsSetReference = new Dictionary<Type, MethodInfo>();
        /// <summary>
        /// 获取 领域对象的SetReference 方法
        /// </summary>
        /// <param name="generictype"></param>
        /// <returns></returns>
        public static MethodInfo SetReference(Type generictype)
        {
            if (!MethodsSetReference.ContainsKey(generictype))
                MethodsSetReference[generictype] = MethodSetReference.MakeGenericMethod(generictype);
            return MethodsSetReference[generictype];
        }
        static MethodInfo MethodGetGetEntities = typeof(Owl.Domain.DomainObject).GetMethod("GetEntities", BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[] { typeof(string) }, null);
        static Dictionary<Type, MethodInfo> MethodsGetEntities = new Dictionary<Type, MethodInfo>();
        /// <summary>
        /// 获取 领域对象的 GetEntities 方法
        /// </summary>
        /// <param name="generictype"></param>
        /// <returns></returns>
        public static MethodInfo GetEntities(Type generictype)
        {
            if (!MethodsGetEntities.ContainsKey(generictype))
                MethodsGetEntities[generictype] = MethodGetGetEntities.MakeGenericMethod(generictype);
            return MethodsGetEntities[generictype];
        }
        #endregion
        public static Expression StripQuotes(Expression e)
        {
            if (e == null)
                return null;
            while (e.NodeType == ExpressionType.Quote || e.NodeType == ExpressionType.Convert)
            {
                e = ((UnaryExpression)e).Operand;
            }
            return e;
        }
        public static MemberExpression GetMember(Expression exp)
        {
            exp = StripQuotes(exp);
            if (exp is MemberExpression)
                return exp as MemberExpression;
            return null;
        }
        public static string GetMemberName(Expression exp)
        {
            exp = StripQuotes(exp);
            if (exp is MemberExpression)
            {
                return (exp as MemberExpression).Member.Name;
            }
            return "";
        }

        /// <summary>
        /// 获取成员名称（包含子字段）
        /// </summary>
        /// <param name="exp"></param>
        /// <returns></returns>
        public static string GetMemberName(MemberExpression exp)
        {
            return string.Join(".", MemberVistor.GetMembers(exp).Item2);
        }
        private static object InvokeMethod(MethodCallExpression exp)
        {
            var obj = exp.Object == null ? null : GetValue(exp.Object);
            var arg = exp.Arguments.Select(s => GetValue(s)).ToArray();
            return exp.Method.FaseInvoke(obj, arg);
        }
        private static object InvokeMember(MemberExpression exp)
        {
            if (exp.Member.MemberType == MemberTypes.Field)
            {
                return ((FieldInfo)exp.Member).GetValue(GetValue(exp.Expression));
            }
            else if (exp.Member.MemberType == MemberTypes.Property)
            {
                var obj = GetValue(exp.Expression);
                if (obj is Expression)
                    return exp;
                var member = ((PropertyInfo)exp.Member);
                var get = member.GetGetMethod();
                if ((get == null || !get.IsStatic) && obj == null)
                    return exp;
                var value = member.GetValue(obj, null);
                return value;
            }
            return null;
        }
        private static object InvokeArray(NewArrayExpression exp)
        {
            return exp.Expressions.Select(s => GetValue(s));
        }
        /// <summary>
        /// 获取算数运算的值
        /// </summary>
        /// <param name="exp"></param>
        /// <param name="op"></param>
        /// <returns></returns>
        private static object InvokeMath(BinaryExpression exp, char op)
        {
            var left = GetValue(exp.Left);
            var right = GetValue(exp.Right);
            if (exp.Method != null)
            {
                if (exp.Method.IsStatic)
                    return exp.Method.FaseInvoke(null, left, right);
                else
                    return exp.Method.FaseInvoke(left, right);
            }
            else
            {
                dynamic lt = left;
                dynamic rt = right;
                switch (op)
                {
                    case '+': return lt + rt;
                    case '-': return lt - rt;
                    case '*': return lt * rt;
                    case '/': return lt / rt;
                    case '%': return lt % rt;
                    case '^': return lt ^ rt;
                }
            }
            return null;
        }

        /// <summary>
        /// 获取表达式的值
        /// </summary>
        /// <param name="exp">支持类型为 constant,call,memberaccess,newarrayinit</param>
        /// <returns></returns>
        public static object GetValue(Expression exp)
        {
            if (exp == null)
                return null;
            exp = StripQuotes(exp);
            switch (exp.NodeType)
            {
                case ExpressionType.Constant: return ((ConstantExpression)exp).Value;
                case ExpressionType.Call: return InvokeMethod((MethodCallExpression)exp);
                case ExpressionType.MemberAccess: return InvokeMember((MemberExpression)exp);
                case ExpressionType.NewArrayInit: return InvokeArray((NewArrayExpression)exp);
                case ExpressionType.Coalesce: return GetValue(((BinaryExpression)exp).Left) ?? GetValue(((BinaryExpression)exp).Right);
                case ExpressionType.Equal: return object.Equals(GetValue(((BinaryExpression)exp).Left), GetValue(((BinaryExpression)exp).Right));
                case ExpressionType.NotEqual: return !object.Equals(GetValue(((BinaryExpression)exp).Left), GetValue(((BinaryExpression)exp).Right));
                case ExpressionType.GreaterThan: return ObjectExt.Compare(GetValue(((BinaryExpression)exp).Left), GetValue(((BinaryExpression)exp).Right)) > 0;
                case ExpressionType.GreaterThanOrEqual: return ObjectExt.Compare(GetValue(((BinaryExpression)exp).Left), GetValue(((BinaryExpression)exp).Right)) >= 0;
                case ExpressionType.LessThan: return ObjectExt.Compare(GetValue(((BinaryExpression)exp).Left), GetValue(((BinaryExpression)exp).Right)) < 0;
                case ExpressionType.LessThanOrEqual: return ObjectExt.Compare(GetValue(((BinaryExpression)exp).Left), GetValue(((BinaryExpression)exp).Right)) <= 0;
                case ExpressionType.Parameter: return exp;
                case ExpressionType.Add: return InvokeMath(exp as BinaryExpression, '+');
                case ExpressionType.Subtract: return InvokeMath(exp as BinaryExpression, '-');
                case ExpressionType.Multiply: return InvokeMath(exp as BinaryExpression, '*');
                case ExpressionType.Divide: return InvokeMath(exp as BinaryExpression, '/');
                case ExpressionType.Modulo: return InvokeMath(exp as BinaryExpression, '%');
                case ExpressionType.Power: return InvokeMath(exp as BinaryExpression, '^');
            }
            return null;
        }

        public static bool Compare(object left, ExpressionType type, object right)
        {
            var b = ObjectExt.Compare(left, right);
            switch (type)
            {
                case ExpressionType.Equal: return b == 0;
                case ExpressionType.Not: return b != 0;
                case ExpressionType.GreaterThan: return b > 0;
                case ExpressionType.GreaterThanOrEqual: return b >= 0;
                case ExpressionType.LessThan: return b < 0;
                case ExpressionType.LessThanOrEqual: return b <= 0;
            }
            return false;
        }
        public static object GetValue(Expression member, Expression exp)
        {
            object value = GetValue(exp);
            MemberExpression mem = (MemberExpression)StripQuotes(member);
            if (mem == null || !mem.Type.IsEnum)
                return value;
            return Convert.ChangeType(value, mem.Type);
        }
        /// <summary>
        /// 判断表达式是否为表达式树中参数的成员
        /// </summary>
        /// <param name="exp"></param>
        /// <returns></returns>
        public static bool IsMember(Expression exp)
        {
            return ParamVisitor.GetParams(exp).Count > 0;
        }

        public static Tuple<ParameterExpression, Stack<string>> GetMembers(Expression exp)
        {
            return MemberVistor.GetMembers(exp);
        }

        public static IEnumerable<ParameterExpression> GetParas(Expression exp)
        {
            return ParamVisitor.GetParams(exp);
        }
        public static Type GetFuncBool(Type parametertype)
        {
            return typeof(Func<,>).MakeGenericType(parametertype, typeof(bool));
        }

        public static Expression BuildConstant(Expression exp, Type type)
        {
            var value = ExprHelper.GetValue(exp);
            if (value != null)
            {
                var ptype = Nullable.GetUnderlyingType(type) ?? type;
                if (ptype.IsEnum && value is int)
                    value = Enum.ToObject(ptype, value);
            }
            return Expression.Constant(value);
        }

        static Dictionary<ExpressionType, MethodInfo> expressionmethod = new Dictionary<ExpressionType, MethodInfo>(100);
        static Type exprtype = typeof(Expression);
        static MethodInfo getMethod(ExpressionType type)
        {
            MethodInfo info = null;
            if (!expressionmethod.ContainsKey(type))
            {
                info = exprtype.GetMethod(type.ToString(), new Type[] { exprtype, exprtype });
                expressionmethod[type] = info;
            }
            else
                info = expressionmethod[type];
            return info;
        }

        public static BinaryExpression Create(Expression tl, ExpressionType type, Expression tr)
        {
            switch (type)
            {
                case ExpressionType.Equal: return Expression.Equal(tl, tr);
                case ExpressionType.NotEqual: return Expression.NotEqual(tl, tr);
                case ExpressionType.GreaterThan: return Expression.GreaterThan(tl, tr);
                case ExpressionType.GreaterThanOrEqual: return Expression.GreaterThanOrEqual(tl, tr);
                case ExpressionType.LessThan: return Expression.LessThan(tl, tr);
                case ExpressionType.LessThanOrEqual: return Expression.LessThanOrEqual(tl, tr);
                case ExpressionType.Add: return Expression.Add(tl, tr);
                case ExpressionType.Subtract: return Expression.Subtract(tl, tr);
                case ExpressionType.Multiply: return Expression.Multiply(tl, tr);
                case ExpressionType.Divide: return Expression.Divide(tl, tr);
                default: return (BinaryExpression)getMethod(type).Invoke(null, new object[] { tl, tr });
            }
        }

        /// <summary>
        /// 从字符串解析
        /// </summary>
        /// <param name="type">解析的类型</param>
        /// <param name="value">字符串的值</param>
        /// <returns></returns>
        public static Expression Parse(Type type, Expression value)
        {
            var vtype = TypeHelper.StripType(type);
            var info = TypeHelper.GetParseMethod(vtype);
            value = Expression.Convert(value, typeof(string));
            Expression call = null;
            if (TypeHelper.IsNumeric(vtype, true))
            {
                var numbestyle = Expression.Constant(NumberStyles.AllowThousands | NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign);
                call = Expression.Call(null, info, value, numbestyle);
            }
            else if (vtype.IsEnum || vtype == typeof(byte[]))
                call = Expression.Call(null, info, Expression.Constant(vtype), value);
            else
                call = Expression.Call(null, info, value);
            if (vtype.IsEnum && type.IsGenericType)
                return Expression.Convert(Expression.Convert(call, vtype), type);
            return Expression.Convert(call, type);
        }

        static MethodInfo changetype = typeof(Convert).GetMethod("ChangeType", new Type[] { typeof(object), typeof(Type) });
        /// <summary>
        /// 类型转换
        /// </summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Expression ChangeType(Type type, Expression value)
        {
            var vtype = TypeHelper.StripType(type);
            Expression body = null;
            if (vtype.IsEnum)
                body = value;
            else
                body = Expression.Call(null, changetype, value, Expression.Constant(vtype));
            return Expression.Convert(body, type);
        }
        static MethodInfo changetype2 = typeof(Convert2).GetMethod("ChangeType", new Type[] { typeof(object), typeof(Type) });
        public static Expression ChangeType2(Type type, Expression value)
        {
            var vtype = TypeHelper.StripType(type);
            Expression body = null;
            if (vtype.IsEnum)
                body = value;
            else
                body = Expression.Call(null, changetype2, value, Expression.Constant(vtype));
            return Expression.Convert(body, type);
        }

        static MethodInfo tostring = typeof(string).GetMethod("Format", new Type[] { typeof(string), typeof(object) });
        public static Expression ToString(Expression value)
        {
            return Expression.Call(null, tostring, Expression.Constant("{0}"), value);
        }

        /// <summary>
        /// 不为空值
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Expression NotEmpty(Expression value)
        {
            return Expression.NotEqual(Expression.Convert(value, typeof(string)), Expression.Constant(""));
        }

        /// <summary>
        /// 不等于
        /// </summary>
        /// <param name="value"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static Expression NotEqual(Expression value, string obj)
        {
            return Expression.NotEqual(Expression.Convert(value, typeof(string)), Expression.Constant(obj));
        }

        /// <summary>
        /// 不为空
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Expression NotNull(Expression value)
        {
            return Expression.NotEqual(value, Expression.Constant(null));
        }
    }
}
