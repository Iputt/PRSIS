using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using Owl.Feature;
using Owl.Domain.Driver;
using Owl.Util;
namespace Owl.Domain
{
    /// <summary>
    /// 比较运算符
    /// </summary>
    public enum CmpCode
    {
        /// <summary>
        /// 等于
        /// </summary>
        [DomainLabel("=")]
        EQ,
        /// <summary>
        /// 大于
        /// </summary>
        [DomainLabel(">")]
        GT,
        /// <summary>
        /// 大于等于
        /// </summary>
        [DomainLabel(">=")]
        GTE,
        /// <summary>
        /// 小于
        /// </summary>
        [DomainLabel("<")]
        LT,
        /// <summary>
        /// 小于等于
        /// </summary>
        [DomainLabel("<=")]
        LTE,
        /// <summary>
        /// 不等于
        /// </summary>
        [DomainLabel("!=")]
        NE,
        /// <summary>
        /// 在集合中
        /// </summary>
        [DomainLabel("in")]
        IN,
        /// <summary>
        /// 包含
        /// </summary>
        [DomainLabel("包含")]
        Con,
        /// <summary>
        /// 字段为多选时包含
        /// </summary>
        [DomainLabel("多选包含")]
        Conm,
        /// <summary>
        /// 起始于
        /// </summary>
        [DomainLabel("开始于")]
        Start,
        /// <summary>
        /// 结束于
        /// </summary>
        [DomainLabel("结束于")]
        End
    }

    public static class CmpHelper
    {
        public static string toString(this CmpCode code)
        {
            switch (code)
            {
                case CmpCode.EQ: return "=";
                case CmpCode.GT: return ">";
                case CmpCode.GTE: return ">=";
                case CmpCode.LT: return "<";
                case CmpCode.LTE: return "<=";
                case CmpCode.NE: return "<>";
                case CmpCode.IN: return "in";
                case CmpCode.Con: return "con";
                case CmpCode.Start: return "start";
                case CmpCode.End: return "end";
                default: return "";
            }
        }
        /// <summary>
        /// 从字符串解析
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static CmpCode Parse(string input)
        {
            input = (input ?? "").ToLower();
            switch (input)
            {
                case "=": return CmpCode.EQ;
                case ">":
                case "gt":
                    return CmpCode.GT;
                case ">=":
                case "gte":
                    return CmpCode.GTE;
                case "<":
                case "lt":
                    return CmpCode.LT;
                case "<=":
                case "lte":
                    return CmpCode.LTE;
                case "in": return CmpCode.IN;
                case "<>":
                case "!=": return CmpCode.NE;
                case "con": return CmpCode.Con;
                case "conm": return CmpCode.Conm;
                case "start": return CmpCode.Start;
                case "end": return CmpCode.End;
                default: return CmpCode.EQ;
            }
        }
    }

    /// <summary>
    /// 表达式
    /// </summary>
    public abstract class Specification
    {
        public static Specification Null { get { return null; } }

        #region 帮助
        protected static string[] getArrary(string arstring)
        {
            List<string> array = new List<string>();
            int ac = 0;
            int tc = 0;
            List<char> current = new List<char>();
            foreach (var c in arstring)
            {
                switch (c)
                {
                    case '[': ac += 1; break;
                    case ']': ac -= 1; break;
                    case '(': tc += 1; break;
                    case ')': tc -= 1; break;
                    case ',':
                        if (ac == 0 && tc == 0)
                        {
                            array.Add(new string(current.ToArray()));
                            current.Clear();
                            continue;
                        }
                        break;
                }
                current.Add(c);
            }
            array.Add(new string(current.ToArray()));
            return array.ToArray();
        }
        /// <summary>
        /// 解析变量
        /// </summary>
        /// <param name="input">输入表达式</param>
        /// <param name="value">转换结果</param>
        /// <returns>是否成功</returns>
        protected static bool TryParseValue(string input, out object value)
        {
            if (input.StartsWith("'") && input.EndsWith("'"))
                value = input.Substring(1, input.Length - 2);
            else if (input.StartsWith("[") && input.EndsWith("]"))
            {
                List<object> objs = new List<object>();
                foreach (var exp in getArrary(input.Substring(1, input.Length - 2)))
                {
                    object obj;
                    if (TryParseValue(exp, out obj))
                        objs.Add(obj);
                }
                value = objs;
            }
            else if (input.ToLower() == "false")
                value = false;
            else if (input.ToLower() == "true")
                value = true;
            else if (input.ToLower() == "null")
                value = null;
            else if (StringHelper.IsDigits(input))
                value = int.Parse(input);
            else if (StringHelper.IsNumber(input))
                value = float.Parse(input);
            else
            {
                value = null;
                return false;
            }
            return true;
        }
        #endregion

        #region 创建表达式
        static Specification createcompare(params string[] array)
        {
            if (array.Length != 1 && array.Length != 3)
                return null;
            AtomSpecification Left = null, Right = null;
            CmpCode? Code = CmpCode.EQ;
            if (array.Length == 1)
            {
                Code = CmpCode.EQ;
                if (array[0].StartsWith("!"))
                {
                    Left = AtomSpecification.Atom(array[0].Substring(1));
                    Right = new ConstantSpecification(false);
                }
                else
                {
                    if (array[0].Contains(','))
                    {
                        throw new AlertException("owl.domian.expression2.atom.novalid", "表达式 {0} 不是有效的原子表达式", array[0]);
                    }
                    Left = AtomSpecification.Atom(array[0]);
                    Right = new ConstantSpecification(true);
                }
            }
            else if (array.Length == 3)
            {
                Left = AtomSpecification.Atom(array[0]);
                Code = CmpHelper.Parse(array[1]);
                Right = AtomSpecification.Atom(array[2]);
            }
            return new CompareSpecification(Left, Code.Value, Right);
        }
        /// <summary>
        /// 从字符串创建规格
        /// </summary>
        /// <param name="specification">规格字符串</param>
        /// <returns></returns>
        public static Specification Create(string specification)
        {
            if (string.IsNullOrWhiteSpace(specification))
                return null;
            specification = specification.Trim();
            if (specification == "true")
                return Specification.Constant(true);
            if (specification == "false")
                return Specification.Constant(false);
            if (specification.StartsWith("(") && specification.EndsWith(")"))
            {
                return createcompare(getArrary(specification.Substring(1, specification.Length - 2)));
            }
            else if (specification.StartsWith("[") && specification.EndsWith("]"))
            {
                var sarr = getArrary(specification.Substring(1, specification.Length - 2));
                if (sarr.Length == 1)
                    return Create(sarr[0]);
                List<Specification> specifications = new List<Specification>();
                for (int i = 1; i < sarr.Length; i++)
                {
                    specifications.Add(Create(sarr[i]));
                }
                switch (sarr[0].Trim().Replace("'", ""))
                {
                    case "Or":
                    case "or":
                    case "|": return Or(specifications.ToArray());
                    case "And":
                    case "and":
                    case "&": return And(specifications.ToArray());
                    case "!": return Not(specifications.FirstOrDefault());
                    case "any":
                    case "Any": return Any(sarr[1], specifications.LastOrDefault());
                }
                return null;
            }
            else
                return createcompare(specification);
        }

