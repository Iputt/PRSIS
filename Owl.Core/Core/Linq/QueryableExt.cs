using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl;
using System.Linq.Expressions;
using System.Reflection;
using System.Collections;
using Owl.Domain;

namespace System.Linq
{
    public class DelegeteComparer<T> : IEqualityComparer<T>
    {
        public Func<T, T, bool> m_func;

        public DelegeteComparer(Func<T, T, bool> func)
        {
            if (func == null)
                throw new ArgumentNullException(nameof(func));
            m_func = func;
        }

        public bool Equals(T x, T y)
        {
            if (x != null && y != null)
                return m_func(x, y);
            if (x == null && y == null)
                return true;
            return false;
        }

        public int GetHashCode(T obj)
        {
            return obj.GetHashCode();
        }
    }

    public static class QueryableExt
    {
        /// <summary>
        /// 求和
        /// </summary>
        /// <typeparam name="TSource">数据类型</typeparam>
        /// <param name="source">源</param>
        /// <param name="selector">参与求和的字段</param>
        /// <returns></returns>
        public static IDictionary<string, object> Sum<TSource>(this IQueryable<TSource> source, params Expression<Func<TSource, object>>[] selector)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (selector == null)
                throw new ArgumentNullException(nameof(selector));
            Expression ex = Expression.NewArrayInit(typeof(Expression<Func<TSource, object>>), selector);
            MethodInfo info = ((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new Type[] { typeof(TSource) });
            MethodCallExpression exp = Expression.Call(info, new Expression[] { source.Expression, ex });
            return source.Provider.Execute<IDictionary<string, object>>(exp);
        }
        /// <summary>
        /// 求和
        /// </summary>
        /// <param name="source">源</param>
        /// <param name="selectors">参与求和的字段</param>
        /// <returns></returns>
        public static IDictionary<string, object> Sum(this IQueryable source, params string[] selectors)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (selectors == null) throw new ArgumentNullException(nameof(selectors));

            Expression ex = Expression.Constant(selectors);
            MethodInfo info = (MethodInfo)MethodBase.GetCurrentMethod();
            MethodCallExpression exp = Expression.Call(info, new Expression[] { source.Expression, ex });
            return source.Provider.Execute<IDictionary<string, object>>(exp);
        }
        /// <summary>
        /// 选择特定字段返回
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="source"></param>
        /// <param name="selectors"></param>
        /// <returns></returns>
        public static IQueryable<TEntity> Select<TEntity>(this IQueryable<TEntity> source, params string[] selectors)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (selectors == null) throw new ArgumentNullException(nameof(selectors));
            if (source is EnumerableQuery<TEntity>)
                return source;
            Expression ex = Expression.Constant(selectors);
            var exp = Expression.Call(typeof(QueryableExt), "Select", new Type[] { source.ElementType }, source.Expression, ex);
            return source.Provider.CreateQuery<TEntity>(exp);
        }
        /// <summary>
        /// 选择特定字段返回
        /// </summary>
        /// <param name="source"></param>
        /// <param name="selectors"></param>
        /// <returns></returns>
        public static IQueryable Select(this IQueryable source, params string[] selectors)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (selectors == null) throw new ArgumentNullException(nameof(selectors));
            if (source is EnumerableQuery)
                return source;
            Expression ex = Expression.Constant(selectors);
            var exp = Expression.Call(typeof(QueryableExt), "Select", new Type[] { source.ElementType }, source.Expression, ex);
            return source.Provider.CreateQuery(exp);
        }
        /// <summary>
        /// 同时加载指定的关系
        /// </summary>
        /// <param name="source"></param>
        /// <param name="relations"></param>
        /// <returns></returns>
        public static IQueryable With(this IQueryable source, params string[] relations)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (relations == null) throw new ArgumentNullException(nameof(relations));
            if (source is EnumerableQuery)
                return source;
            Expression ex = Expression.Constant(relations);
            //MethodInfo info = (MethodInfo)MethodBase.GetCurrentMethod();
            var exp = Expression.Call(typeof(QueryableExt), "With", new Type[] { source.ElementType }, source.Expression, ex);
            return source.Provider.CreateQuery(exp);
        }

        public static IQueryable<TSource> With<TSource>(this IQueryable<TSource> source, params string[] selectors)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (selectors == null) throw new ArgumentNullException(nameof(selectors));
            if (source is EnumerableQuery<TSource>)
                return source;
            Expression ex = Expression.Constant(selectors);
            //MethodInfo info = (MethodInfo)MethodBase.GetCurrentMethod();
            var exp = Expression.Call(typeof(QueryableExt), "With", new Type[] { source.ElementType }, source.Expression, ex);
            return source.Provider.CreateQuery<TSource>(exp);
        }

        public static IEnumerable<TSource> Distinct<TSource>(this IEnumerable<TSource> source, Func<TSource, TSource, bool> compare)
        {
            return source.Distinct(new DelegeteComparer<TSource>(compare));
        }

        public static IQueryable<TransferObject> GroupBy(this IQueryable source, IEnumerable<string> keySelector, IEnumerable<ResultSelector> resultSelector)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
            if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));
            MethodInfo info = (MethodInfo)MethodBase.GetCurrentMethod();
            var exp = Expression.Call(info, new Expression[] { source.Expression, Expression.Constant(keySelector), Expression.Constant(resultSelector) });
            return source.Provider.CreateQuery<TransferObject>(exp);
        }
    }
}
