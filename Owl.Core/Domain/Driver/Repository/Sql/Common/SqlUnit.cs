using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using Owl.Util;
using Owl.Feature;
namespace Owl.Domain.Driver.Repository.Sql
{
    /// <summary>
    /// sql单元
    /// </summary>
    public class SqlUnit
    {
        public static SqlUnit CreateUnit(string[] tables, params string[] elements)
        {
            var unit = new SqlUnit();
            if (elements != null)
                unit.AddField(elements);
            if (tables != null)
                unit.AddTable(tables);
            return unit;
        }
        /// <summary>
        /// 当 RepositoryRunning.NoLock 为true时自动加上nolock 标记
        /// </summary>
        /// <returns>The nolock.</returns>
        /// <param name="s">S.</param>
        public static string WrapNolock(string s)
        {
            if (!RepositoryRunning.NoLock)
                return s;
            if (s.Contains("with(NOLOCK)"))
                return s;
            if (s.StartsWith("left join", StringComparison.Ordinal))
            {
                var index = s.IndexOf(" on ", StringComparison.Ordinal);
                return s.Insert(index, " with(NOLOCK)");
            }
            else
                return string.Format("{0} with(NOLOCK)", s);
        }

        HashSet<string> m_elements = new HashSet<string>();
        /// <summary>
        /// 字段集合
        /// </summary>
        public IEnumerable<string> Fields { get { return m_elements; } }

        public void AddField(params string[] fields)
        {
            foreach (var field in fields)
            {
                if (!m_elements.Contains(field))
                    m_elements.Add(field);
            }
        }
        string strfield;
        /// <summary>
        /// 字段集合
        /// </summary>
        public string StrField
        {
            get
            {
                if (strfield == null)
                    strfield = string.Join(",", Fields);
                return strfield;
            }
        }

        HashSet<string> m_tables = new HashSet<string>();
        /// <summary>
        /// 数据表集合
        /// </summary>
        public IEnumerable<string> Tables { get { return m_tables; } }

        public void AddTable(params string[] tables)
        {
            foreach (var table in tables)
            {
                if (!m_tables.Contains(table))
                    m_tables.Add(table);
            }
        }

        string nolocktable;
        string strtable;
        public string StrTable
        {
            get
            {
                if (RepositoryRunning.NoLock)
                {
                    if (nolocktable == null)
                    {
                        nolocktable = string.Join(" ", Tables.Select(s => WrapNolock(s)).Distinct());
                    }
                    return nolocktable;
                }
                else
                {
                    if (strtable == null)
                        strtable = string.Join(" ", Tables);
                    return strtable;
                }
            }
        }

        string strorder;
        public string StrOrder
        {
            get
            {
                if (strorder == null)
                    strorder = m_elements.Count == 0 ? "" : " order by " + StrField;
                return strorder;
            }
        }

        /// <summary>
        /// 需要distinct
        /// </summary>
        public bool Distinct { get; set; }

        /// <summary>
        /// 合并sql 单元
        /// </summary>
        /// <param name="unit"></param>
        public void Merge(SqlUnit unit)
        {
            if (!Distinct)
                Distinct = unit.Distinct;
            foreach (var field in unit.Fields)
            {
                if (!m_elements.Contains(field))
                    m_elements.Add(field);
            }
            foreach (var table in unit.Tables)
            {
                if (!m_tables.Contains(table))
                    m_tables.Add(table);
            }
        }
    }

    public class SqlExt
    {
        public Func<string, string> Wrapper { get; private set; }

        public Func<SqlTranslator> Translator { get; private set; }


        public Func<Many2OneField, string, string> M2OMultiple { get; private set; }

        public SqlExt(Func<string, string> wrapper, Func<SqlTranslator> translator, Func<Many2OneField, string, string> m2o)
        {
            Wrapper = wrapper;
            if (wrapper == null)
                Wrapper = s => string.Format("\"{0}\"", s);
            Translator = translator;
            M2OMultiple = m2o;
        }
    }

