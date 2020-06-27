using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Globalization;
using System.Linq.Expressions;
using Owl.Util;

namespace Owl.Domain
{
    /// <summary>
    /// 快速调用器
    /// </summary>
    internal abstract class FastInvoker
    {
        /// <summary>
        /// 设置对象的值
        /// </summary>
        /// <param name="entity">对象</param>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        public abstract void SetValue(object entity, string key, object value);
        /// <summary>
        /// 获取对象的值
        /// </summary>
        /// <param name="entity">对象</param>
        /// <param name="key">键</param>
        /// <returns></returns>
        public abstract object GetValue(object entity, string key);
        /// <summary>
        /// 从数据库中加载对象
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="record"></param>
        public abstract void ParsefromDb(object entity, IDictionary<string, object> record);

        /// <summary>
        /// 是否包含键
        /// </summary>
        public abstract bool ContainsKey(string key);

        static Dictionary<Type, FastInvoker> instances = new Dictionary<Type, FastInvoker>(100);

        /// <summary>
        /// 获取快速调用器
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static FastInvoker GetInstance(Type type)
        {
            if (!instances.ContainsKey(type))
            {
                instances[type] = Activator.CreateInstance(typeof(FastInvoker<>).MakeGenericType(type)) as FastInvoker;
            }
            return instances[type];
        }

        public abstract TResult ExecuteFunc<TResult>(Delegate func, object entity);

        protected static Type dicttype = typeof(IDictionary<string, object>);
        protected static MethodInfo containkey = dicttype.GetMethod("ContainsKey");
        protected static MethodInfo getitem = dicttype.GetMethod("get_Item", new Type[] { typeof(string) });
        protected static MethodInfo setitem = dicttype.GetMethod("set_Item", new Type[] {
			typeof(string),
			typeof(object)
		});
        protected static MethodInfo log = typeof(Console).GetMethod("WriteLine", new Type[] { typeof(string) });
    }