        /// <summary>
        /// 创建成员变量
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Specification Member(string name)
        {
            return new MemberSpecification(name);
        }
        /// <summary>
        /// 创建输入变量
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Specification Variable(string name)
        {
            return new VariableSpecification(name);
        }
        /// <summary>
        /// 创建常量
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Specification Constant(object value)
        {
            return new ConstantSpecification(value);
        }
        /// <summary>
        /// 创建算数表达式
        /// </summary>
        /// <param name="expr"></param>
        /// <returns></returns>
        public static AtomSpecification Math(string expr)
        {
            if (!StringHelper.IsMath(expr))
                return null;
            return MathSpecification.CreateMath(expr);
        }
        /// <summary>
        /// 创建算数表达是
        /// </summary>
        /// <param name="left">左侧运算数</param>
        /// <param name="op">运算符 + - * / % ^(幂)</param>
        /// <param name="right">右侧运算数</param>
        /// <returns></returns>
        public static Specification Math(Specification left, char op, Specification right)
        {
            return new MathSpecification(left as AtomSpecification, op, right as AtomSpecification);
        }

        public static Specification Method(Specification left, MethodInfo method, Specification right)
        {
            return new MethodSpecification(left as AtomSpecification, method, right as AtomSpecification);
        }

        public static Specification Method(Specification left, string method, Specification right)
        {
            return new MethodSpecification(left as AtomSpecification, method, right as AtomSpecification);
        }

        /// <summary>
        /// 创建简单规格
        /// </summary>
        /// <param name="name">字段名称</param>
        /// <param name="code">比较器</param>
        /// <param name="value">值</param>
        /// <returns></returns>
        public static Specification Create(string name, CmpCode code, object value)
        {
            return new CompareSpecification(AtomSpecification.Atom(name), code, new ConstantSpecification(value));
        }
        /// <summary>
        /// 比较规约
        /// </summary>
        /// <param name="left"></param>
        /// <param name="code"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static Specification Compare(string left, CmpCode code, string right)
        {
            return new CompareSpecification(AtomSpecification.Atom(left), code, AtomSpecification.Atom(right));
        }

        /// <summary>
        /// 比较规约
        /// </summary>
        /// <param name="left">左侧原子规约</param>
        /// <param name="code">比较运算符</param>
        /// <param name="right">右侧原子规约</param>
        /// <returns></returns>
        public static Specification Compare(Specification left, CmpCode code, Specification right)
        {
            return new CompareSpecification(left as AtomSpecification, code, right as AtomSpecification);
        }
        /// <summary>
        /// 创建 或 规格
        /// </summary>
        /// <param name="specification">规格</param>
        /// <returns></returns>
        public static Specification Or(params Specification[] specifications)
        {
            var specs = specifications.Where(s => s != null).ToArray();
            if (specs.Length == 0)
                return null;
            if (specs.Length == 1)
                return specs[0];
            return new OrSpecification(specs);
        }
        /// <summary>
        /// 创建 与 规格
        /// </summary>
        /// <param name="specifications"></param>
        /// <returns></returns>
        public static Specification And(params Specification[] specifications)
        {
            var specs = specifications.Where(s => s != null).ToArray();
            if (specs.Length == 0)
                return null;
            if (specs.Length == 1)
                return specs[0];
            return new AndSpecification(specs);
        }
        /// <summary>
        /// 创建 非 规格
        /// </summary>
        /// <param name="specification"></param>
        /// <returns></returns>
        public static Specification Not(Specification specification)
        {
            return new NotSpecification(specification);
        }
        /// <summary>
        /// 创建子查询规格
        /// </summary>
        /// <param name="name"></param>
        /// <param name="specification"></param>
        /// <returns></returns>
        public static Specification Any(string name, Specification specification)
        {
            return new AnySpecification(name, specification);
        }
        #endregion
        /// <summary>
        /// 对象是否符合规约的条件
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        public abstract bool IsValid(Object2 dto);

        /// <summary>
        /// 设置或获取成员包装器
        /// </summary>
        public Func<string, string> WrapperMember { get; set; }

        /// <summary>
        /// 父规约
        /// </summary>
        public Specification Parent { get; set; }

        /// <summary>
        /// 根规约
        /// </summary>
        public Specification Root
        {
            get
            {
                return Parent == null ? this : Parent.Root;
            }
        }
        /// <summary>
        /// 查找表达式
        /// </summary>
        /// <typeparam name="T">表达式类型</typeparam>
        /// <returns></returns>
        public virtual IEnumerable<T> Find<T>()
            where T : Specification
        {
            if (this is T)
                return new List<T> { (T)this };
            return new List<T>();
        }

        #region 参数与上下文
        List<string> parameters;
        /// <summary>
        /// 额外参数列表
        /// </summary>
        public List<string> Parameters
        {
            get
            {
                if (parameters == null)
                    parameters = new List<string>();
                return parameters;
            }
        }