    /// <summary>
    /// sql table
    /// </summary>
    public class SqlTable
    {
        public SqlTable(ModelMetadata metadata, SqlExt ext, string alias, string[] fields, string[] disfields)
        {
            ModelMeta = metadata;
            Ext = ext;
            Name = metadata.TableName;
            Alias = alias.Coalesce("it");
            if (fields == null)
                fields = new string[0];
            if (disfields == null)
                disfields = new string[0];

            Fields = new Dictionary<string, SqlField>();
            if (fields.Length == 0 && disfields.Length == 0)
            {
                foreach (var field in metadata.GetFields())
                {
                    if (field.Field_Type == FieldType.one2many)
                        continue;
                    Fields[field.Name] = new SqlField(field, this);
                }
            }
            else
            {
                foreach (var key in fields)
                {
                    var tmp = key.Split(new char[] { '.' }, 2);
                    var field = metadata.GetField(tmp[0]);
                    if (field != null)
                        Fields[field.Name] = new SqlField(field, this, tmp.Length == 1 ? "" : tmp[1]);
                }
                foreach (var key in disfields)
                {
                    var tmp = key.Split(new char[] { '.' }, 2);
                    var field = metadata.GetField(tmp[0]);
                    if (field != null)
                        Fields[field.Name] = new SqlField(field, this, tmp.Length == 1 ? "" : tmp[1]) { IsDisplay = true };
                }
            }
        }

        public SqlTable(ModelMetadata metadata, SqlExt ext, string alias, params string[] fields)
            : this(metadata, ext, alias, fields, null)
        {

        }

        /// <summary>
        /// 对象元数据
        /// </summary>
        public ModelMetadata ModelMeta { get; private set; }

        /// <summary>
        /// 表名称
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// 别名
        /// </summary>
        public string Alias { get; set; }

        string m_walias;
        /// <summary>
        /// 包装过的别名
        /// </summary>
        public string WAlias
        {
            get
            {
                if (m_walias == null)
                    m_walias = Wrap(Alias);
                return m_walias;
            }
        }
        /// <summary>
        /// 关联的字段
        /// </summary>
        public Dictionary<string, SqlField> Fields { get; private set; }

        /// <summary>
        /// 根据字段名称获取字段
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public SqlField GetField(string name)
        {
            if (Fields.ContainsKey(name))
                return Fields[name];
            var mfield = ModelMeta.GetField(name);
            if (mfield != null && Fields.ContainsKey(mfield.Name))
                return Fields[mfield.Name];
            return null;
        }

        /// <summary>
        /// sql table 扩展器
        /// </summary>
        public SqlExt Ext { get; private set; }

        public string Wrap(string str)
        {
            return Ext.Wrapper(str);
        }

        public QueryCommand Translate(ModelMetadata meta, LambdaExpression exp)
        {
            return Ext.Translator().Translate(meta, exp);
        }

        public QueryCommand Translate(LambdaExpression exp, params PMAlias[] pmas)
        {
            return Ext.Translator().Translate(exp, pmas);
        }

        public string M2O(Many2OneField field, string exp)
        {
            return Ext.M2OMultiple(field, exp);
        }

        protected SqlUnit _GetUnit(bool forui, IEnumerable<string> keys)
        {
            var unit = new SqlUnit();
            if (Alias == "it")
                unit.AddTable(string.Format("{0} as {1}", Wrap(Name), Alias));
            foreach (var key in keys)
            {
                var tmp = key.Split(new char[] { '.' }, 2);
                if (!Fields.ContainsKey(tmp[0]))
                {
                    if (key.Contains(" as "))
                    {
                        var tfield = key.Split(new string[] { " as " }, StringSplitOptions.RemoveEmptyEntries);
                        var spec = Specification.Math(tfield[0]);
                        if (spec != null && spec.Members.All(s => Fields.ContainsKey(s)))
                        {
                            spec.WrapperMember = s =>
                            {
                                return string.Format("ISNULL({0},0)", Fields[s].GetUint(forui).StrField);
                            };
                            unit.AddField(string.Format("{0} as {1}", spec.ToString(), tfield[1]));
                        }
                    }
                    continue;
                }
                var field = Fields[tmp[0]];
                if (field.FieldMeta.Field_Type == FieldType.many2many)
                    continue;

                if (!forui && field.IsDisplay)
                    continue;
                if (tmp.Length == 2)
                {
                    unit.Merge(new SqlField(field.FieldMeta, this, tmp[1]).GetUint(forui));
                }
                else
                    unit.Merge(Fields[key].GetUint(forui));

            }
            return unit;
        }