    internal class FastInvoker<TEntity> : FastInvoker
        where TEntity : class
    {

        #region private static common field and method

        static Type type = typeof(TEntity);
        static Dictionary<string, PropertyInfo> Properties;
        static Action<TEntity, IDictionary<string, object>> parsefromdbaction;
        static Func<TEntity, string, object> getvalueaction;
        static Action<TEntity, string, object> setvalueaction;

        static FastInvoker()
        {
            Properties = TypeHelper.GetProperties(type);
            parsefromdbaction = getparsefromdbaction();
            getvalueaction = getgetvalueaction();
            setvalueaction = getsetvalueaction();
        }

        #endregion

        #region db property get set;

        /// <summary>
        /// 获取从数据库中读取的对单个字段赋值的表达式
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="dict"></param>
        /// <param name="info"></param>
        /// <returns></returns>
        static Expression getfieldexpressionfordb(ParameterExpression entity, ParameterExpression dict, PropertyInfo info)
        {
            var member = Expression.MakeMemberAccess(entity, info);
            var value = Expression.Call(dict, getitem, Expression.Constant(info.Name));
            var ptype = TypeHelper.StripType(info.PropertyType);
            var isnullable = TypeHelper.IsNullable(info.PropertyType);
            var vexp = Expression.Parameter(typeof(object));
            var assign = Expression.Assign(member, Expression.Convert(vexp, info.PropertyType));
            if (ptype == typeof(float) || ptype == typeof(double))
                assign = Expression.Assign(member, ExprHelper.ChangeType(info.PropertyType, vexp));
            var assigndefault = Expression.Assign(member, Expression.Convert(Expression.Constant(TypeHelper.Default(info.PropertyType)), info.PropertyType));
            Expression body = null;

            if (ptype.Name == "String")
            {
                body = Expression.IfThen(
                    ExprHelper.NotNull(vexp),
                    Expression.IfThenElse(
                        Expression.TypeIs(vexp, typeof(string)),
                        assign,
                        Expression.Assign(member, ExprHelper.ToString(vexp))
                    )
                );
                body = Expression.IfThenElse(ExprHelper.NotNull(vexp), assign, assigndefault);
            }
            else if (ptype.IsValueType || TypeHelper.GetParseMethod(ptype) != null)
            {
                body = Expression.IfThen(
                    ExprHelper.NotNull(vexp),
                    Expression.IfThenElse(
                        Expression.TypeIs(vexp, typeof(string)),
                        Expression.IfThen(
                            ExprHelper.NotEmpty(vexp),
                            Expression.Assign(member, ExprHelper.Parse(info.PropertyType, vexp))
                        ),
                        assign
                    )
                );
            }
            else
                body = Expression.IfThenElse(ExprHelper.NotNull(vexp), assign, assigndefault);
            //if (ptype.IsEnum)
            //{
            //    body = Expression.IfThen(
            //        ExprHelper.NotNull(vexp),
            //        Expression.IfThenElse(
            //            Expression.TypeIs(vexp, typeof(string)),
            //            Expression.IfThen(
            //                ExprHelper.NotEmpty(vexp),
            //                Expression.Assign(member, ExprHelper.Parse(info.PropertyType, vexp))
            //            ),
            //            assign
            //        )
            //    );
            //}
            //else
            //{
            //    body = Expression.IfThenElse(ExprHelper.NotNull(vexp), assign, assigndefault);
            //}
            return Expression.Block(
                new[] { vexp },
                Expression.IfThen(
                    Expression.Call(dict, containkey, Expression.Constant(info.Name)),
                    Expression.Block(
                //Expression.Call(null,log,Expression.Constant(info.Name)),
                        Expression.Assign(vexp, value),
                        body
                    )
                )
            );
        }

        static Expression getdomainexpressionfordb(DomainModel metedata, ParameterExpression entity, ParameterExpression dict)
        {
            List<Expression> expressions = new List<Expression>();
            foreach (var field in metedata.GetFields(s => s.Field_Type != FieldType.one2many && s.Field_Type != FieldType.many2many))
            {
                string fieldname = field.Name;
                var property = field.PropertyInfo;
                if (field.Field_Type == FieldType.many2one)
                {
                    fieldname = field.PrimaryField;
                    property = null;
                }
                if (property == null)
                    property = metedata.ModelType.GetProperty(fieldname, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (property == null)
                    continue;

                expressions.Add(getfieldexpressionfordb(entity, dict, property));
            }
            return Expression.Block(expressions);
        }

        static Action<TEntity, IDictionary<string, object>> getparsefromdbaction()
        {
            var metedata = MetaEngine.GetModel(typeof(TEntity));
            if (metedata == null || (metedata.ObjType != DomainType.Entity && metedata.ObjType != DomainType.AggRoot))
                return null;
            ParameterExpression paraexp = Expression.Parameter(metedata.ModelType);
            ParameterExpression vexp = Expression.Parameter(dicttype);
            var expression = getdomainexpressionfordb(metedata, paraexp, vexp);
            return Expression.Lambda<Action<TEntity, IDictionary<string, object>>>(expression, paraexp, vexp).Compile();
        }

        #endregion

        #region property set get

        static Func<TEntity, string, object> getgetvalueaction()
        {
            var infos = TypeHelper.GetProperties(type);
            if (infos.Count == 0)
                return (s, a) => null;
            var entity = Expression.Parameter(type);
            var key = Expression.Parameter(typeof(string));
            var result = Expression.Parameter(typeof(object));

            List<SwitchCase> switchcases = new List<SwitchCase>();
            HashSet<string> pinfos = new HashSet<string>();
            foreach (PropertyInfo info in infos.Values)
            {
                if (pinfos.Contains(info.Name))
                    continue;
                pinfos.Add(info.Name);
                switchcases.Add(Expression.SwitchCase(
                    Expression.Assign(result, Expression.Convert(Expression.Property(entity, info), typeof(object))),
                    Expression.Constant(info.Name)
                ));
            }
            var body = Expression.Block(
                typeof(object),
                new[] { result },
                Expression.Switch(key, Expression.Assign(result, Expression.Constant(null)), switchcases.ToArray()),
                result
            );
            return Expression.Lambda<Func<TEntity, string, object>>(body, entity, key).Compile();
        }

        static Expression getsetpropertyexpression(ParameterExpression entity, ParameterExpression value, PropertyInfo info)
        {
            var ptype = TypeHelper.StripType(info.PropertyType);
            var member = Expression.Property(entity, info);
            var assign = Expression.Assign(member, Expression.Convert(value, info.PropertyType));

            Expression body = null;
            if (ptype.IsValueType || TypeHelper.GetParseMethod(ptype) != null)
            {
                body = Expression.IfThenElse(
                    Expression.TypeIs(value, ptype),
                    assign,
                    Expression.IfThenElse(
                        Expression.TypeIs(value, typeof(string)),
                        Expression.IfThenElse(
                            Expression.AndAlso(ExprHelper.NotEmpty(value), ExprHelper.NotEqual(value, "-")),
                            Expression.Assign(member, ExprHelper.Parse(info.PropertyType, value)),
                            Expression.Assign(member, Expression.Default(info.PropertyType))
                        ),
                        Expression.Assign(member, ptype.IsValueType ? ExprHelper.ChangeType2(info.PropertyType, value) : Expression.Default(info.PropertyType))
                    )
                );
            }
            else
            {
                body = Expression.IfThenElse(
                    Expression.TypeIs(value, ptype),
                    assign,
                    Expression.Assign(member, Expression.Default(info.PropertyType))
                );
                //body = assign;
            }
            return Expression.IfThenElse(
                ExprHelper.NotNull(value),
                body,
                Expression.Assign(member, Expression.Default(info.PropertyType))
            );
        }

        static Action<TEntity, string, object> getsetvalueaction()
        {
            var infos = Properties.Values.Where(s => s.CanWrite).ToList();
            if (infos.Count == 0)
                return (s, a, o) => { };

            var entity = Expression.Parameter(type);
            var key = Expression.Parameter(typeof(string));
            var value = Expression.Parameter(typeof(object));

            List<SwitchCase> switchcases = new List<SwitchCase>();
            HashSet<string> pinfos = new HashSet<string>();
            foreach (PropertyInfo info in infos)
            {
                if (pinfos.Contains(info.Name) || !info.CanWrite)
                    continue;
                pinfos.Add(info.Name);

                var swichcase = Expression.SwitchCase(
                    Expression.Block(typeof(void), getsetpropertyexpression(entity, value, info)),
                    Expression.Constant(info.Name)
                );
                switchcases.Add(swichcase);
            }
            Expression body = Expression.Block(Expression.Switch(key, switchcases.ToArray()));
            return Expression.Lambda<Action<TEntity, string, object>>(body, entity, key, value).Compile();
        }

        #endregion

        #region 实现基类

        public sealed override void SetValue(object entity, string key, object value)
        {
            SetValue(entity as TEntity, key, value);
        }

        public sealed override object GetValue(object entity, string key)
        {
            return GetValue(entity as TEntity, key);
        }

        public sealed override void ParsefromDb(object entity, IDictionary<string, object> record)
        {
            ParsefromDb(entity as TEntity, record);
        }

        public sealed override bool ContainsKey(string key)
        {
            return Properties.ContainsKey(key);
        }

        public override TResult ExecuteFunc<TResult>(Delegate func, object entity)
        {
            return ((Func<TEntity, TResult>)func)(entity as TEntity);
        }

        #endregion

        public static void SetValue(TEntity entity, string key, object value)
        {
            setvalueaction(entity, key, value);
        }

        public static object GetValue(TEntity entity, string key)
        {
            return getvalueaction(entity, key);
        }

        public static void ParsefromDb(TEntity entity, IDictionary<string, object> record)
        {
            parsefromdbaction(entity, record);
        }
    }
}