        List<string> m_members;

        /// <summary>
        /// 作用的成员
        /// </summary>
        public List<string> Members
        {
            get
            {
                if (m_members == null)
                    m_members = new List<string>();
                return m_members;
            }
        }

        TransferObject m_Context;
        public TransferObject Context
        {
            get
            {
                if (m_Context == null)
                    m_Context = new TransferObject();
                return m_Context;
            }
        }
        #endregion

        #region 表达式树
        /// <summary>
        /// 转为 表达式树
        /// </summary>
        /// <param name="metadata">元数据</param>
        /// <param name="main">当前元数据对应的参数</param>
        /// <param name="parameters">参数列表按顺序决定层级 0为当前 1为上一级 2为上上级</param>
        /// <returns></returns>
        internal abstract Expression ToExpression(ModelMetadata metadata, ParameterExpression main, Expression[] expressions);

        public LambdaExpression GetExpression(ModelMetadata metadata, ParameterExpression main, Expression[] expressions)
        {
            if (main == null)
                main = Expression.Parameter(metadata.ModelType);

            expressions = expressions.Where(s => s != null).ToArray();
            var exp = ToExpression(metadata, main, expressions);
            if (exp == null)
                return null;
            return Expression.Lambda(exp, main);
        }

        public LambdaExpression GetExpression(ModelMetadata metadata, params Expression[] expressions)
        {
            return GetExpression(metadata, Expression.Parameter(metadata.ModelType), expressions);
        }
        public LambdaExpression GetExpression(Type modeltype)
        {
            return GetExpression(ModelMetadataEngine.GetModel(modeltype.MetaName()));
        }
        public Expression<Func<TRoot, bool>> getExpression<TRoot>(ModelMetadata metadata = null)
            where TRoot : AggRoot
        {
            if (metadata == null)
                return (Expression<Func<TRoot, bool>>)GetExpression(typeof(TRoot));
            return (Expression<Func<TRoot, bool>>)GetExpression(metadata);
        }

        #endregion
    }

    /// <summary>
    /// 二元表达式
    /// </summary>
    public abstract class BinarySpecification : Specification
    {
        /// <summary>
        /// 左侧表达式
        /// </summary>
        public AtomSpecification Left { get; private set; }

        /// <summary>
        /// 右侧表达式
        /// </summary>
        public AtomSpecification Right { get; private set; }

        public BinarySpecification(AtomSpecification left, AtomSpecification right)
        {
            Left = left;
            Left.Parent = this;
            Right = right;
            Right.Parent = this;
            Members.AddRange(left.Members);
            Members.AddRange(right.Members);
            Parameters.AddRange(Left.Parameters);
            Parameters.AddRange(Right.Parameters);
        }

        public override IEnumerable<T> Find<T>()
        {
            var results = new List<T>(base.Find<T>());
            results.AddRange(Left.Find<T>());
            results.AddRange(Right.Find<T>());
            return results;
        }
    }

    /// <summary>
    /// 原子规约 表达式中的一方
    /// </summary>
    public abstract class AtomSpecification : Specification
    {
        public override bool IsValid(Object2 dto)
        {
            return true;
        }

        /// <summary>
        /// 表达式类型
        /// </summary>
        public Type Type { get; set; }

        /// <summary>
        /// 是否有成员变量
        /// </summary>
        /// <returns></returns>
        public virtual bool HasMember() { return false; }

        /// <summary>
        /// 获取值
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public abstract object GetValue(Object2 obj = null);
        /// <summary>
        /// 创建原子规约
        /// </summary>
        /// <param name="expression">表达式</param>
        /// <returns></returns>
        public static AtomSpecification Atom(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
                return null;
            expression = expression.Trim();
            object value;
            if (TryParseValue(expression, out value))
                return new ConstantSpecification(value);
            else if (expression.Contains("_*_"))
                return MethodSpecification.CreateMethod(expression);
            else if (StringHelper.IsMath(expression))
                return MathSpecification.CreateMath(expression);
            else if (expression.StartsWith("@"))
                return new VariableSpecification(expression.Substring(1));
            else
                return new MemberSpecification(expression);
        }

        public virtual string ToString(string modelalias)
        {
            return ToString();
        }
    }
    /// <summary>
    /// 成员变量规约
    /// </summary>
    public class MemberSpecification : AtomSpecification
    {
        /// <summary>
        /// 成员名称
        /// </summary>
        public string Name { get; private set; }
        /// <summary>
        /// 对象层级
        /// </summary>
        protected int Level { get; private set; }

        /// <summary>
        /// 字段源数据
        /// </summary>
        public FieldMetadata Field { get; private set; }

        static Regex Regex = new Regex("^p([0-9]?)$");
        /// <summary>
        /// 构造成员变量
        /// </summary>
        /// <param name="name">成员名称，前缀 p,p0表示上级对象 p1  表示上上级 依次类推</param>
        public MemberSpecification(string name)
        {
            Name = name;
            var tmp = name.Split(new char[] { '.' }, 2);
            Level = 0;
            if (tmp.Length == 2)
            {
                var match = Regex.Match(tmp[0]);
                if (match.Success)
                {
                    Name = tmp[1];
                    Level = 1;
                    var level = match.Groups[1].Value;
                    if (!string.IsNullOrEmpty(level))
                        Level = int.Parse(level) + 1;
                }
            }
            if (Level == 0)
                Members.Add(Name);
        }

        public override bool HasMember()
        {
            return true;
        }

        public override string ToString()
        {
            if (Level == 0)
                return Root.WrapperMember == null ? Name : Root.WrapperMember(Name);
            return string.Format("p{0}.{1}", Level - 1, Name);
        }
        public override string ToString(string modelalias)
        {
            if (string.IsNullOrEmpty(modelalias))
                return ToString();
            return string.Format("{0}.{1}", modelalias, Name);
        }
        object getValue(Object2 dto, Queue<string> names)
        {
            var name = names.Dequeue();
            if (names.Count == 0)
                return dto[name];

            Object2 obj = dto[name] as Object2;
            return getValue(obj, names);
        }