        SqlUnit fullnoui;
        protected SqlUnit FullNoUI
        {
            get
            {
                if (fullnoui == null)
                    fullnoui = _GetUnit(false, Fields.Keys);
                return fullnoui;
            }
        }

        SqlUnit fullforui;
        protected SqlUnit FullForUI
        {
            get
            {
                if (fullforui == null)
                    fullforui = _GetUnit(true, Fields.Keys);
                return fullforui;
            }
        }

        /// <summary>
        /// 获取sqlunit
        /// </summary>
        /// <param name="forui">是否为ui查询</param>
        /// <param name="selector">返回的字段</param>
        /// <returns></returns>
        public SqlUnit GetUnit(bool forui, params string[] selector)
        {
            if (selector.Length == 0)
                return forui ? FullForUI : FullNoUI;
            return _GetUnit(forui, selector);
        }
    }

    /// <summary>
    /// sql字段
    /// </summary>
    public class SqlField
    {
        string getalias(Many2OneField field)
        {
            var alias = field.Name;
            //if (field.Name == field.GetFieldname())
            //    alias = string.Format("{0}__ValueM2O__", field.Name);
            if (Table.Alias != "it")
                alias = string.Format("{0}_{1}", Table.Alias, alias);
            return alias;
        }

        public SqlField(FieldMetadata meta, SqlTable table, string lname = "")
        {
            FieldMeta = meta;
            Name = meta.Name;
            Table = table;
            if (meta.Field_Type == FieldType.many2one)
            {
                var navmeta = meta as Many2OneField;
                var rfields = new List<string>();
                var disfields = new List<string>();
                if (!string.IsNullOrEmpty(lname))
                    rfields.Add(lname);
                else
                {
                    foreach (var disfield in navmeta.RelationDisField)
                    {
                        var fmeta = navmeta.RelationModelMeta.GetField(disfield);
                        if (fmeta != null)
                        {
                            disfields.Add(disfield);
                            if (fmeta.Field_Type == FieldType.select)
                                disfields.AddRange(fmeta.GetDomainField().Dependence);
                        }
                    }
                }
                Relation = new SqlTable(navmeta.RelationModelMeta, table.Ext, getalias(navmeta), rfields.ToArray(), disfields.ToArray());
            }
            if (meta.Field_Type == FieldType.select)
            {
                foreach (var dep in meta.GetDomainField().Dependence)
                {
                    var tmp = dep.Split(new char[] { '.' }, 2);
                    var field = table.ModelMeta.GetField(tmp[0]);
                    if (field is Many2OneField && tmp.Length > 1)
                    {
                        var navfield = field as Many2OneField;
                        Relation = new SqlTable(navfield.RelationModelMeta, table.Ext, getalias(navfield), null, new[] { tmp[1] });
                    }
                }
            }
        }
        /// <summary>
        /// 字段名称
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// 为展现而存在的字段
        /// </summary>
        public bool IsDisplay { get; set; }

        /// <summary>
        /// 关联的table
        /// </summary>
        public SqlTable Table { get; private set; }

        /// <summary>
        /// 字段元数据
        /// </summary>
        public FieldMetadata FieldMeta { get; private set; }

        string m_wname;
        /// <summary>
        /// 包装过的字段名称
        /// </summary>
        public string WName
        {
            get
            {
                if (m_wname == null)
                    m_wname = Table.Wrap(FieldMeta.GetFieldname());
                return m_wname;
            }
        }

