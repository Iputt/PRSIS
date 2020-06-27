using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using Owl.Util;

namespace Owl.Domain.Driver.Repository
{
    /// <summary>
    /// 表达是树缓存key
    /// </summary>
    public class CacheKey : ExpressionVisitor
    {
        #region
        public class CacheData
        {
            public int HashCode { get; set; }

            public bool HasRelation { get; set; }

            public TranslatorParameter CreateParameter(object value)
            {
                return parameters.Create(value);
            }

            TranslatorParameterCollection parameters = new TranslatorParameterCollection();
            /// <summary>
            /// 参数列表
            /// </summary>
            public TranslatorParameterCollection Parameters { get { return parameters; } }

        }

        public void AddHash(int hash)
        {
            cacheData.HashCode = cacheData.HashCode + hash;
        }

        public override int GetHashCode()
        {
            return cacheData.HashCode;
        }
        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is CacheKey))
                return false;
            return GetHashCode() == obj.GetHashCode();
        }

        /// <summary>
        /// 相关参数
        /// </summary>
        public TranslatorParameterCollection Parameters
        {
            get { return cacheData.Parameters; }
        }

        int index;
        CacheData cacheData { get; set; }
        public CacheKey(Expression node, CacheData data = null, int index = 0)
        {
            if (data == null)
            {
                data = new CacheData();
                index = 0;
            }
            cacheData = data;
            this.index = index;
            Visit(node);
        }
        public bool HasRelation
        {
            get
            {
                return cacheData.HasRelation;
            }
        }
        #endregion

        #region 解析表达式
        public override Expression Visit(Expression node)
        {
            switch (node.NodeType)
            {
                case ExpressionType.Not:
                    Visit((node as UnaryExpression).Operand);
                    break;
                case ExpressionType.Lambda:
                    cacheData.HashCode += ((LambdaExpression)node).Parameters[0].Type.FullName.GetHashCode();
                    Visit(((LambdaExpression)node).Body);
                    break;
                default:
                    return base.Visit(node);
            }
            return node;
        }

        void _visit(Expression exp, Type type)
        {
            if (exp.NodeType == ExpressionType.Lambda)
            {
                new CacheKey(exp, cacheData, index);
                return;
            }
            if (!ExprHelper.IsMember(exp))
                exp = ExprHelper.BuildConstant(exp, type);
            Visit(exp);
        }


        protected override Expression VisitBinary(BinaryExpression node)
        {
            var left = ExprHelper.StripQuotes(node.Left);
            var right = ExprHelper.StripQuotes(node.Right);
            _visit(left, right.Type);
            cacheData.HashCode += node.NodeType.ToString().GetHashCode() * index;
            _visit(right, left.Type);
            return node;
        }
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if ((node.Method.Name == "GetReferences" || node.Method.Name == "get_Item" || node.Method.Name == "GetEntities") && node.Object.NodeType == ExpressionType.Parameter)
            {
                _visitmember(node.Object, (string)ExprHelper.GetValue(node.Arguments[0]));
                cacheData.HasRelation = true;
            }
            else
            {
                Expression left, right;
                Type ltype, rtype;

                if (node.Method.IsStatic)
                {
                    left = node.Arguments[0];
                    right = node.Arguments[1];
                    var param = node.Method.GetParameters();
                    ltype = param[0].ParameterType;
                    rtype = param[1].ParameterType;
                }
                else
                {
                    left = node.Object;
                    right = node.Arguments[0];
                    var param = node.Method.GetParameters();
                    ltype = left.Type;
                    rtype = param[0].ParameterType;
                }
                _visit(left, rtype);
                cacheData.HashCode += (rtype.GetHashCode() + node.Method.Name.GetHashCode()) * index;
                _visit(right, ltype);
            }
            return node;
        }
        protected override Expression VisitConstant(ConstantExpression node)
        {
            index += 1;
            cacheData.HashCode += cacheData.CreateParameter(node.Value).Name.GetHashCode() * index;
            return node;
        }
        string getprefix(Expression exp)
        {
            if (exp.NodeType == ExpressionType.MemberAccess)
            {
                var mexp = exp as MemberExpression;
                var prefix = getprefix(mexp.Expression);
                return prefix == null ? mexp.Member.Name : prefix + "." + mexp.Member.Name;
            }
            return null;
        }
        void _visitmember(Expression exp, string name)
        {
            index += 1;
            var prefix = getprefix(exp);
            var mname = prefix == null ? name : prefix + "." + name;
            cacheData.HashCode += mname.GetHashCode() * index;
            if (cacheData.HasRelation)
                return;
            if (exp.NodeType == ExpressionType.MemberAccess && exp.Type.IsSubclassOf(typeof(AggRoot)))
                cacheData.HasRelation = true;
        }
        protected override Expression VisitMember(MemberExpression node)
        {
            _visitmember(node.Expression, node.Member.Name);
            if (node.Type.Name == "AggregateCollection`1")
                cacheData.HasRelation = true;
            return node;
        }
        #endregion
    }
}