        public override object GetValue(Object2 obj = null)
        {
            var value = getValue(obj, new Queue<string>(Name.Split('.')));
            return value is object[]? (value as object[])[0] : value;
        }

        protected Expression getMember(ModelMetadata metadata, Expression instance, string name)
        {
            Field = null;
            if (string.IsNullOrEmpty(name))
                return null;
            string field = name.Split('.')[0];
            string endfield = name.Length > field.Length + 1 ? name.Substring(field.Length + 1) : "";
            Field = metadata.GetField(field);
            if (Field != null)
            {
                if (Field.PropertyInfo == null)
                {
                    if (Field.Field_Type == FieldType.many2many || Field.Field_Type == FieldType.one2many)
                    {
                        NavigatField mfield = Field as NavigatField;
                        MethodInfo method = null;
                        if (mfield.IsEntityCollection)
                            method = ExprHelper.GetEntities(mfield.RelationType);
                        else
                            method = ExprHelper.GetReferences(mfield.RelationType);

                        return Expression.Call(instance, method, Expression.Constant(Field.Name));
                    }
                    else
                    {

                        if (Field.Field_Type == FieldType.many2one)
                        {
                            if (string.IsNullOrEmpty(endfield))
                                return Expression.Convert(Expression.Call(instance, metadata.GetItem, Expression.Constant(Field.GetFieldname())), ((NavigatField)Field).RelationFieldType);
                            var exp = Expression.Convert(Expression.Call(instance, metadata.GetItem, Expression.Constant(field)), Type ?? ((NavigatField)Field).RelationModelMeta.ModelType);
                            return getMember(((Many2OneField)Field).RelationModelMeta, exp, endfield);
                        }
                        else
                            return Expression.Convert(Expression.Call(instance, metadata.GetItem, Expression.Constant(field)), Type ?? Field.PropertyType);
                    }
                }
                else
                {
                    Expression exp = Expression.MakeMemberAccess(instance, Field.PropertyInfo);

                    if (Field.Field_Type == FieldType.many2one)
                    {
                        if (string.IsNullOrEmpty(endfield))
                            return Expression.MakeMemberAccess(instance, ((NavigatField)Field).PrimaryInfo);
                        return getMember(((Many2OneField)Field).RelationModelMeta, exp, endfield);
                    }
                    else
                    {
                        if (Type != null && Type != Field.PropertyInfo.PropertyType)
                            exp = Expression.Convert(exp, Type);
                        return exp;
                    }
                }
            }
            else
            {
                var property = metadata.ModelType.GetProperty(field, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (property != null)
                {
                    Expression exp = Expression.MakeMemberAccess(instance, property);

                    if (string.IsNullOrEmpty(endfield))
                        return exp;
                    return getMember(ModelMetadata.GetModel(exp.Type), exp, endfield);
                }
            }
            throw new Exception2("the model {0} not contain field {1}", metadata.Name, name);
            //return null;
        }

        internal override Expression ToExpression(ModelMetadata metadata, ParameterExpression main, params Expression[] expressions)
        {
            if (expressions.Length < Level)
                return null;
            Expression instance = main;
            if (Level > 0)
                instance = expressions[Level - 1];
            if (metadata.ModelType != instance.Type)
                metadata = ModelMetadataEngine.GetModel(instance.Type);
            return getMember(metadata, instance, Name);
        }
    }

    /// <summary>
    /// 值规约
    /// </summary>
    public abstract class ValueSpecification : AtomSpecification
    {

        static readonly Expression expnull = Expression.Constant(null);

        internal override Expression ToExpression(ModelMetadata metadata, ParameterExpression main, params Expression[] expressions)
        {
            var value = GetValue();
            if (Type == null)
            {
                if (value == null)
                    return expnull;
                else
                    Type = value.GetType();
            }
            var realtype = Nullable.GetUnderlyingType(Type) ?? Type;
            bool needconvert = realtype != Type || Type == typeof(object) || Type.IsEnum;
            bool isnullable = TypeHelper.IsNullable(Type);
            if (value == null)
            {
                if (isnullable)
                    return Expression.Convert(expnull, Type);
                return Expression.Constant(TypeHelper.Default(Type));
            }
            if (!(value is string) && value is IEnumerable)
            {
                if (Type.Name == "String" || Type.GetInterface("IEnumerable") == null)
                    Type = typeof(List<>).MakeGenericType(Type);

                //dynamic values = Activator.CreateInstance(typeof(List<>).MakeGenericType(Type));
                //foreach (object v in (dynamic)value)
                //{
                //    if (v == null)
                //    {
                //        if (isnullable)
                //            values.Add(null);
                //    }
                //    else
                //        values.Add((dynamic)Convert2.ChangeType(v, Type));
                //}
                //return Expression.Constant(values);
            }
            var result = Expression.Constant(Convert2.ChangeType(value, Type));
            if (needconvert)
                return Expression.Convert(result, Type);
            return result;
        }
    }
    /// <summary>
    /// 变量表达式
    /// </summary>
    public class VariableSpecification : ValueSpecification
    {
        /// <summary>
        /// 变量名称
        /// </summary>
        public string Name { get; private set; }

        public VariableSpecification(string name)
        {
            Name = name;//.Replace(".", "_");
            Parameters.Add(Name);
        }

        string m_tempkey;
        /// <summary>
        /// 临时值的线程缓存key
        /// </summary>
        private string TempKey
        {
            get
            {
                if (m_tempkey == null)
                    m_tempkey = "owl.domain.variablespecification." + Util.Serial.GetRandom(5, false).ToLower();
                return m_tempkey;
            }
        }
        /// <summary>
        /// 临时值
        /// </summary>
        public object TempValue
        {
            get
            {
                return Cache.Thread(TempKey);
            }
            set
            {
                Cache.Thread(TempKey, value);
            }
        }
        public override object GetValue(Object2 obj = null)
        {
            var value = TempValue ?? Feature.Variable.GetValue(Name);
            if (value is AggRoot)
                return (value as AggRoot)["Id"];
            return value;
        }
        public override string ToString()
        {
            return string.Format("@{0}", Name);
        }