        /// <summary>
        /// 引用的table
        /// </summary>
        public SqlTable Relation { get; private set; }

        SqlUnit forui;
        SqlUnit noui;

        protected string GetExp()
        {
            var navmeta = FieldMeta as Many2OneField;
            string exp = "1=1";
            if (!navmeta.Multiple)
                exp = string.Format("{0}.{1}={2}.{3}", Table.Alias, Table.Wrap(navmeta.PrimaryField), Relation.WAlias, Table.Wrap(navmeta.RelationField));
            if (navmeta.RelationField != navmeta.RelationModelMeta.PrimaryField.Name && navmeta.Specific != null)
            {
                if (navmeta.Specific.Parameters.Contains("MetaName"))
                    Variable.CurrentParameters["MetaName"] = Table.ModelMeta.Name;
                //var cmd = Table.Translate(navmeta.RelationModelMeta, navmeta.FilterExp);

                var cmd = Table.Translate(navmeta.ExpHasParam,
                     new PMAlias(navmeta.ModelParamExp, Table.ModelMeta, Table.Alias),
                     new PMAlias(navmeta.ExpHasParam.Parameters[0], navmeta.RelationModelMeta, Relation.WAlias));
                var dcon = cmd.CommandText.Trim();

                //var dcon = cmd.CommandText.Trim();//.Replace("it.", Relation.Alias + ".");
                foreach (var par in cmd.Parameters)
                {
                    if (par.Value != null)
                        dcon = dcon.Replace("@" + par.Name, "'" + par.Value.ToString() + "'");
                }

                if (!string.IsNullOrEmpty(dcon))
                    exp = string.Format("{0} and {1}", exp, dcon);
            }
            return exp;
        }

        public SqlUnit GetUint(bool forui)
        {
            if (forui && this.forui != null)
                return this.forui;
            if (!forui && noui != null)
                return noui;

            var unit = new SqlUnit();
            var fieldname = FieldMeta.GetFieldname();

            if (FieldMeta.Field_Type == FieldType.many2many)
            {
                var m2m = FieldMeta as Many2ManyField;
                unit.AddField(string.Format("it.{0}", Table.Wrap(m2m.MiddleField)));
                unit.AddField(string.Format("it.{0}", Table.Wrap(m2m.TargetMiddleField)));
                unit.AddTable(string.Format("{0} as it", Table.Wrap(m2m.MiddleTable)));
            }
            else if (FieldMeta.Field_Type == FieldType.one2many)
            {
                unit.AddField(string.Format("{0}.{1}", Table.WAlias, Table.Wrap((FieldMeta as NavigatField).PrimaryField)));
            }
            else
            {
                if (Table.Alias != "it")
                    unit.AddField(string.Format("{0}.{1} as {2}", Table.WAlias, WName, Table.Wrap(string.Format("{0}_{1}", Table.Alias, fieldname))));
                else
                    unit.AddField(string.Format("it.{0}", WName));
            }
            if (Relation != null && FieldMeta.Field_Type == FieldType.many2one)
            {
                Relation.Fields.Remove(FieldMeta.GetDomainField().RelationField);
            }
            if (Relation != null)
            {
                if (FieldMeta.Field_Type == FieldType.many2one)
                {
                    var navmeta = FieldMeta as Many2OneField;
                    if (!(navmeta.IsSingle && FieldMeta.PropertyType != navmeta.PropertyType))
                    {
                        string exp = GetExp();
                        if (navmeta.Multiple)
                        {
                            if (forui)
                            {
                                var tmp = Table.M2O(navmeta, exp.Replace(Relation.Alias + ".", "").Replace(Relation.WAlias + ".", ""));
                                unit.AddField(string.Format("{0} as {1}", tmp, Table.Wrap(string.Format("{0}_{1}", Relation.Alias, Relation.Fields.Keys.FirstOrDefault()))));
                            }
                        }
                        else if (forui || Relation.Fields.Values.Any(s => !s.IsDisplay))
                        {
                            var leftjoin = true;
                            if (navmeta.RelationField != navmeta.RelationModelMeta.PrimaryField.Name)
                            {
                                if (navmeta.IsPrimary)
                                {
                                    leftjoin = false;
                                    foreach (var field in Relation.Fields.Values)
                                    {
                                        var f_name = field.FieldMeta.GetFieldname();
                                        var top1 = Table.Ext.Translator().BuildTop1(string.Format("{0} from (select * from {1}) as {2} where {3} order by {4} Desc", Table.Wrap(f_name), Table.Wrap(Relation.Name), Table.Wrap(Relation.Alias), exp, Table.Wrap("Created")));
                                        unit.AddField(string.Format("({0}) as {1}", top1, Table.Wrap(string.Format("{0}_{1}", Relation.Alias, f_name))));
                                    }
                                }
                                else
                                    unit.Distinct = true;
                            }
                            if (leftjoin)
                            {
                                unit.AddTable(string.Format("left join {0} as {1} on {2}", Table.Wrap(Relation.Name), Relation.WAlias, exp));
                                unit.Merge(Relation.GetUnit(forui));
                            }
                        }
                    }
                }
                else if (FieldMeta.Field_Type == FieldType.select)
                {
                    unit.Merge(Relation.GetUnit(forui));
                }
            }
            if (forui)
                this.forui = unit;
            else
                this.noui = unit;
            return unit;
        }
    }

