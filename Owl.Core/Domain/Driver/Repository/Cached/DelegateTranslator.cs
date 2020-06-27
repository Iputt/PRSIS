using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Reflection;
using Owl.Util;
using System.Collections;
using Owl.Feature;
namespace Owl.Domain.Driver.Repository
{

    public class DelegateCommand
    {
        TranslatorParameterCollection m_parameters;

        public Delegate Expresssion { get; set; }

        public bool HasRelation { get; set; }

        public DelegateCommand()
        {
            m_parameters = new TranslatorParameterCollection();
        }

        public DelegateCommand(Delegate expression, TranslatorParameterCollection para)
        {
            if (expression == null)
                throw new ArgumentNullException("text");
            if (para == null)
                throw new ArgumentNullException("para");
            Expresssion = expression;
            m_parameters = para;
        }

        public TranslatorParameter CreateParameter(object value)
        {
            return m_parameters.Create(value);
        }

        public Dictionary<string, object> Parameters
        {
            get
            {
                Dictionary<string, object> dict = new Dictionary<string, object>();
                foreach (var para in m_parameters)
                {
                    dict[para.Name] = para.Value;
                }
                return dict;
            }
        }
    }

    public class DelegateTranslator : ExpressionVisitor
    {
        static readonly SyncDictionary<CacheKey, Delegate> commands = new SyncDictionary<CacheKey, Delegate>();

        public DelegateCommand Cmd { get; private set; }
        CacheKey key;
        public DelegateTranslator(Expression node)
        {
            key = new CacheKey(node);
            if (AppConfig.Section.Debug)
                Log.Info(DateTime.Now, "表达式缓存", string.Format("{0}\t{1}", key.GetHashCode(), node));
            if (commands.ContainsKey(key))
            {
                Cmd = new DelegateCommand(commands[key], key.Parameters);
                Cmd.HasRelation = key.HasRelation;
            }
            else
            {
                Cmd = new DelegateCommand();
                var tnode = ((LambdaExpression)Visit(node));
                Cmd.Expresssion = tnode.Compile();
                if (key != null)
                    commands[key] = Cmd.Expresssion;
                Cmd = new DelegateCommand(Cmd.Expresssion, key.Parameters);
                Cmd.HasRelation = key.HasRelation;
            }
        }
        public override Expression Visit(Expression node)
        {
            switch (node.NodeType)
            {
                case ExpressionType.AndAlso:
                    return Expression.AndAlso(Visit((node as BinaryExpression).Left), Visit((node as BinaryExpression).Right));
                case ExpressionType.OrElse:
                    return Expression.OrElse(Visit((node as BinaryExpression).Left), Visit((node as BinaryExpression).Right));
                case ExpressionType.Not:
                    return Expression.Not(Visit((node as UnaryExpression).Operand));
                case ExpressionType.Lambda:
                    LambdaExpression exp = node as LambdaExpression;
                    return Expression.Lambda(Visit(exp.Body), exp.Parameters[0], dictpara);
                default:
                    return base.Visit(node);
            }
        }

        private static MethodInfo get_item = typeof(Dictionary<string, object>).GetMethod("get_Item");
        private ParameterExpression dictpara = Expression.Parameter(typeof(Dictionary<string, object>));
        protected override Expression VisitConstant(ConstantExpression node)
        {
            var param = Cmd.CreateParameter(node.Value);
            if (node.Value == null)
                return Expression.Call(dictpara, get_item, Expression.Constant(param.Name));
            return Expression.Convert(Expression.Call(dictpara, get_item, Expression.Constant(param.Name)), node.Type);
        }

        Expression _visit(Expression exp, Type type)
        {
            if (exp.NodeType == ExpressionType.Lambda)
            {
                var lexp = exp as LambdaExpression;
                return Expression.Lambda(Visit(lexp.Body), lexp.Parameters);
            }
            if (!ExprHelper.IsMember(exp))
                exp = Expression.Convert(Expression.Constant(null), type == typeof(object) ? exp.Type : type);
            else if (exp.Type == typeof(object))
                exp = Expression.Convert(exp, type);
            return Visit(exp);
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            Expression left = ExprHelper.StripQuotes(node.Left);
            Expression right = ExprHelper.StripQuotes(node.Right);
            var tl = _visit(left, right.Type);
            var tr = _visit(right, left.Type);
            return ExprHelper.Create(tl, node.NodeType, tr);
        }
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if ((node.Method.Name == "GetReferences" || node.Method.Name == "get_Item") && node.Object.NodeType == ExpressionType.Parameter)
                return node;
            Expression left;
            Expression right;
            if (node.Method.IsStatic)
            {
                left = node.Arguments[0];
                right = node.Arguments[1];
                var param = node.Method.GetParameters();
                return node.Update(node.Object, new Expression[] { _visit(left, param[0].ParameterType), _visit(right, param[1].ParameterType) });
            }
            else
            {
                left = node.Object;
                right = node.Arguments[0];
                var param = node.Method.GetParameters();
                return node.Update(_visit(left, left.Type), new Expression[] { _visit(right, param[0].ParameterType) });
            }
        }
    }
}