        internal override Expression ToExpression(ModelMetadata metadata, ParameterExpression main, params Expression[] expressions)
        {
            if (TempValue != null || Feature.Variable.Contain(Name))
                return base.ToExpression(metadata, main, expressions);
            if (expressions.Length > 0)
            {
                var level = 0;
                var tarray = Name.Split('.');
                var tlist = tarray.ToList();
                for (var i = 0; i < tarray.Length; i++)
                {
                    if (tarray[i] != "TopObj")
                    {
                        break;
                    }
                    tlist.Remove(tarray[i]);
                    level = i + 1;
                }

                var exp = new MemberSpecification(string.Format("p{0}.{1}", level, string.Join(".", tlist))).ToExpression(metadata, main, expressions);
                if (exp != null)
                    return (this.Type != null && exp.Type != this.Type) ? Expression.Convert(exp, Type) : exp;
            }
            return base.ToExpression(metadata, main, expressions);
        }
    }
    /// <summary>
    /// 常量规约
    /// </summary>
    public class ConstantSpecification : ValueSpecification
    {
        /// <summary>
        /// 常量值
        /// </summary>
        public object Value { get; private set; }

        public override object GetValue(Object2 obj = null)
        {
            return Value;
        }
        /// <summary>
        /// 常量
        /// </summary>
        /// <param name="value">值</param>
        public ConstantSpecification(object value)
        {
            Value = value;
        }
        string tostring(object value)
        {
            if (value == null)
                return "null";
            if (value is string || value is DateTime || value is Guid || value is Enum)
                return string.Format("'{0}'", value);
            if (value is IEnumerable)
            {
                List<string> str = new List<string>();
                foreach (var v in (IEnumerable)value)
                {
                    str.Add(tostring(v));
                }
                return string.Format("[{0}]", string.Join(",", str));
            }
            return value.ToString();
        }

        public override string ToString()
        {
            return tostring(Value);
        }
    }

    public abstract class BinaryAtomSpecification : AtomSpecification
    {

        /// <summary>
        /// 左侧表达式
        /// </summary>
        public AtomSpecification Left { get; private set; }

        /// <summary>
        /// 右侧表达式
        /// </summary>
        public AtomSpecification Right { get; private set; }

        public BinaryAtomSpecification(AtomSpecification left, AtomSpecification right)
        {
            Left = left;
            Left.Parent = this;
            Right = right;
            Right.Parent = this;
            Members.AddRange(left.Members);
            Members.AddRange(right.Members);
            Parameters.AddRange(Left.Parameters);
            Parameters.AddRange(Right.Parameters);
        }

        public override IEnumerable<T> Find<T>()
        {
            var results = new List<T>(base.Find<T>());
            results.AddRange(Left.Find<T>());
            results.AddRange(Right.Find<T>());
            return results;
        }
        public override bool HasMember()
        {
            return Left.HasMember() || Right.HasMember();
        }
    }

    /// <summary>
    /// 算术表达式
    /// </summary>
    public class MathSpecification : BinaryAtomSpecification
    {
        #region 静态方法
        static readonly Dictionary<char, int> oplevels = new Dictionary<char, int>() {
            { '?', 1 }, //联合操作符
            { '+', 2 },
            { '-', 2 },
            { '*', 3 },
            { '/', 3 },
            { '%', 3 },
            { '^', 4 },
            { '(', 1000 },
            { ')', 1000 }
        };

        static List<int> getlower(string exp)
        {
            int lower = int.MaxValue;
            int tc = 0;
            bool str = false;
            Dictionary<int, List<int>> indexs = new Dictionary<int, List<int>>();
            indexs[lower] = new List<int>();
            for (int i = 0; i < exp.Length; i++)
            {
                var e = exp[i];

                switch (e)
                {
                    case '\'': str = !str; break;
                    case '(': tc += 1; break;
                    case ')': tc -= 1; break;
                    default:
                        if (oplevels.ContainsKey(e) && tc == 0 && !str)
                        {
                            var level = oplevels[e];
                            if (!indexs.ContainsKey(level))
                                indexs[level] = new List<int>();
                            indexs[level].Add(i);
                            if (level < lower)
                                lower = level;
                        }
                        break;
                }
            }
            return indexs[lower];
        }

        /// <summary>
        /// 根据表达式创建
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public static AtomSpecification CreateMath(string expression)
        {
            expression = expression.Trim();
            var lowers = getlower(expression);
            var oldexp = expression;
            if (lowers.Count == 0 && expression.StartsWith("(") && expression.EndsWith(")"))
            {
                expression = expression.Substring(1, expression.Length - 2);
                lowers = getlower(expression);
            }
            if (lowers.Count == 0)
            {
                if (oldexp == expression)
                    throw new AlertException("owl.domian.expression2.math.novalid", "表达式 {0} 不是有效的算术表达式", expression);
                return Atom(expression);
            }

            lowers.Add(expression.Length);
            AtomSpecification result = null;
            int start = 0;
            foreach (var lower in lowers)
            {
                var tmp = Atom(expression.Substring(start, lower - start));
                if (result == null)
                    result = tmp;
                else
                {
                    result = new MathSpecification(result, expression[start - 1], tmp);
                }
                start = lower + 1;
            }
            return result;
        }
        #endregion

        /// <summary>
        /// 运算符
        /// </summary>
        public char Op { get; private set; }
        private int Level
        {
            get { return oplevels[Op]; }
        }
        public MathSpecification(AtomSpecification left, char op, AtomSpecification right)
            : base(left, right)
        {
            Op = op;
        }

        public override string ToString(string modelalias)
        {
            string left = Left.ToString(modelalias);// Left is MemberSpecification? : Left.ToString();
            var mleft = Left as MathSpecification;
            if (mleft != null && mleft.Level < Level)
                left = "(" + left + ")";

            string right = Right.ToString(modelalias);
            var mright = Right as MathSpecification;
            if (mright != null && (mright.Level < Level || (mright.Level == Level && mright.Op != Op)))
                right = "(" + right + ")";

            return string.Format("{0}{1}{2}", left, Op, right);
        }

