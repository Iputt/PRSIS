using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using Owl.Util;
using Owl.Domain.Driver;
namespace Owl.Domain.Driver.Repository
{
    public class RepositoryMethod
    {
        public string MethodName { get; private set; }

        Expression m_expression;

        public Type ReturnType { get; set; }

        IEnumerable<ParameterExpression> parameters;

        public IEnumerable<ParameterExpression> Parameters
        {
            get
            {
                return parameters;
            }
        }

        public LambdaExpression LambdaExpression
        {
            get
            {
                if (m_expression != null)
                    return Expression.Lambda(m_expression, parameters);
                return null;
            }
        }

        SortBy m_Sortby = new SortBy();

        IEnumerable<string> Selector;

        IEnumerable<string> Relations;

        int Start;

        int Count;

        LambdaExpression groupselector { get; set; }

        LambdaExpression groupresultselector { get; set; }

        IEnumerable<string> groupselector2 { get; set; }

        IEnumerable<ResultSelector> groupresultselector2 { get; set; }

        void BuildExpression(LambdaExpression expression)
        {
            if (m_expression == null)
            {
                m_expression = expression.Body;
                parameters = expression.Parameters;
            }
            else
                m_expression = Expression.AndAlso(m_expression, expression.Body);
        }
        void BuildMethod(string methodname)
        {
            switch (methodname)
            {
                case "Any":
                case "Sum":
                case "Count":
                case "FirstOrDefault":
                case "LastOrDefault":
                case "GroupBy":
                    MethodName = methodname;
                    break;
                case "Skip":
                case "Take":
                case "Where":
                case "OrderBy":
                case "OrderByDescending":
                case "ThenBy":
                case "ThenByDescending":
                case "Select":
                case "With":
                    break;
                default:
                    throw new NotImplementedException(string.Format("方法{0}未实现", methodname));
            }
        }

        private static T StripQuotes<T>(Expression expression)
            where T : Expression
        {
            while (expression.NodeType == ExpressionType.Quote)
                expression = ((UnaryExpression)expression).Operand;
            return expression as T;
        }
        static Expression StripQuotes(Expression expression)
        {
            while (expression.NodeType == ExpressionType.Quote)
                expression = ((UnaryExpression)expression).Operand;
            return expression;
        }
        static IEnumerable<string> getsetlector(Expression expression)
        {
            List<string> selector = new List<string>();
            var exp = StripQuotes(expression);
            switch (exp.NodeType)
            {
                case ExpressionType.NewArrayInit:
                    selector.AddRange(((NewArrayExpression)exp).Expressions.Select(s => ExprHelper.GetMemberName(StripQuotes<LambdaExpression>(s).Body)));
                    break;
                case ExpressionType.Constant:

                    dynamic value = ((ConstantExpression)exp).Value;
                    if (value is string)
                        selector.Add(value);
                    else
                    {
                        foreach (string s in value)
                        {
                            selector.Add(s);
                        }
                    }
                    break;
                case ExpressionType.Lambda:
                    LambdaExpression bexp = (LambdaExpression)exp;
                    Expression bbody = ExprHelper.StripQuotes(bexp.Body);
                    switch (bbody.NodeType)
                    {
                        case ExpressionType.MemberAccess: selector.Add((bbody as MemberExpression).Member.Name); break;
                        case ExpressionType.New:
                            if (bbody.Type != bexp.Parameters[0].Type)
                                throw new Exception2("select 返回结果只能为" + bexp.Parameters[0].Type.Name);
                            break;
                        case ExpressionType.MemberInit:
                            if (bbody.Type != bexp.Parameters[0].Type)
                                throw new Exception2("select 返回结果只能为" + bexp.Parameters[0].Type.Name);
                            MemberInitExpression iexp = (MemberInitExpression)bbody;
                            foreach (var member in iexp.Bindings)
                            {
                                selector.Add(member.Member.Name);
                            }
                            break;
                    }
                    break;
            }
            return selector;
        }
        public void Build(string methodname, params Expression[] expressions)
        {
            BuildMethod(methodname);
            if (expressions.Length == 0)
                return;
            switch (methodname)
            {
                case "Skip": Start = (int)StripQuotes<ConstantExpression>(expressions[0]).Value; break;
                case "Take": Count = (int)StripQuotes<ConstantExpression>(expressions[0]).Value; break;
                case "Where":
                case "Any":
                case "Count":
                case "FirstOrDefault":
                    BuildExpression(StripQuotes<LambdaExpression>(expressions[0]));
                    break;
                case "Sum":
                case "Select":
                    Selector = getsetlector(expressions[0]);
                    break;
                case "With":
                    Relations = getsetlector(expressions[0]);
                    break;
                case "GroupBy":
                    if (expressions.Length == 2)
                    {
                        var expselector = StripQuotes(expressions[0]);
                        var expresult = StripQuotes(expressions[1]);
                        if (expselector is LambdaExpression)
                            groupselector = expselector as LambdaExpression;
                        else
                            groupselector2 = (expselector as ConstantExpression).Value as IEnumerable<string>;

                        if (expresult is LambdaExpression)
                            groupresultselector = expresult as LambdaExpression;
                        else
                            groupresultselector2 = (expresult as ConstantExpression).Value as IEnumerable<ResultSelector>;

                        m_Sortby.Clear();
                        Start = 0;
                        Count = 0;
                    }
                    break;
                case "ThenBy":
                case "OrderBy":
                    m_Sortby[ExprHelper.GetMemberName(StripQuotes<LambdaExpression>(expressions[0]).Body)] = SortOrder.Ascending;
                    break;
                case "ThenByDescending":
                case "OrderByDescending":
                    m_Sortby[ExprHelper.GetMemberName(StripQuotes<LambdaExpression>(expressions[0]).Body)] = SortOrder.Descending; ;
                    break;
            }
        }

