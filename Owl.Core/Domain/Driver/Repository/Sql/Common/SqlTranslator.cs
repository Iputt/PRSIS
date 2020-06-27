using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Collections;
using Owl.Util;
namespace Owl.Domain.Driver.Repository.Sql
{
    /// <summary>
    /// 格式转换器
    /// </summary>
    public class TranslatorFormat
    {
        /// <summary>
        /// 序号
        /// </summary>
        public int Index { get; private set; }
        /// <summary>
        /// 参数名称
        /// </summary>
        public string Parameter { get; private set; }

        public Func<TranslatorParameter, string> Alternate { get; private set; }

        public Func<TranslatorParameter, bool> Ignores { get; private set; }

        public TranslatorFormat(int index, string parameter, Func<TranslatorParameter, string> alternate, Func<TranslatorParameter, bool> ignore)
        {
            Index = index;
            Parameter = parameter;
            Alternate = alternate;
            Ignores = ignore;
        }
    }
    public class TranslatorFormatCollection : IEnumerable<TranslatorFormat>
    {
        List<TranslatorFormat> formats = new List<TranslatorFormat>();

        public TranslatorFormat Create(string parameter, Func<TranslatorParameter, string> alternate, Func<TranslatorParameter, bool> ignore)
        {
            var format = new TranslatorFormat(formats.Count, parameter, alternate, ignore);
            formats.Add(format);
            return format;
        }