        public override string ToString()
        {
            string left = Left.ToString();
            var mleft = Left as MathSpecification;
            if (mleft != null && mleft.Level < Level)
                left = "(" + left + ")";

            string right = Right.ToString();
            var mright = Right as MathSpecification;
            if (mright != null && (mright.Level < Level || (mright.Level == Level && mright.Op != Op)))
                right = "(" + right + ")";

            return string.Format("{0}{1}{2}", left, Op, right);
        }
        public override object GetValue(Object2 obj = null)
        {
            dynamic lvalue = Left.GetValue(obj);
            dynamic rvalue = Right.GetValue(obj);
            if (lvalue == null && rvalue == null)
                return null;
            if (lvalue == null && rvalue != null)
                lvalue = TypeHelper.Default(rvalue.GetType());
            else if (lvalue != null && rvalue == null)
                rvalue = TypeHelper.Default(lvalue.GetType());

            switch (Op)
            {
                case '?': return lvalue ?? rvalue;
                case '+': return lvalue + rvalue;
                case '-': return lvalue - rvalue;
                case '*': return lvalue * rvalue;
                case '/': return lvalue / rvalue;
                case '%': return lvalue % rvalue;
                case '^': return System.Math.Pow(lvalue, rvalue);
            }
            return null;
        }

        MethodInfo GetAddMethod(Expression left, Expression right)
        {
            MethodInfo method = null;
            if (left.Type.Name == "String")
                method = ExprHelper.Str_Add;
            if (left.Type.Name == "DateTime")
                method = ExprHelper.DT_Add;
            return method;
        }
        MethodInfo GetSubtractMethod(Expression left, Expression right)
        {
            MethodInfo method = null;
            if (left.Type.Name == "DateTime")
                method = ExprHelper.DT_Subtract;
            return method;
        }
        internal override Expression ToExpression(ModelMetadata metadata, ParameterExpression main, params Expression[] expressions)
        {
            Left.Type = null;
            Right.Type = null;
            if (Type != null)
            {
                var type = TypeHelper.StripType(Type);
                Left.Type = type;
                Right.Type = type;
            }
            Expression left, right;
            if (Right.HasMember() && !Left.HasMember())
            {
                right = Right.ToExpression(metadata, main, expressions);
                Left.Type = right.Type;
                left = Left.ToExpression(metadata, main, expressions);
            }
            else
            {
                left = Left.ToExpression(metadata, main, expressions);
                Right.Type = left.Type;
                if (left.Type.Name == "DateTime")
                {
                    if (Op == '+')
                    {
                        Right.Type = ExprHelper.DT_Add.GetParameters()[1].ParameterType;
                    }
                    else if (Op == '-')
                    {
                        Right.Type = ExprHelper.DT_Subtract.GetParameters()[1].ParameterType;
                    }
                }
                right = Right.ToExpression(metadata, main, expressions);
            }
            if (left == null || right == null)
                return null;
            Expression exp = null;
            switch (Op)
            {
                case '?': exp = Expression.Coalesce(left, right); break;
                case '+': exp = Expression.Add(left, right, GetAddMethod(left, right)); break;
                case '-': exp = Expression.Subtract(left, right, GetSubtractMethod(left, right)); break;
                case '*': exp = Expression.Multiply(left, right); break;
                case '/': exp = Expression.Divide(left, right); break;
                case '%': exp = Expression.Modulo(left, right); break;
                case '^': exp = Expression.Power(left, right); break;
            }

            return exp == null ? null : (Type != null && Type != exp.Type) ? Expression.Convert(exp, Type) : exp;
        }
    }

    /// <summary>
    /// 方法调用
    /// </summary>
    public class MethodSpecification : BinaryAtomSpecification
    {
        public string MethodName { get; private set; }

        public MethodInfo Info { get; private set; }

        public override object GetValue(Object2 obj = null)
        {
            throw new NotImplementedException();
        }
        public static AtomSpecification CreateMethod(string expression)
        {
            var specs = expression.Split(new char[] { '_', '*', '_' }, StringSplitOptions.RemoveEmptyEntries);
            return new MethodSpecification(AtomSpecification.Atom(specs[0]), specs[1], AtomSpecification.Atom(specs[2]));
        }

        static string toString(MethodInfo info)
        {
            List<string> names = new List<string>();
            names.Add(info.DeclaringType.FullName + "," + info.DeclaringType.Assembly.GetName().Name);
            names.Add(info.Name);
            names.AddRange(info.GetParameters().Select(s => s.ParameterType.FullName + "," + s.ParameterType.Assembly.GetName().Name));
            return string.Join("|", names);
        }

        static MethodInfo fromStr(string method)
        {
            var names = method.Split('|');
            List<Type> types = new List<Type>();
            for (var i = 2; i < names.Length; i++)
            {
                types.Add(Type.GetType(names[i]));
            }
            return Type.GetType(names[0]).GetMethod(names[1], types.ToArray());
        }

        public MethodSpecification(AtomSpecification left, string method, AtomSpecification right)
            : this(left, fromStr(method), right)
        {

        }

        public MethodSpecification(AtomSpecification left, MethodInfo method, AtomSpecification right)
            : base(left, right)
        {
            Info = method;
            MethodName = toString(Info);
        }

        public override string ToString()
        {
            return string.Format("{0}_*_{1}_*_{2}", Left.ToString(), MethodName, Right.ToString());
        }
        internal override Expression ToExpression(ModelMetadata metadata, ParameterExpression main, Expression[] expressions)
        {
            var left = Left.ToExpression(metadata, main, expressions);
            var right = Right.ToExpression(metadata, main, expressions);
            if (Info.IsStatic)
                return Expression.Call(null, Info, left, right);
            return Expression.Call(left, Info, right);
        }
    }

    /// <summary>
    /// 比较表达式
    /// </summary>
    public class CompareSpecification : BinarySpecification
    {
        /// <summary>
        /// 比较运算符
        /// </summary>
        public CmpCode Code { get; private set; }

