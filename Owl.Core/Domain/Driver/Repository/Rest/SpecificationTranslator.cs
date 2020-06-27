using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using Owl.Util;

namespace Owl.Domain.Driver.Repository
{
    public class SpecifactionTranslater : ExpressionVisitor
    {
        Specification specific = null;

        public Specification Translate(Expression expression)
        {
            specific = Specification.Null;
            Visit(expression);
            return specific;
        }

        bool isMember(MemberExpression member)
        {
            if (member.Expression.NodeType == ExpressionType.Parameter)
                return true;
            if (member.Expression.NodeType == ExpressionType.MemberAccess)
                return isMember(member.Expression as MemberExpression);
            return false;
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            var parameter = ExprHelper.GetParas(node).FirstOrDefault();
            if (parameter != null)
            {
                var names = node.ToString().Split(new char[] { '.' }, 2);
                string member = names[1];
                int level = 0;
                for (var i = 0; i < parameters.Count; i++)
                {
                    if (object.Equals(parameter, parameters.ElementAt(i)))
                    {
                        level = i - 1;
                        break;
                    }
                }
                if (level >= 0)
                    member = string.Format("p{0}.{1}", level, member);
                specific = Specification.Member(member);
            }
            else
                specific = Specification.Constant(ExprHelper.GetValue(node));
            return node;
        }
        protected override Expression VisitConstant(ConstantExpression node)
        {
            specific = Specification.Constant(node.Value);
            return node;
        }
        protected override Expression VisitBinary(BinaryExpression node)
        {
            Visit(node.Left);
            var left = specific;
            Visit(node.Right);
            var right = specific;

            switch (node.NodeType)
            {
                case ExpressionType.AndAlso: specific = Specification.And(left, right); break;
                case ExpressionType.OrElse: specific = Specification.Or(left, right); break;
                case ExpressionType.Equal: specific = Specification.Compare(left, CmpCode.EQ, right); break;
                case ExpressionType.NotEqual: specific = Specification.Compare(left, CmpCode.NE, right); break;
                case ExpressionType.LessThan: specific = Specification.Compare(left, CmpCode.LT, right); break;
                case ExpressionType.LessThanOrEqual: specific = Specification.Compare(left, CmpCode.LTE, right); break;
                case ExpressionType.GreaterThan: specific = Specification.Compare(left, CmpCode.GT, right); break;
                case ExpressionType.GreaterThanOrEqual: specific = Specification.Compare(left, CmpCode.GTE, right); break;
                case ExpressionType.Coalesce: specific = Specification.Math(left, '?', right); break;
                case ExpressionType.Add: specific = Specification.Math(left, '+', right); break;
                case ExpressionType.Subtract: specific = Specification.Math(left, '-', right); break;
                case ExpressionType.Multiply: specific = Specification.Math(left, '*', right); break;
                case ExpressionType.Modulo: specific = Specification.Math(left, '%', right); break;
                case ExpressionType.Divide: specific = Specification.Math(left, '/', right); break;
                case ExpressionType.Power: specific = Specification.Math(left, '^', right); break;
            }
            return node;
        }
        protected override Expression VisitUnary(UnaryExpression node)
        {
            if (node.NodeType == ExpressionType.Not)
            {
                Visit(node.Operand);
                specific = Specification.Not(specific);
                return node;
            }
            else
                return base.VisitUnary(node);
        }
        Stack<ParameterExpression> parameters = new Stack<ParameterExpression>();
        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            parameters.Push(node.Parameters[0]);
            Visit(node.Body);
            parameters.Pop();
            return node.Body;
        }
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (ExprHelper.IsMember(node))
            {
                Expression left = null, right = null;
                if (node.Method.IsStatic)
                {
                    left = node.Arguments[0];
                    right = node.Arguments[1];
                }
                else
                {
                    left = node.Object;
                    right = node.Arguments[0];
                }
                if (node.Method.Name == "GetReferences" || node.Method.Name == "get_Item")
                {
                    if (left.NodeType == ExpressionType.Parameter)
                    {
                        string value = (string)ExprHelper.GetValue(right);
                        specific = Specification.Member(value);
                    }
                }
                else
                {
                    Visit(left);
                    var lspc = specific;
                    Visit(right);
                    var rspc = specific;
                    switch (node.Method.Name)
                    {
                        case "GreaterThan": specific = Specification.Compare(lspc, CmpCode.GT, rspc); break;
                        case "GreaterThanOrEqual": specific = Specification.Compare(lspc, CmpCode.GTE, rspc); break;
                        case "LessThan": specific = Specification.Compare(lspc, CmpCode.LT, rspc); break;
                        case "LessThanOrEqual": specific = Specification.Compare(lspc, CmpCode.LTE, rspc); break;
                        case "Contains": specific = Specification.Compare(lspc, CmpCode.Con, rspc); break;
                        case "StartsWith": specific = Specification.Compare(lspc, CmpCode.Start, rspc); break;
                        case "EndsWith": specific = Specification.Compare(lspc, CmpCode.End, rspc); break;
                        case "Any": specific = Specification.Any(lspc.ToString(), rspc); break;
                        case "In":specific = Specification.Compare(lspc, CmpCode.IN, rspc);break;
                        default: specific = Specification.Method(lspc, node.Method, rspc); break;
                    }
                }
            }
            else
                specific = Specification.Constant(ExprHelper.GetValue(node));
            return node;
        }
    }
}
