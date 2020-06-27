using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
namespace Owl.Domain
{

    public class ExpConvert : ExpressionVisitor
    {
        public static Expression ChangePara(LambdaExpression exp, ParameterExpression parameter)
        {
            ExpConvert convert = new ExpConvert();
            convert.Parameter = parameter;
            LambdaExpression le = convert.Visit(exp) as LambdaExpression;
            return le.Body;
        }
        ParameterExpression Parameter;

        protected override Expression VisitParameter(ParameterExpression node)
        {
            if (node.Type == Parameter.Type)
                return Parameter;
            return node;
            //return base.VisitParameter(node);
        }
    }

    public class ExpressionSpecification<T>
    {
        private ExpressionSpecification() { }

        Expression<Func<T, bool>> m_exp;
        /// <summary>
        /// 表达式树
        /// </summary>
        public Expression<Func<T, bool>> Exp
        {
            get { return m_exp == null ? s => true : m_exp; }
        }

        ParameterExpression parameter;

        public static ExpressionSpecification<T> Create(Expression<Func<T, bool>> exp)
        {
            var spec = new ExpressionSpecification<T>();
            if (exp != null)
            {
                spec.m_exp = exp;
                spec.parameter = exp.Parameters.FirstOrDefault();
            }
            return spec;
        }
        /// <summary>
        /// Where
        /// </summary>
        /// <param name="exp"></param>
        /// <returns></returns>
        public ExpressionSpecification<T> Where(Expression<Func<T, bool>> exp)
        {
            if (exp != null)
            {
                if (m_exp == null)
                {
                    m_exp = exp;
                    parameter = exp.Parameters.FirstOrDefault();
                }
                else
                {
                    m_exp = Expression.Lambda<Func<T, bool>>(Expression.AndAlso(m_exp.Body, ExpConvert.ChangePara(exp, parameter)), parameter);
                }
            }
            return this;
        }
    }
}