        public CompareSpecification(AtomSpecification left, CmpCode code, AtomSpecification right)
            : base(left, right)
        {
            bool leftismem = left is MemberSpecification;
            bool rightismem = right is MemberSpecification;
            if (code == CmpCode.IN && !left.HasMember())
                throw new Exception2("包含表达式的左侧必须为 成员变量 ！");
            Code = code;
            if (code == CmpCode.EQ && ((leftismem && !rightismem) || (!leftismem && rightismem)))
            {
                MemberSpecification member = leftismem ? left as MemberSpecification : right as MemberSpecification;
                ValueSpecification value = leftismem ? right as ValueSpecification : left as ValueSpecification;
                if (value is VariableSpecification)
                    Context[member.Name] = "@" + (value as VariableSpecification).Name;
                else
                    Context[member.Name] = (value as ConstantSpecification).Value;
            }
        }

        #region 有效性验证


        public override bool IsValid(Object2 dto)
        {
            var lvalue = Left.GetValue(dto);
            var rvalue = Right.GetValue(dto);
            switch (Code)
            {
                case CmpCode.EQ: return ObjectExt.Compare(lvalue, rvalue) == 0;
                case CmpCode.GT: return ObjectExt.Compare(lvalue, rvalue) > 0;
                case CmpCode.GTE: return ObjectExt.Compare(lvalue, rvalue) >= 0;
                case CmpCode.LT: return ObjectExt.Compare(lvalue, rvalue) < 0;
                case CmpCode.LTE: return ObjectExt.Compare(lvalue, rvalue) <= 0;
                case CmpCode.NE: return ObjectExt.Compare(lvalue, rvalue) != 0;
                case CmpCode.IN:
                    if (lvalue == null)
                        return false;
                    List<object> tmp = new List<object>();
                    var type1 = lvalue.GetType();
                    foreach (var v in (IEnumerable)rvalue)
                    {
                        if (v != null)
                            tmp.Add(Convert2.ChangeType(v, type1));
                    }
                    return ObjectExt.In(lvalue, tmp);
                case CmpCode.Start: return (lvalue is string) ? (lvalue as string).StartsWith(rvalue as string) : false;
                case CmpCode.End: return (lvalue is string) ? (lvalue as string).EndsWith(rvalue as string) : false;
                case CmpCode.Con:
                    if (lvalue is string)
                        return (lvalue as string).Contains((string)rvalue);
                    return ObjectExt.In(rvalue, lvalue);
                case CmpCode.Conm:
                    return ObjectExt.ArrayContains(lvalue as string, rvalue as string);
            }
            return false;
        }
        #endregion

        public override string ToString()
        {
            return string.Format("({0},{1},{2})", Left.ToString(), Code.toString(), Right.ToString());
        }

        internal override Expression ToExpression(ModelMetadata metadata, ParameterExpression main, params Expression[] expressions)
        {
            Left.Type = null;
            Right.Type = null;
            Expression left = null;
            Expression right = null;
            if (Right.HasMember() && !Left.HasMember())
            {
                right = Right.ToExpression(metadata, main, expressions);
                if (right != null)
                {
                    Left.Type = right.Type;
                    left = Left.ToExpression(metadata, main, expressions);
                }
            }
            else
            {
                left = Left.ToExpression(metadata, main, expressions);
                if (left != null)
                {
                    Right.Type = left.Type;
                    right = Right.ToExpression(metadata, main, expressions);
                }
            }
            bool needfitempty = false;
            if (Right is VariableSpecification)
            {
                if (object.Equals(Right.GetValue(), ""))
                {
                    needfitempty = true;
                }
            }
            if ((Left is VariableSpecification && left == null) ||
                (Right is VariableSpecification && right == null))
                return Expression.Constant(true);

            if (Left is ValueSpecification && Right is ValueSpecification)
            {
                return Expression.Constant(IsValid(null));
            }

            if (left == null || right == null)
                return null;

            var propertytype = left.Type;
            var isnullable = Left is MemberSpecification && !(Left as MemberSpecification).Field.Required;
            Expression notnull = null;
            if (isnullable || left.Type.Name == "String")
                notnull = Expression.NotEqual(left, Expression.Constant(null));
            switch (Code)
            {
                case CmpCode.EQ:
                    return needfitempty ? Expression.OrElse(Expression.Equal(left, right), Expression.Equal(left, Expression.Constant(null))) : Expression.Equal(left, right);
                case CmpCode.GT:
                    if (propertytype.Name == "String")
                        return Expression.Call(null, ExprHelper.Str_GT, left, right);
                    return Expression.GreaterThan(left, right);
                case CmpCode.GTE:
                    if (propertytype.Name == "String")
                        return Expression.Call(null, ExprHelper.Str_GTE, left, right);
                    return Expression.GreaterThanOrEqual(left, right);
                case CmpCode.LT:
                    if (propertytype.Name == "String")
                        return Expression.Call(null, ExprHelper.Str_LT, left, right);
                    return Expression.LessThan(left, right);
                case CmpCode.LTE:
                    if (propertytype.Name == "String")
                        return Expression.Call(null, ExprHelper.Str_LTE, left, right);
                    return Expression.LessThanOrEqual(left, right);
                case CmpCode.NE:
                    return Expression.NotEqual(left, right);
                case CmpCode.IN:
                    return Expression.Call(null, Util.ExprHelper.GetIn(left.Type), left, right);
                case CmpCode.Start:
                    return Expression.AndAlso(notnull, Expression.Call(left, ExprHelper.Str_Start, right));
                case CmpCode.End:
                    return Expression.AndAlso(notnull, Expression.Call(left, ExprHelper.Str_End, right));
                case CmpCode.Con:
                    if (left.NodeType == ExpressionType.Constant)
                        return Expression.Call(left, ExprHelper.Str_Contains, right);
                    return Expression.AndAlso(notnull, Expression.Call(left, ExprHelper.Str_Contains, right));
                case CmpCode.Conm:
                    return Expression.AndAlso(notnull, Expression.Call(null, ExprHelper.Array_Contains, left, right));
                default: return null;
            }
        }

    }
    /// <summary>
    /// 多元表达式
    /// </summary>
    public abstract class MultipleSpecification : Specification
    {
        /// <summary>
        /// 规约集合
        /// </summary>
        public IEnumerable<Specification> Specifications { get; private set; }