        public IEnumerator<TranslatorFormat> GetEnumerator()
        {
            return formats.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class CommandFormat
    {
        TranslatorFormatCollection m_formats;
        int index;
        string m_text;
        List<string> m_tables;
        internal bool Distinct;
        public CommandFormat()
        {
            m_formats = new TranslatorFormatCollection();
            index = 0;
            m_text = "";
            m_tables = new List<string>();
        }
        /// <summary>
        /// 创建参数名称
        /// </summary>
        /// <returns></returns>
        public string CreateName()
        {
            index += 1;
            return string.Format("p{0}", index - 1);
        }
        /// <summary>
        /// 创建格式化器
        /// </summary>
        /// <param name="parameter">参数名称</param>
        /// <param name="alternate">格式化器</param>
        /// <param name="ignoreparam">是否跳过参数</param>
        /// <returns></returns>
        public TranslatorFormat CretaeFormater(string parameter, Func<TranslatorParameter, string> alternate, Func<TranslatorParameter, bool> ignoreparam)
        {
            return m_formats.Create(parameter, alternate, ignoreparam);
        }
        /// <summary>
        /// 构建格式化字符串
        /// </summary>
        /// <param name="format"></param>
        public void BuildFormat(string format, IEnumerable<string> tables)
        {
            m_text = format;
            m_tables.AddRange(tables);
        }

        public QueryCommand Create(TranslatorParameterCollection parameters)
        {
            string text = m_text;
            List<string> args = new List<string>();
            List<TranslatorParameter> tps = new List<TranslatorParameter>(parameters);
            foreach (var format in m_formats)
            {
                var param = parameters.Get(format.Parameter);
                if (param == null)
                    throw new Exception2("格式器与参数不匹配");
                args.Add(format.Alternate(param));
                if (format.Ignores(param))
                    tps.Remove(param);
            }
            if (args.Count > 0)
                text = string.Format(m_text, args.ToArray());

            var cmd = new QueryCommand(text, new TranslatorParameterCollection(tps, parameters.Index), m_tables);
            cmd.Distinct = Distinct;
            return cmd;
        }
    }

    public class QueryCommand
    {
        TranslatorParameterCollection m_params;
        StringBuilder builder;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="text"></param>
        public void BuildText(string text)
        {
            builder.Clear();
            builder.Append(text);
        }
        public void AppendText(string text)
        {
            builder.Append(text);
        }
        /// <summary>
        /// 命令字符串
        /// </summary>
        public string CommandText { get { return builder.ToString(); } }

        /// <summary>
        /// 命令的长度
        /// </summary>
        public int CommandLength { get { return builder.Length; } }

        /// <summary>
        /// 本命令引用的表
        /// </summary>
        public List<string> Tables { get; private set; }

        public bool Distinct { get; set; }

        public string StrTables
        {
            get
            {
                //return string.Join(" ", Tables.Distinct());
                if (RepositoryRunning.NoLock)
                {
                    return string.Join(" ", Tables.Select(s => SqlUnit.WrapNolock(s)).Distinct());
                }
                else
                {
                    return string.Join(" ", Tables.Distinct());
                }
            }
        }

        public QueryCommand(string text = "", TranslatorParameterCollection parameters = null, IEnumerable<string> tables = null)
        {
            builder = new StringBuilder(text);
            if (parameters == null)
                parameters = new TranslatorParameterCollection();
            m_params = parameters;
            Tables = new List<string>();
            if (tables != null)
                Tables.AddRange(tables);
        }
        /// <summary>
        /// 创建参数
        /// </summary>
        /// <param name="value">值</param>
        /// <param name="alias">别名</param>
        /// <returns></returns>
        public TranslatorParameter CreateParameter(object value, string alias)
        {
            return m_params.Create(value, alias);
        }
        public IEnumerable<TranslatorParameter> Parameters
        {
            get
            {
                List<TranslatorParameter> parameters = new List<TranslatorParameter>();
                foreach (var param in m_params)
                {
                    if (param.Children == null)
                        parameters.Add(param);
                    else
                        foreach (var child in param.Children)
                            parameters.Add(child);
                }
                return parameters;
            }
        }
        public int HashCode { get; set; }
        /// <summary>
        /// 是否包含有参数
        /// </summary>
        public bool HasParameter
        {
            get { return m_params.Count > 0; }
        }
    }

    /// <summary>
    /// 参数，元数，别名
    /// </summary>
    public class PMAlias
    {
        /// <summary>
        /// 参数
        /// </summary>
        public ParameterExpression Parameter { get; private set; }

        /// <summary>
        /// 元数据
        /// </summary>
        public ModelMetadata Metadata { get; private set; }

        /// <summary>
        /// 别名
        /// </summary>
        public string Alias { get; private set; }

        public PMAlias(ParameterExpression param, ModelMetadata metadata, string alias = "it")
        {
            if (param == null)
                throw new ArgumentNullException("param");
            Parameter = param;
            Metadata = metadata ?? ModelMetadataEngine.GetModel(param.Type);
            Alias = string.IsNullOrEmpty(alias) ? "it" : alias;
        }

        public PMAlias(ParameterExpression param, string alias)
            : this(param, null, alias)
        {

        }

        public PMAlias(ParameterExpression param)
            : this(param, null, "it")
        {

        }
    }

    public abstract class SqlTranslator : ExpressionVisitor
    {
        static readonly Dictionary<CacheKey, CommandFormat> commands = new Dictionary<CacheKey, CommandFormat>();
        static readonly Dictionary<CacheKey, object> keylocker = new Dictionary<CacheKey, object>();
        #region 变量设置
        Dictionary<string, object> m_param;
        Dictionary<string, object> m_Param
        {
            get
            {
                if (m_param == null)
                    m_param = new Dictionary<string, object>(10);
                return m_param;
            }
            set { m_param = value; }
        }
        T getValue<T>(string key)
        {
            if (m_Param.ContainsKey(key))
                return (T)m_Param[key];
            return default(T);
        }
        void setValue(string key, object value)
        {
            m_Param[key] = value;
        }
        #endregion

        #region
        StringBuilder builder
        {
            get { return getValue<StringBuilder>("builder"); }
            set { setValue("builder", value); }
        }
        /// <summary>
        /// 返回的command
        /// </summary>
        protected CommandFormat Formater
        {
            get { return getValue<CommandFormat>("cmd"); }
            private set { setValue("cmd", value); }
        }
        protected string Alias
        {
            get { return getValue<string>("alias"); }
            set { setValue("alias", value); }
        }
        protected ModelMetadata MetaData
        {
            get { return getValue<ModelMetadata>("MetaData"); }
            set { setValue("MetaData", value); }
        }
        protected List<string> Tables
        {
            get { return getValue<List<string>>("Tables"); }
            set { setValue("Tables", value); }
        }
        protected void AddTables(IEnumerable<string> tables)
        {
            foreach (var table in tables)
            {
                if (!Tables.Contains(table))
                    Tables.Add(table);
            }
        }
        protected List<string> NavTables
        {
            get { return getValue<List<string>>("NavTables"); }
            set { setValue("NavTables", value); }
        }
        CacheKey key
        {
            get { return getValue<CacheKey>("key"); }
            set { setValue("key", value); }
        }
        Stack<string> Conversions
        {
            get { return getValue<Stack<string>>("Conversions"); }
            set { setValue("Conversions", value); }
        }
        /// <summary>
        /// 当前的导航字段
        /// </summary>
        protected NavigatField CurrentNav
        {
            get { return getValue<NavigatField>("CurrentNav"); }
            set { setValue("CurrentNav", value); }
        }
        /// <summary>
        /// 导航的 别名
        /// </summary>
        protected string NavAlias
        {
            get { return getValue<string>("NavAlias"); }
            set { setValue("NavAlias", value); }
        }
        /// <summary>
        /// 元数据和别名集合
        /// </summary>
        protected Dictionary<ParameterExpression, PMAlias> PMA
        {
            get { return getValue<Dictionary<ParameterExpression, PMAlias>>("pma"); }
            set { setValue("pma", value); }
        }
        /// <summary>
        /// any 方法的expression
        /// </summary>
        protected List<Expression> Instances
        {
            get
            {
                var instances = getValue<List<Expression>>("instances");
                if (instances == null)
                {
                    instances = new List<Expression>();
                    setValue("instances", instances);
                }
                return instances;
            }
            set { setValue("instances", value); }
        }
        #endregion

        #region

        public QueryCommand Translate(LambdaExpression node, params PMAlias[] pmas)
        {
            node = new MemberConditionVistor(node).Result;
            var _key = new CacheKey(node);
            for (var i = 0; i < pmas.Length; i++)
            {
                var pma = pmas[i];
                _key.AddHash(pma.Alias.GetHashCode() * i);
            }

            if (!commands.ContainsKey(_key))
            {
                lock (commands)
                {
                    if (!keylocker.ContainsKey(_key))
                        keylocker[_key] = new object();
                }
                lock (keylocker[_key])
                {
                    if (!commands.ContainsKey(_key))
                    {
                        var param = m_Param;
                        m_Param = null;
                        PMA = pmas.ToDictionary(s => s.Parameter);
                        var pma = PMA[node.Parameters[0]];
                        MetaData = pma.Metadata;
                        Alias = pma.Alias;

                        key = _key;
                        Formater = new CommandFormat();
                        builder = new StringBuilder();
                        Conversions = new Stack<string>();
                        Tables = new List<string>();
                        Visit(node);
                        Formater.BuildFormat(builder.ToString(), Tables);
                        commands[key] = Formater;
                        m_Param = param;
                    }
                }
            }

            var cmd = commands[_key].Create(_key.Parameters);
            cmd.HashCode = _key.GetHashCode();
            return cmd;
        }

        public QueryCommand Translate(ModelMetadata metadata, LambdaExpression node)
        {
            node = new MemberConditionVistor(node).Result;
            var _key = new CacheKey(node);
            if (!commands.ContainsKey(_key))
            {
                lock (commands)
                {
                    if (!keylocker.ContainsKey(_key))
                        keylocker[_key] = new object();
                }
                lock (keylocker[_key])
                {
                    if (!commands.ContainsKey(_key))
                    {
                        key = _key;
                        Formater = new CommandFormat();
                        MetaData = metadata;
                        builder = new StringBuilder();
                        Conversions = new Stack<string>();
                        Tables = new List<string>();
                        PMA = new Dictionary<ParameterExpression, PMAlias>();
                        Alias = "it";
                        PMA[node.Parameters[0]] = new PMAlias(node.Parameters[0], metadata, Alias);
                        Visit(node);
                        Formater.BuildFormat(builder.ToString(), Tables);
                        commands[key] = Formater;
                    }
                }
            }
            Formater = commands[_key];
            var cmd = Formater.Create(_key.Parameters);
            cmd.HashCode = _key.GetHashCode();
            return cmd;
        }

        void translatenav(LambdaExpression exp)
        {
            var _builder = builder;
            var _cmd = Formater;
            var param = m_Param;
            var alias = Serial.GetRandom(6, false);
            var meta = CurrentNav.RelationModelMeta;
            var palias = new Dictionary<ParameterExpression, PMAlias>();
            foreach (var pair in PMA)
            {
                palias[pair.Key] = pair.Value;
            }
            palias[exp.Parameters[0]] = new PMAlias(exp.Parameters[0], meta, alias);
            var pinstances = new List<Expression>(Instances);
            m_Param = null;
            var tables = new List<string>();
            MetaData = meta;
            builder = _builder;
            Formater = _cmd;
            Conversions = new Stack<string>();
            Alias = alias;
            Tables = tables;
            PMA = palias;
            Instances = pinstances;
            Visit(exp.Body);
            m_Param = param;
            NavAlias = alias;
            NavTables = new List<string>(tables);
        }
        #endregion

        #region 虚拟方法
        protected virtual string Wrapper(string name)
        {
            return "\"" + name + "\"";
        }

        /// <summary>
        /// 是否包含布尔型
        /// </summary>
        protected virtual bool HasBoll { get { return false; } }

        /// <summary>
        /// 等号
        /// </summary>
        protected virtual string EQ { get { return "="; } }
        /// <summary>
        /// 不等号
        /// </summary>
        protected virtual string NE { get { return "<>"; } }
        /// <summary>
        /// 大于号
        /// </summary>
        protected virtual string GT { get { return ">"; } }
        /// <summary>
        /// 小于等于号
        /// </summary>
        protected virtual string GTE { get { return ">="; } }
        /// <summary>
        /// 小于号
        /// </summary>
        protected virtual string LT { get { return "<"; } }
        /// <summary>
        /// 小于等于号
        /// </summary>
        protected virtual string LTE { get { return "<="; } }

        /// <summary>
        /// 包含于
        /// </summary>
        protected virtual string IN { get { return "in"; } }
        /// <summary>
        /// 并且
        /// </summary>
        protected virtual string AND { get { return "and"; } }
        /// <summary>
        /// 或者
        /// </summary>
        protected virtual string OR { get { return "or"; } }
        /// <summary>
        /// 非
        /// </summary>
        protected virtual string Not { get { return "not"; } }
        /// <summary>
        /// 加
        /// </summary>
        protected virtual string Add { get { return "+"; } }

        /// <summary>
        /// 字符串连接
        /// </summary>
        protected virtual string Concat { get { return "+"; } }
        /// <summary>
        /// 减
        /// </summary>
        protected virtual string Subtract { get { return "-"; } }
        /// <summary>
        /// 乘
        /// </summary>
        protected virtual string Multiply { get { return "*"; } }
        /// <summary>
        /// 除
        /// </summary>
        protected virtual string Divide { get { return "/"; } }
        /// <summary>
        /// 空
        /// </summary>
        protected virtual string IsNull { get { return "is null"; } }
        /// <summary>
        /// 不为空
        /// </summary>
        protected virtual string NotNull { get { return "is not null"; } }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        protected virtual string BuildStartWith(string left, string right)
        {
            return string.Format("{0} like {1}+'%'", left, right);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        protected virtual string BuildEndWidth(string left, string right)
        {
            return string.Format("{0} like '%'+{1}", left, right);
        }

        /// <summary>
        /// 构建值为空的字符串
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        protected abstract string BuildIsNull(string left, string right);

        /// <summary>
        /// 构建包含
        /// </summary>
        /// <param name="left">左侧</param>
        /// <param name="right">右侧</param>
        /// <returns></returns>
        protected abstract string BuildContain(string left, string right);

        /// <summary>
        /// 构建 对多的关系
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        protected virtual string BuildExists(string left, string right)
        {
            List<string> tables = new List<string>();
            if (CurrentNav.Field_Type == FieldType.one2many)
            {
                tables.Add(SqlUnit.WrapNolock(string.Format("{0} as {1}", Wrapper(CurrentNav.RelationModelMeta.TableName), NavAlias)));
                //tables.AddRange(NavTables);
                var fromexp = string.Join(" ", tables);
                if (string.IsNullOrEmpty(CurrentNav.RelationField))
                    return string.Format("EXISTS(SELECT 1 FROM {0} WHERE {1})", fromexp, right);

                return string.Format("EXISTS(SELECT 1 FROM {0} WHERE {1}.{2}={3} and {4})",
                   fromexp, NavAlias, Wrapper(CurrentNav.RelationField), left, right);
            }
            if (CurrentNav.Field_Type == FieldType.many2many)
            {
                var nfield = CurrentNav as Many2ManyField;
                var rfield = nfield.RelationModelMeta.GetField(nfield.RelationField) as Many2ManyField;
                tables.Add(SqlUnit.WrapNolock(string.Format("{0} as {1}", Wrapper(nfield.RelationModelMeta.TableName), NavAlias)));
                //tables.AddRange(tables);
                var fromexp = string.Join(" ", tables);
                string middtable = SqlUnit.WrapNolock(string.Format("{0} as midd", Wrapper(nfield.MiddleTable)));
                return string.Format("EXISTS(SELECT 1 FROM {0} WHERE {2}={1}.{3} and EXISTS(SELECT 1 FROM {4}  WHERE {1}.{6}={5}.{7} and {8}))",
                    middtable, "midd", left, Wrapper(nfield.MiddleField),
                    fromexp, NavAlias, Wrapper(rfield.MiddleField), Wrapper(rfield.PrimaryField), right);
            }
            return "";
        }

        /// <summary>
        /// 构建indexof
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        protected abstract string BuildIndexOf(string left, string right);

        /// <summary>
        /// 转换为字符串
        /// </summary>
        /// <param name="exp"></param>
        /// <returns></returns>
        protected virtual string ToVarchar(string exp)
        {
            return string.Format("CAST({0} as varchar)", exp);
        }

        protected string GetConversion(string name)
        {
            switch (name)
            {
                case "AndAlso": return AND;
                case "OrElse": return OR;
                case "Not": return Not;
                case "Equal": return EQ;
                case "NotEqual": return NE;
                case "GreaterThan": return GT;
                case "GreaterThanOrEqual": return GTE;
                case "LessThan": return LT;
                case "LessThanOrEqual": return LTE;
                case "Add": return Add;
                case "Subtract": return Subtract;
                case "Divide": return Divide;
                case "Multiply": return Multiply;
                case "In": return IN;
                case "Concat": return Concat;
                default: return "";
            }
        }

        public abstract string BuildTop1(string cmd);
        #endregion

        void buildcond(string left, string conversion, string right)
        {
            switch (conversion)
            {
                case "Contains": builder.Append(BuildContain(left, right)); break;
                case "ArrayContains": builder.Append(BuildContain("','+" + left + "+','", "','+" + right + "+','")); break;
                case "StartsWith": builder.Append(BuildStartWith(left, right)); break;
                case "EndsWith": builder.Append(BuildEndWidth(left, right)); break;
                case "Coalesce": builder.Append(BuildIsNull(left, right)); break;
                case "Any": builder.Append(BuildExists(left, right)); break;
                case "IndexOf": builder.Append(BuildIndexOf(left, right)); break;
                default: throw new NotImplementedException(string.Format("未实现方法{0}", conversion));
            }
        }
        void _visit(Expression left, string conversion, Expression right)
        {
            var orgleft = left;
            var orgright = right;

            left = ExprHelper.StripQuotes(left);
            right = ExprHelper.StripQuotes(right);

            if (!ExprHelper.IsMember(left))
                left = Expression.Constant(null);
            if (!ExprHelper.IsMember(right))
                right = Expression.Constant(null);
            string lstr, rstr;
            Conversions.Push(conversion);
            if (conversion == "Concat")
            {
                if (orgleft.NodeType == ExpressionType.Convert)
                    left = Expression.Convert(left, orgleft.Type);
                if (orgright.NodeType == ExpressionType.Convert)
                    right = Expression.Convert(left, orgright.Type);
            }

            var tmp = builder;
            builder = new StringBuilder();
            Visit(left);
            lstr = builder.ToString();

            builder = new StringBuilder();
            Visit(right);
            rstr = builder.ToString();

            builder = tmp;
            Conversions.Pop();

            var conv = GetConversion(conversion);
            if (conv != "")
            {
                if ((conversion == "Equal" || conversion == "NotEqual") && (left.NodeType == ExpressionType.Constant || right.NodeType == ExpressionType.Constant))
                {
                    if (right.NodeType == ExpressionType.Constant)
                        builder.AppendFormat("{0} {1}", lstr, rstr);
                    else
                        builder.AppendFormat("{0} {1}", rstr, lstr);
                }
                else
                    builder.AppendFormat("{0} {1} {2}", lstr, conv, rstr);
            }
            else
                buildcond(lstr, conversion, rstr);
        }
        void VisitRelation(NavigatField navfield, Stack<string> members)
        {
            var meta = navfield.RelationModelMeta;
            var member = members.Pop();
            var field = meta.GetField(member);
            if (field is NavigatField)
            {
                CurrentNav = field as NavigatField;
                if (members.Count > 0)
                    VisitRelation(CurrentNav, members);
            }
        }
        string BuildRelation(NavigatField navfield, Stack<string> members, string alias = null)
        {
            if (navfield == null || members.Count == 0)
                return "";
            var meta = navfield.RelationModelMeta;
            var fieldname = navfield.Name + "." + string.Join(".", members);
            var unit = new SqlTable(navfield.Metadata, new SqlExt(Wrapper, () => this, null), alias, fieldname).GetUnit(false, fieldname);
            AddTables(unit.Tables);
            if (unit.Distinct)
                Formater.Distinct = true;
            VisitRelation(navfield, members);
            var fieldexp = unit.Fields.LastOrDefault();
            var index = fieldexp.LastIndexOf(" as ");
            return index > 0 ? fieldexp.Substring(0, index) : fieldexp;
        }
        string BuildMember(ParameterExpression param, Stack<string> members)
        {
            var pma = PMA[param];
            var meta = pma.Metadata;
            var alias = pma.Alias;

            var member = members.Pop();
            var field = meta.GetField(member);
            if (field == null)
                throw new Exception2("the model {0} dont contains the field {1}!", meta.Name, member);
            var navfield = field as NavigatField;
            if (members.Count == 0)
            {
                CurrentNav = navfield;
                return string.Format("{0}.{1}", alias, Wrapper(navfield == null ? member : navfield.PrimaryField));
            }
            else
            {
                var sql = BuildRelation(navfield, members, alias);
                return sql;
            }
        }
        protected override Expression VisitMember(MemberExpression node)
        {
            var members = ExprHelper.GetMembers(node);
            builder.Append(BuildMember(members.Item1, members.Item2));
            string currentconversion = Conversions.FirstOrDefault();
            if (currentconversion == null || currentconversion == "AndAlso" || currentconversion == "OrElse")
            {
                builder.Append("=1");
            }
            return node;
        }
        protected override Expression VisitConstant(ConstantExpression node)
        {
            var param = Formater.CreateName();
            var currentconversion = Conversions.FirstOrDefault();
            if (currentconversion == null || currentconversion == "AndAlso" || currentconversion == "OrElse")
            {
                if (!HasBoll)
                    builder.AppendFormat("1 {0} @{1}", EQ, param);
                else
                    builder.AppendFormat("@{0}", param);
            }
            else
            {
                if (currentconversion == "In")
                {
                    var format = Formater.CretaeFormater(param, s => s.Children.Count > 0 ? string.Join(",", s.Children.Select(t => "@" + t.Name)) : "null", s => false);
                    builder.AppendFormat("({0}{1}{2})", "{", format.Index, "}");
                }
                else if (currentconversion == "Equal")
                {
                    var conv = EQ;
                    var alt = IsNull;
                    var format = Formater.CretaeFormater(param, s => s.Value == null ? " " + alt : string.Format(" {0} @{1}", conv, s.Name), s => s.Value == null);
                    builder.AppendFormat("{0}{1}{2}", "{", format.Index, "}");
                }
                else if (currentconversion == "NotEqual")
                {
                    var conv = NE;
                    var alt = NotNull;
                    var format = Formater.CretaeFormater(param, s => s.Value == null ? " " + alt : string.Format(" {0} @{1}", conv, s.Name), s => s.Value == null);
                    builder.AppendFormat("{0}{1}{2}", "{", format.Index, "}");
                }
                else
                    builder.AppendFormat("@{0}", param);
            }
            return node;
        }

        protected override Expression VisitUnary(UnaryExpression node)
        {
            if (node.NodeType == ExpressionType.Not)
            {
                builder.Append(Not);
                builder.Append("(");
                Visit(node.Operand);
                builder.Append(")");
                return node.Operand;
            }
            if (node.NodeType == ExpressionType.Convert && Conversions.FirstOrDefault() == "Concat")
            {
                var tmpbuilder = builder;
                builder = new StringBuilder();
                Visit(node.Operand);
                tmpbuilder.Append(ToVarchar(builder.ToString()));
                builder = tmpbuilder;
                return node.Operand;
            }
            return base.VisitUnary(node);
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            builder.Append("(");
            var con = node.NodeType.ToString();
            var left = node.Left;
            var right = node.Right;

            if (node.NodeType == ExpressionType.Add && node.Type == typeof(string))
            {
                con = "Concat";
            }
            _visit(node.Left, con, node.Right);
            builder.Append(")");
            return node;
        }

        bool bodycompleted;
        //{
        //    get { return getValue<bool>("bodycompleted"); }
        //    set { setValue("bodycompleted", value); }
        //}
        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            if (!bodycompleted)
            {
                bodycompleted = true;
                return Visit(node.Body);
            }
            else
            {
                translatenav(node);
                return node;
            }
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.Name == "GetReferences" || node.Method.Name == "get_Item" || node.Method.Name == "GetEntities")
            {
                var members = ExprHelper.GetMembers(node);
                builder.Append(BuildMember(members.Item1, members.Item2));
            }
            else
            {
                Expression left = null;
                Expression right = null;
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

                //if (node.Method.Name == "Any")
                //{
                //    var fieldname = "";

                //    if (left.NodeType == ExpressionType.MemberAccess)
                //    {
                //        fieldname = (left as MemberExpression).Member.Name;
                //        Instances.Add((left as MemberExpression).Expression);
                //    }
                //    else if (left.NodeType == ExpressionType.Call && (left as MethodCallExpression).Method.Name == "GetReferences")
                //        fieldname = ((left as MethodCallExpression).Arguments[0] as ConstantExpression).Value as string;
                //    var navfield = MetaData.GetField(fieldname) as NavigatField;
                //    if (navfield != null && navfield.RelationField != navfield.RelationModelMeta.PrimaryField.Name && navfield.Specific != null)
                //    {
                //        LambdaExpression lambda = right as LambdaExpression;
                //        // var tmpexp = navfield.Specific.GetExpression(navfield.RelationModelMeta, lambda.Parameters.FirstOrDefault(), new ParameterExpression[] { (left as MemberExpression).Expression as ParameterExpression });
                //        var tmpexp = navfield.Specific.GetExpression(navfield.RelationModelMeta, lambda.Parameters.FirstOrDefault(), Instances.AsEnumerable().Reverse().ToArray());
                //        right = Expression.Lambda(Expression.AndAlso(lambda.Body, tmpexp.Body), lambda.Parameters.ToArray());
                //    }
                //}

                _visit(left, node.Method.Name, right);
            }
            return node;
        }
    }

}