    public class SqlSort
    {
        protected bool ForUI;
        protected SqlTable Table;
        protected SortBy Sortby;
        protected string Alias;

        public SqlSort(SqlTable table, SortBy sortby, bool forui, string alias = "it")
        {
            ForUI = forui;
            Table = table;
            Sortby = sortby ?? new SortBy();
            Alias = alias;
        }

        /// <summary>
        /// 获取排序的sql单元
        /// </summary>
        /// <param name="alias">是否使用别名</param>
        public SqlUnit GetUnit(bool alias = false)
        {
            var unit = new SqlUnit();
            foreach (var pair in Sortby)
            {
                var orderby = pair.Value == SortOrder.Ascending ? "asc" : "desc";

                var tmp = pair.Key.Split(new char[] { '.' }, 2);
                var field = Table.GetField(tmp[0]);
                if (field == null || field.Name != tmp[0] || !ForUI || field.FieldMeta.Field_Type != FieldType.many2one)
                {
                    string tmpstr = null;
                    if (field == null)
                    {
                        var spec = Specification.Math(pair.Key);
                        if (spec != null)
                        {
                            spec.WrapperMember = s => string.Format("ISNULL({0}.{1},0)", Table.Wrap(Alias), Table.Wrap(s));
                            tmpstr = string.Format("{0} {1}", spec.ToString(), orderby);
                        }
                    }
                    if (tmpstr == null)
                        tmpstr = field == null ? string.Format("{0} {1}", Table.Wrap(pair.Key), orderby) : string.Format("{0}.{1} {2}", Table.Wrap(Alias), Table.Wrap(field.FieldMeta.GetFieldname()), orderby);
                    unit.AddField(tmpstr);
                }
                else
                {
                    var tmpunit = Table.GetUnit(ForUI, pair.Key);
                    var tufield = tmpunit.Fields.LastOrDefault();//.Split(new string[] { " as " }, StringSplitOptions.RemoveEmptyEntries)[0];

                    //unit.AddField(string.Format("{0} {1}", tufield, orderby));

                    var index = tufield.LastIndexOf(" as ");
                    if (index == -1)
                        unit.AddField(string.Format("{0} {1}", tufield, orderby));
                    else if (alias)
                        unit.AddField(string.Format("{0} {1}", tufield.Substring(index + 4), orderby));
                    else
                    {
                        unit.AddField(string.Format("{0} {1}", tufield.Substring(0, index), orderby));
                        foreach (var t in tmpunit.Tables)
                            unit.AddTable(t);
                    }
                }
            }
            return unit;
        }

    }
}