        public MultipleSpecification(Specification[] specifications, bool ignoremember = false)
        {
            Specifications = specifications.Where(s => s != null);
            foreach (var spec in Specifications)
            {
                spec.Parent = this;
                Parameters.AddRange(spec.Parameters);
                if (!ignoremember)
                    Members.AddRange(spec.Members);
                foreach (var key in spec.Context.Keys)
                    Context[key] = spec.Context[key];
            }
        }
        public override IEnumerable<T> Find<T>()
        {
            var result = new List<T>();
            result.AddRange(base.Find<T>());
            foreach (var spec in Specifications)
                result.AddRange(spec.Find<T>());
            return result;
        }
        public override string ToString()
        {
            return string.Join(",", Specifications.Select(s => s.ToString()));
        }
    }
    /// <summary>
    /// 或者规约
    /// </summary>
    public class OrSpecification : MultipleSpecification
    {
        public OrSpecification(params Specification[] specifications)
            : base(specifications)
        {
        }
        public override string ToString()
        {
            return string.Format("[|,{0}]", base.ToString());
        }

        internal override Expression ToExpression(ModelMetadata metadata, ParameterExpression main, params Expression[] expressions)
        {
            Expression exp = null;
            foreach (var spec in Specifications)
            {
                var tmp = spec.ToExpression(metadata, main, expressions);
                if (tmp == null)
                    continue;
                if (tmp is ConstantExpression)
                {
                    var ctmp = tmp as ConstantExpression;
                    if (ctmp.Value is bool)
                    {
                        if ((bool)ctmp.Value == false)
                            continue;
                        else
                            return tmp;
                    }
                }
                if (exp == null)
                    exp = tmp;
                else
                    exp = Expression.OrElse(exp, tmp);
            }
            return exp;
        }
        public override bool IsValid(Object2 dto)
        {
            foreach (var spec in Specifications)
            {
                if (spec.IsValid(dto))
                    return true;
            }
            return false;
        }
    }
    /// <summary>
    /// 并且规约
    /// </summary>
    public class AndSpecification : MultipleSpecification
    {
        public AndSpecification(params Specification[] specifications)
            : base(specifications)
        {

        }

        public override string ToString()
        {
            return string.Format("[&,{0}]", base.ToString());
        }
        public override bool IsValid(Object2 dto)
        {
            foreach (var spec in Specifications)
            {
                if (!spec.IsValid(dto))
                    return false;
            }
            return true;
        }
        internal override Expression ToExpression(ModelMetadata metadata, ParameterExpression main, params Expression[] expressions)
        {
            Expression exp = null;
            foreach (var spec in Specifications)
            {
                var tmp = spec.ToExpression(metadata, main, expressions);
                if (tmp == null)
                    continue;
                if (tmp is ConstantExpression)
                {
                    var ctmp = tmp as ConstantExpression;
                    if (ctmp.Value is bool)
                    {
                        if ((bool)ctmp.Value == false)
                            return tmp;
                        else
                            continue;
                    }
                }
                if (exp == null)
                    exp = tmp;
                else
                    exp = Expression.AndAlso(exp, tmp);
            }
            return exp;
        }
    }
    /// <summary>
    /// 非 规约
    /// </summary>
    public class NotSpecification : MultipleSpecification
    {
        public NotSpecification(Specification specification)
            : base(new Specification[] { specification })
        {

        }
        public override bool IsValid(Object2 dto)
        {
            return !Specifications.FirstOrDefault().IsValid(dto);
        }
        public override string ToString()
        {
            return string.Format("[!,{0}]", base.ToString());
        }
        internal override Expression ToExpression(ModelMetadata metadata, ParameterExpression main, params Expression[] expressions)
        {
            var exp = Specifications.FirstOrDefault().GetExpression(metadata, main, expressions);
            if (exp == null)
                return null;
            return Expression.Not(exp.Body);
        }
    }
    /// <summary>
    /// 子查询
    /// </summary>
    public class AnySpecification : MultipleSpecification
    {
        /// <summary>
        /// 字段名称
        /// </summary>
        public string Name { get; private set; }

        public MemberSpecification Left { get; private set; }

        public AnySpecification(string name, Specification specification)
            : base(new Specification[] { specification }, true)
        {
            Left = new MemberSpecification(name);
            Members.Add(name.Split('.')[0]);
            Name = name;
        }
        public override IEnumerable<T> Find<T>()
        {
            var result = new List<T>();
            result.AddRange(base.Find<T>());
            result.AddRange(Left.Find<T>());
            return result;
        }
        public override string ToString()
        {
            return string.Format("[Any,{0},{1}]", Name, base.ToString());
        }

        internal override Expression ToExpression(ModelMetadata metadata, ParameterExpression main, params Expression[] expressions)
        {
            var member = Left.ToExpression(metadata, main, expressions);
            if (member == null || (Left.Field.Field_Type != FieldType.one2many && Left.Field.Field_Type != FieldType.many2many))
                return null;
            var navfield = Left.Field as NavigatField;
            var navmeta = navfield.RelationModelMeta;
            var p = new List<Expression>() { main };
            p.AddRange(expressions);
            var tparam = Expression.Parameter(navmeta.ModelType);
            var anyexp = Specifications.FirstOrDefault().ToExpression(navmeta, tparam, p.ToArray());
            return Expression.Call(null, ExprHelper.GetAnyMethod(navmeta.ModelType), member, Expression.Lambda(anyexp, tparam));
        }
        public override bool IsValid(Object2 dto)
        {
            dynamic children = dto.GetDepthValue(Name);
            var spec = Specifications.FirstOrDefault();
            foreach (Object2 child in children)
            {
                if (spec.IsValid(child))
                    return true;
            }
            return false;
        }
    }
}