        public object Invoke<TEntity>(ModelMetadata metadata)
            where TEntity : AggRoot
        {
            var repo = RepositoryProviderFactory.CreateProvider<TEntity>(metadata);
            if (Selector == null)
                Selector = new List<string>();
            if (Relations == null)
                Relations = new List<string>();
            Expression<Func<TEntity, bool>> expression = (Expression<Func<TEntity, bool>>)LambdaExpression;
            if (expression == null)
            {
                expression = s => true;
            }
            switch (MethodName)
            {
                case "Count":
                    return repo.Count(expression);
                case "Any":
                    return repo.Exists(expression);
                case "Sum":
                    var record = repo.Sum(expression, Selector.ToArray());
                    if (ReturnType.IsInterface && ReturnType.Name == "IDictionary`2" || ReturnType.GetInterface("IDictory") != null)
                        return record;
                    else
                        return record.Values.FirstOrDefault();
                case "LastOrDefault":
                    if (m_Sortby.Count > 0)
                        return repo.FindLast(expression, m_Sortby, Selector.ToArray());
                    return repo.FindLast(expression, Selector.ToArray());
                case "FirstOrDefault":
                    if (m_Sortby.Count > 0)
                        return repo.FindFirst(expression, m_Sortby, Selector.ToArray());
                    return repo.FindFirst(expression, Selector.ToArray());
                case "GroupBy":
                    if (groupselector != null)
                    {
                        var method = repo.GetType().GetMethod("GroupByExp", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).MakeGenericMethod(groupselector.ReturnType, groupresultselector.ReturnType);
                        return method.FaseInvoke(repo, new object[] { expression, groupselector, groupresultselector, m_Sortby, Start, Count });
                    }
                    else
                    {
                        var method = repo.GetType().GetMethod("DoGroupBy", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                        return method.FaseInvoke(repo, new object[] { expression, groupselector2, groupresultselector2, m_Sortby, Start, Count, false });
                    }
                default:
                    if (m_Sortby.Count > 0)
                        return repo.FindAll(expression, m_Sortby, Start, Count, Selector.ToArray(), Relations.ToArray());
                    return repo.FindAll(expression, new SortBy(), Start, Count, Selector.ToArray(), Relations.ToArray());
            }
        }
    }

    public class ExpressionTranslator : ExpressionVisitor
    {
        RepositoryMethod Info;

        private static T StripQuotes<T>(Expression expression)
           where T : Expression
        {
            while (expression.NodeType == ExpressionType.Quote)
                expression = ((UnaryExpression)expression).Operand;
            return expression as T;
        }
        public RepositoryMethod BuildMethod(Expression expression)
        {
            Info = new RepositoryMethod();
            Info.ReturnType = expression.Type;
            Visit(expression);
            return Info;
        }
        Dictionary<string, ParameterExpression> parameters = new Dictionary<string, ParameterExpression>();
        protected override Expression VisitParameter(ParameterExpression node)
        {
            if (!parameters.ContainsKey(node.Type.FullName))
                parameters[node.Type.FullName] = Expression.Parameter(node.Type);
            return parameters[node.Type.FullName];
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.DeclaringType == typeof(Queryable) || node.Method.DeclaringType == typeof(QueryableExt))
            {
                Visit(node.Arguments[0]);
                Expression[] paras = new Expression[node.Arguments.Count - 1];
                Array.Copy(node.Arguments.ToArray(), 1, paras, 0, paras.Length);
                for (int i = 0; i < paras.Length; i++)
                    paras[i] = Visit(paras[i]);
                Info.Build(node.Method.Name, paras);
                return node;
            }
            return base.VisitMethodCall(node);
        }
    }

    public class SmartQueryProvider<TEntity> : QueryProvider
        where TEntity : AggRoot
    {
        public ModelMetadata ModelData { get; private set; }
        public SmartQueryProvider(ModelMetadata metadata = null)
        {
            ModelData = metadata;
        }
        public override object Execute(Expression expression)
        {
            ExpressionTranslator builder = new ExpressionTranslator();
            RepositoryMethod method = builder.BuildMethod(expression);
            return method.Invoke<TEntity>(ModelData);
        }
        public override string GetQueryText(Expression expression)
        {
            //ExpressionTranslator builder = new ExpressionTranslator();
            //RepositoryMethod method = builder.BuildMethod(expression);
            //Sql.MssqlTranslator trans = new Sql.MssqlTranslator();
            //Sql.QueryCommand cmd = trans.Translate(ModelData, method.LambdaExpression);
            //return cmd.CommandText;
            return "";
        }
    }
}
