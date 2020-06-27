using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Data;
using Owl.Util;
using System.Runtime.InteropServices;
using Om.Sys.Log;
using System.Diagnostics;

namespace Owl.Domain.Driver.Repository.Sql
{
    public enum AlterTable
    {
        AlterTable,
        AddColumn,
        AlterColumn,
        DropColumn,
    }

    public class DBColumn : SmartObject
    {
        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public bool Identity { get; set; }

        /// <summary>
        /// 是否主键
        /// </summary>
        public bool PK { get; set; }
        /// <summary>
        /// 字段类型
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// 长度
        /// </summary>
        public int Length { get; set; }



        public int Scale { get; set; }

        /// <summary>
        /// 可为空
        /// </summary>
        public bool IsNull { get; set; }

        string StripDefault(string value)
        {
            if (!string.IsNullOrEmpty(value) && value.StartsWith("(", StringComparison.Ordinal) && value.EndsWith(")", StringComparison.Ordinal))
                return StripDefault(value.Substring(1, value.Length - 2));
            return value;
        }
        string _default;
        /// <summary>
        /// 默认值
        /// </summary>
        public string Default
        {
            get { return _default; }
            set
            {
                _default = StripDefault(value);
            }
        }

        /// <summary>
        /// 备注
        /// </summary>
        public string Memo { get; set; }

        /// <summary>
        /// 字段约束名称
        /// </summary>
        public string Constraint { get; set; }

        public bool DefaultEquals(string dvalue)
        {
            return Default == dvalue;
        }
    }

    public class DbField
    {
        public string Name { get; private set; }

        public FieldType FieldType { get; private set; }

        public int Size { get; private set; }

        public int Precision { get; private set; }

        public bool Required { get; private set; }

        public static DbField Create(FieldMetadata meta)
        {
            if (meta.Field_Type == FieldType.many2one && meta.CanIgnore)
                return null;
            var dbfield = new DbField();
            var field = meta.GetDomainField();
            dbfield.Name = meta.GetFieldname();
            var fieldtype = meta.Field_Type;
            dbfield.Size = field.Size;
            dbfield.Precision = field.Precision;
            dbfield.Required = meta.Required;
            if (meta.Field_Type == FieldType.many2one)
            {
                var nfield = meta as Many2OneField;
                var rfield = nfield.RelationModelMeta.GetField(field.RelationField).GetDomainField();
                fieldtype = DomainHelper.GetFieldType(Nullable.GetUnderlyingType(nfield.PrimaryType) ?? nfield.PrimaryType);
                dbfield.Size = rfield.Field_Type == fieldtype ? rfield.Size : DomainHelper.GetSize(fieldtype, nfield.PrimaryType);
                dbfield.Precision = rfield.Field_Type == fieldtype ? rfield.Precision : DomainHelper.GetPrecision(fieldtype, nfield.PrimaryType);
                dbfield.Required = field.Required;
            }
            else if (meta.Field_Type == FieldType.file || meta.Field_Type == FieldType.image)
            {
                dbfield.FieldType = DomainHelper.GetFieldType(Nullable.GetUnderlyingType(meta.PropertyType) ?? meta.PropertyType);
            }
            dbfield.FieldType = fieldtype;
            return dbfield;
        }
    }

    public abstract class SqlRepositoryProvider<TAggregateRoot> : RepositoryProvider<TAggregateRoot>
        where TAggregateRoot : AggRoot
    {
        protected virtual string Wrapper(string name)
        {
            return string.Format("[{0}]", name);
        }

        #region sql元数据字段缓存

        protected abstract string BuildM2OMultiple(Many2OneField field, string exp);
        Dictionary<string, SqlTable> sqltables = new Dictionary<string, SqlTable>(5);

        SqlTable m_table;
        /// <summary>
        /// 主表
        /// </summary>
        protected SqlTable Table
        {
            get
            {
                if (m_table == null)
                    m_table = sqltables[MetaData.Name];
                return m_table;
            }
        }
        void BuildTables(ModelMetadata metadata)
        {
            var ext = new SqlExt(Wrapper, () => Builder, BuildM2OMultiple);
            sqltables[metadata.Name] = new SqlTable(metadata, ext, null);
            foreach (var field in metadata.GetEntityRelated())
            {
                BuildTables(field.RelationModelMeta);
            }
        }
        /// <summary>
        /// 获取sqltable
        /// </summary>
        /// <param name="meta"></param>
        /// <param name="forui"></param>
        /// <param name="fields"></param>
        /// <returns></returns>
        Dictionary<string, SqlUnit> GetUnits(ModelMetadata meta, bool forui, string[] fields)
        {
            Dictionary<string, SqlUnit> units = new Dictionary<string, SqlUnit>();
            if (fields.Length == 0 && meta.Name == MetaData.Name)
            {
                foreach (var pair in sqltables)
                    units[pair.Key] = pair.Value.GetUnit(forui);
                foreach (var field in meta.GetFields<Many2ManyField>())
                {
                    units[string.Format("{0}.{1}", meta.Name, field.Name)] = Table.GetField(field.Name).GetUint(false);
                }
            }
            else
            {
                units[meta.Name] = sqltables[meta.Name].GetUnit(forui, fields);
                foreach (var field in meta.GetEntityRelated())
                {
                    if (fields.Length == 0 || fields.Contains(field.Name))
                    {
                        foreach (var pair in GetUnits(field.RelationModelMeta, forui, new string[0]))
                        {
                            units[pair.Key] = pair.Value;
                        }
                    }
                }
                if (meta.Name == MetaData.Name)
                {
                    foreach (var field in meta.GetFields<Many2ManyField>())
                    {
                        if (fields.Contains(field.Name))
                            units[string.Format("{0}.{1}", meta.Name, field.Name)] = Table.GetField(field.Name).GetUint(false);
                    }
                }
            }
            return units;
        }

        #endregion

        #region context
        /// <summary>
        /// sql 语句生成器
        /// </summary>
        protected abstract SqlTranslator Builder { get; }
        /// <summary>
        /// 上下文的类型
        /// </summary>
        protected abstract Type ContextType { get; }

        protected SqlRepositoryContext ReadContext { get; private set; }
        protected SqlRepositoryContext WriteContext
        {
            get
            {
                if (UnitOfWork.Current == null)
                {
                    string keys = string.Join(",", sqltables.Keys);
                    LogFile.Write("UnitOfWorkCurrent", "未将对象引用设置到对象的实例。：" + keys);
                }
                return (SqlRepositoryContext)UnitOfWork.Current.GetContext(ContextType);
            }
        }

        //protected abstract bool ToProcedure { get; }

        protected override void onInit()
        {
            ReadContext = (SqlRepositoryContext)Activator.CreateInstance(ContextType);
            BuildTables(MetaData);

            //buildfields(MetaData);
            //foreach (var key in CachedFields.Keys)
            //{
            //    cachefieldwithparentname[key] = dogetfields(key, true, null);
            //    cachefieldsnoparentname[key] = dogetfields(key, false, null);
            //}
        }
        #endregion

        #region build sql statement
        protected string[] DefaultTable { get { return new[] { string.Format("{0} as it", Wrapper(MetaData.TableName)) }; } }

        /// <summary>
        /// 获取查询字符串
        /// </summary>
        /// <param name="fields">字段</param>
        /// <param name="fieldtables">字段包含的表</param>
        /// <param name="whereexp">查询条件</param>
        /// <param name="wheretables">查询条件包含的表</param>
        /// <param name="sortby">排序</param>
        /// <param name="start"></param>
        /// <param name="limit"></param>
        /// <param name="child">是否group子查询</param>
        /// <returns></returns>
        protected abstract string GetQueryString(SqlUnit fields, QueryCommand where, SqlSort sortby, int start, int limit, bool child = false);

        protected abstract string[] createTemp();

        string BuildCmdStr(ModelMetadata metadata, Dictionary<string, SqlUnit> tables, string querycondition, List<string> models)
        {
            StringBuilder sb = new StringBuilder();
            var temp = createTemp();
            string tmptable = temp[0];
            sb.Append(temp[1]);
            sb.AppendFormat("insert into {0} {1};", tmptable, querycondition);
            var unit = tables[metadata.Name];
            sb.AppendFormat("select {0} from {1} where it.{2} in(select {2} from {3});", unit.StrField, unit.StrTable, Wrapper("Id"), tmptable);
            if (MetaData.Name == metadata.Name)
            {
                sb.AppendFormat("select * from {0};", temp[0]);
                models.Add("sorted");

                foreach (var m2m in metadata.GetFields<Many2ManyField>())
                {
                    var key = string.Format("{0}.{1}", metadata.Name, m2m.Name);
                    if (tables.ContainsKey(key))
                    {
                        var tunit = tables[key];
                        sb.AppendFormat("select {0} from {1}  where it.{2} in(select {3} from {4});",
                               tunit.StrField, tunit.StrTable, Wrapper(m2m.MiddleField), Wrapper("Id"), tmptable);
                        models.Add(string.Format("{0}.{1}", metadata.Name, m2m.Name));
                    }
                }
            }

            foreach (var navfield in metadata.GetEntityRelated())
            {
                if (tables.ContainsKey(navfield.RelationModelMeta.Name))
                {
                    models.Add(navfield.RelationModelMeta.Name);
                    if (navfield.RelationModelMeta.GetEntityRelated().Count() == 0)
                    {
                        var tunit = tables[navfield.RelationModelMeta.Name];
                        sb.AppendFormat("select {0} from {1}  where it.{2} in(select {3} from {4});",
                            tunit.StrField, tunit.StrTable, Wrapper(navfield.RelationField), Wrapper("Id"), tmptable);
                    }
                    else
                    {
                        var queryc = string.Format("select it.{0} from {1} as it where it.{2} in(select {0} from {3})", Wrapper("Id"),
                            Wrapper(navfield.RelationModelMeta.TableName), Wrapper(navfield.RelationField), tmptable);
                        sb.Append(BuildCmdStr(navfield.RelationModelMeta, tables, queryc, models));
                    }
                }
            }

            return sb.ToString();

        }
        QueryCommand BuildCmd(Expression<Func<TAggregateRoot, bool>> expression, Dictionary<string, SqlUnit> tables, SqlSort sortby, int start, int count, out List<string> models)
        {
            QueryCommand cmd = Builder.Translate(MetaData, expression);
            models = new List<string>() { MetaData.Name };
            if (tables.Count == 1)
            {
                cmd.BuildText(GetQueryString(tables[MetaData.Name], cmd, sortby, start, count));
            }
            if (tables.Count > 1)
            {
                string queryconditin = GetQueryString(SqlUnit.CreateUnit(DefaultTable, string.Format("it.{0}", Wrapper("Id"))), cmd, sortby, start, count);
                cmd.BuildText(BuildCmdStr(MetaData, tables, queryconditin, models));
            }
            return cmd;
        }
        protected override object DoInvoke(string name, IDictionary<string, object> param)
        {
            var cmd = new QueryCommand(name, new TranslatorParameterCollection(param.Select(s => new TranslatorParameter(s.Key, s.Value))));
            return ReadContext.ExecuteReaderRecords(MetaData, CommandType.StoredProcedure, cmd);
        }
        #endregion

        #region 实现

        protected virtual IEnumerable<TAggregateRoot> Execute(Expression<Func<TAggregateRoot, bool>> expression, SortBy sortby, int start, int count, string[] selector)
        {
            List<string> models;
            var cmd = BuildCmd(expression, GetUnits(MetaData, false, selector), new SqlSort(Table, sortby, false), start, count, out models);
            return ReadContext.ExecuteReader<TAggregateRoot>(MetaData, CommandType.Text, cmd, models);
        }
        protected virtual IEnumerable<TransferObject> Execute2(Expression<Func<TAggregateRoot, bool>> expression, SortBy sortby, int start, int count, bool translate, string[] selector)
        {
            List<string> models;
            if (selector.Length > 0 && !selector.Contains("Id"))
            {
                var tmp = new List<string>(selector);
                tmp.Add("Id");
                selector = tmp.ToArray();
            }
            var cmd = BuildCmd(expression, GetUnits(MetaData, translate, selector), new SqlSort(Table, sortby, translate), start, count, out models);
            return ReadContext.ExecuteReader2(MetaData, CommandType.Text, cmd, models, selector, translate);
        }
        protected override IEnumerable<TransferObject> DoRead(Guid[] id, bool translate, string[] selector)
        {
            //return Execute2(s => s.Id.In(id), null, 0, 0, translate, selector); 
            Stopwatch sw = new Stopwatch();
            sw.Start();
            var result = Execute2(s => s.Id.In(id), null, 0, 0, translate, selector);
            sw.Stop();
            LogFile.Write("sqltrace", $"DoRead -- 耗时：{sw.ElapsedMilliseconds}ms " +
                $"\n 查询结果数量:{result.Count()}" +
                //$"\n查询表名{typeof(TransferObject).FullName}" + 
                $"\n 查询参数：" +
                $"\n Id:{string.Join(",", id)}" +
                $"\n Translate:{translate}" +
                $"\n Selector:{string.Join(",", selector)}");
            return result;
        }
        protected override IEnumerable<TransferObject> DoGetList(Expression<Func<TAggregateRoot, bool>> expression, SortBy sortby, int start, int size, bool translate, string[] selector)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            var result = Execute2(expression, sortby, start, size, translate, selector);
            sw.Stop();
            LogFile.Write("sqltrace", $"DoGetList -- 耗时：{sw.ElapsedMilliseconds}ms " +
                $"\n 查询结果数量:{result.Count()}" +
                $"\n 表名{typeof(TAggregateRoot).FullName} \n 查询条件:{expression}" +
                $"\n 参数：\n SoryBy：{ sortby}\n Start: { start} \n Size:{ size}" +
                $"\n Translate:{translate}" +
                $"\n Selector:{string.Join(",", selector)}");
            return result;
        }
        protected override void DoPush(TAggregateRoot entity)
        {
            WriteContext.Push(entity);
        }

        protected override void DoUpdateAll(Expression<Func<TAggregateRoot, bool>> expression, TransferObject dto)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            var cmd = Builder.Translate(MetaData, expression);
            SqlUnit unit = new SqlUnit();
            unit.AddTable(string.Format("{0} as it", Wrapper(MetaData.TableName)));
            unit.AddTable(cmd.Tables.ToArray());
            var fields = new List<string>();
            foreach (var pair in dto)
            {
                var field = MetaData.GetField(pair.Key);
                if (field == null)
                    continue;
                fields.Add(string.Format("{0}=@{1}", Wrapper(field.GetFieldname()), cmd.CreateParameter(pair.Value, field.Name).Name));
            }

            cmd.BuildText(string.Format("update {0} set {1} where Id in (select it.Id from {3} where {2})", Wrapper(MetaData.TableName), string.Join(",", fields), cmd.CommandText, unit.StrTable));
            WriteContext.ExcuteTransaction(MetaData, cmd);

            sw.Stop();
            LogFile.Write("sqltrace", $"DoUpdateAll -- 耗时：{sw.ElapsedMilliseconds}ms " +
                $"\n 查询表名{typeof(TAggregateRoot).FullName} \n 查询条件:{expression}" +
                $"\n 查询参数：" +
                $"\n TransferObject:{string.Join(",", dto)}");
        }

        protected override void DoRemove(TAggregateRoot entity)
        {
            WriteContext.Remove(entity);
        }

        protected override void DoRemoveAll(Expression<Func<TAggregateRoot, bool>> expression)
        {
            var cmd = Builder.Translate(MetaData, expression);
            SqlUnit unit = new SqlUnit();
            unit.AddTable(string.Format("{0} as it", Wrapper(MetaData.TableName)));
            unit.AddTable(cmd.Tables.ToArray());
            cmd.BuildText(string.Format("delete from {0} where Id in (select it.Id from {1} where {2}) ", Wrapper(MetaData.TableName), unit.StrTable, cmd.CommandText));
            WriteContext.ExcuteTransaction(MetaData, cmd);
        }

        protected override TAggregateRoot DoFindById(Guid Id, string[] selector)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            var result = Execute(s => s.Id == Id, SortBy.Sortby_Id, 0, 1, selector).FirstOrDefault();

            sw.Stop();
            LogFile.Write("sqltrace", $"DoFindById  --耗时：{sw.ElapsedMilliseconds}ms " +
                $"\n 查询表名{typeof(TAggregateRoot).FullName} " +
                $"\n 查询参数：" +
                $"\n Id:{Id}" +
                $"\n Selector:{string.Join(",", selector)}");
            return result;
        }

        protected override TAggregateRoot DoFindFirst(Expression<Func<TAggregateRoot, bool>> expression, SortBy sortby, string[] selector)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            var result = Execute(expression, sortby, 0, 1, selector).FirstOrDefault();

            sw.Stop();
            LogFile.Write("sqltrace", $"DoFindFirst -- 耗时：{sw.ElapsedMilliseconds}ms " +
                $"\n 表名{typeof(TAggregateRoot).FullName} \n 条件:{expression}" +
                $"\n 查询参数：\n SoryBy：{ sortby}" +
                $"\n Selector:{string.Join(",", selector)}");
            return result;
        }


        protected override IEnumerable<TAggregateRoot> DoFindAll(Expression<Func<TAggregateRoot, bool>> expression, SortBy sortby, int start, int count, string[] selector)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            var result = Execute(expression, sortby, start, count, selector);

            sw.Stop();
            LogFile.Write("sqltrace", $"DoFindAll查询结束 --耗时：{sw.ElapsedMilliseconds}ms " +
                $"\n 查询结果数量:{result.Count()}" +
                $"\n 表名{typeof(TAggregateRoot).FullName} \n 条件:{expression}" +
                $"\n 参数：\n SoryBy：{ sortby}\n Start: { start} \n Count:{ count}" +
                $"\n Selector:{string.Join(",", selector)}" );
            return result;
        }

        protected override bool DoExists(Expression<Func<TAggregateRoot, bool>> expression)
        {
            return DoCount(expression, new string[0]) > 0;
        }

        protected override int DoCount(Expression<Func<TAggregateRoot, bool>> expression, string[] groupselector)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            var cmd = Builder.Translate(MetaData, expression);
            SqlUnit unit = new SqlUnit();
            unit.AddTable(string.Format("{0} as it", Wrapper(MetaData.TableName)));
            unit.AddTable(cmd.Tables.ToArray());
            if (groupselector.Length > 0)
            {

                var keys = new SqlUnit();
                foreach (var key in groupselector)
                {
                    var field = Table.GetField(key);
                    if (field != null)
                    {
                        var tunit = field.GetUint(false);
                        unit.AddTable(tunit.Tables.ToArray());
                        var join = tunit.Tables.Count() > 0;
                        foreach (var tf in tunit.Fields)
                        {
                            var index = tf.LastIndexOf(" as ", StringComparison.Ordinal);
                            if (index == -1)
                                keys.AddField(tf);
                            else if (join)
                                keys.AddField(tf.Substring(0, index));
                        }
                    }
                }
                string from = string.Format("select 1 as tmp from {0} where {1} group by {2}"
                , unit.StrTable, cmd.CommandText, keys.StrField);
                cmd.BuildText(string.Format("select count(1) as Count from ({0}) as g", from));
            }
            else
                cmd.BuildText(string.Format("select count({3} it.{0}) as Count from {1} where {2}", Wrapper("Id"), unit.StrTable, cmd.CommandText, cmd.Distinct ? "distinct" : ""));
            TransferObject record = ReadContext.ExecuteReaderRecord(MetaData, CommandType.Text, cmd).FirstOrDefault();
            var result = Convert.ToInt32(record.Values.FirstOrDefault());

            sw.Stop();
            LogFile.Write("sqltrace", $"DoCount --耗时：{sw.ElapsedMilliseconds}ms " +
                $"\n 查询结果数量:{result}" +
                $"\n 表名{typeof(TAggregateRoot).FullName} \n 条件:{expression}" +
                $"\n 查询参数:" +
                $"\n Selector:{string.Join(",", groupselector)}");
            return result;
        }

        protected override IDictionary<string, object> DoSum(Expression<Func<TAggregateRoot, bool>> expression, string[] selector)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            var cmd = Builder.Translate(MetaData, expression);

            cmd.BuildText(string.Format("select {0} from {1} where {2}",
                string.Join(",", selector.Select(s => string.Format("sum(it.{0}) as {0}", Wrapper(s))))
                , cmd.Tables.Count == 0 ? string.Format("{0} as it", Wrapper(MetaData.TableName)) : cmd.StrTables, cmd.CommandText));
            TransferObject record = ReadContext.ExecuteReaderRecord(MetaData, CommandType.Text, cmd).FirstOrDefault();
            var result = record.ToDict();

            sw.Stop();
            LogFile.Write("sqltrace", $"DoSum --耗时：{sw.ElapsedMilliseconds}ms " +
                $"\n 查询结果数量:{result.Count()}" +
                $"\n 表名{typeof(TAggregateRoot).FullName} \n 条件:{expression}" +
                $"\n 查询参数:" +
                $"\n Selector:{string.Join(",", selector)}");
            return result;
        }

        protected override IEnumerable<TransferObject> DoGroupBy(Expression<Func<TAggregateRoot, bool>> expression, IEnumerable<string> keySelector, IEnumerable<ResultSelector> resultSelector, SortBy sortby, int start, int count, bool translate)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            #region DoGroupBy方法
            var cmd = Builder.Translate(MetaData, expression);

            SqlUnit unit = new SqlUnit();
            unit.AddTable(string.Format("{0} as it", Wrapper(MetaData.TableName)));
            unit.AddTable(cmd.Tables.ToArray());
            var entityAgg = false; //包含子对象汇总
            var fieldInfos = new Dictionary<string, Tuple<FieldMetadata, string, IEnumerable<string>>>();

            Func<string, IEnumerable<string>> HandleSelector = null;

            HandleSelector = key =>
            {
                if (fieldInfos.ContainsKey(key))
                    return fieldInfos[key].Item3;
                var tmpfields = key.Split(new char[] { '.' }, 2);
                var prefix = "";
                var fieldmeta = MetaData.GetField(tmpfields[0]);
                if (tmpfields.Length == 2)
                    prefix = tmpfields[1];
                List<string> aliass = new List<string>();
                if (fieldmeta == null)
                {
                    var spec = Specification.Math(key);
                    if (spec != null)
                    {
                        var ralias = key;
                        foreach (var member in spec.Members)
                        {
                            var talias = HandleSelector(member);
                            if (talias.Count() > 0)
                                ralias = ralias.Replace(member, talias.FirstOrDefault());
                        }
                        aliass.Add(ralias);
                    }
                    return aliass;
                }

                if (fieldmeta.IsEntityCollection)
                {
                    var navfield = fieldmeta as NavigatField;

                    unit.AddTable(string.Format("left join {0} as {1} on it.Id = {1}.{2}", navfield.RelationModelMeta.TableName, fieldmeta.Name, navfield.RelationField));
                    unit.AddField(string.Format("{0} as {1}", key, key.Replace('.', '_')));
                    unit.AddField("it.Id");
                    aliass.Add(key.Replace('.', '_'));
                    entityAgg = true;
                }
                else
                {
                    var tunit = Table.GetUnit(translate, tmpfields.Length > 1 ? key : fieldmeta.Name);
                    unit.AddTable(tunit.Tables.ToArray());

                    foreach (var tf in tunit.Fields)
                    {
                        var index = tf.LastIndexOf(" as ");
                        var alias = "";
                        if (index > 0)
                            alias = tf.Substring(index + 4).Trim();
                        else
                            alias = tf.Replace("it.", "");

                        if (!string.IsNullOrEmpty(prefix) && index == -1)
                            continue;
                        aliass.Add(alias);
                        unit.AddField(tf);
                    }
                }
                fieldInfos[key] = new Tuple<FieldMetadata, string, IEnumerable<string>>(fieldmeta, prefix, aliass);
                return aliass;
            };
            var keyAlias = new List<string>();
            var resultAlias = new List<string>();
            var secondKeyAlias = new List<string>();
            var secondResultAlias = new List<string>();

            var needConvert = new Dictionary<string, ResultSelector>();
            foreach (var key in keySelector)
            {
                var ta = HandleSelector(key);
                keyAlias.AddRange(ta);
                secondKeyAlias.AddRange(ta);
            }
            foreach (var rgroup in resultSelector.GroupBy(s => s.Selector))
            {
                var alias = HandleSelector(rgroup.Key);

                FieldMetadata fieldmeta = null;
                if (fieldInfos.ContainsKey(rgroup.Key))
                    fieldmeta = fieldInfos[rgroup.Key].Item1;

                foreach (var rs in rgroup)
                {
                    if (alias.Count() == 0 && rs.Mode != ResultMode.Count)
                        continue;

                    if (rs.Mode == ResultMode.Self)
                    {
                        keyAlias.AddRange(alias);
                        secondKeyAlias.AddRange(alias);
                        if (alias.Count() > 1)
                        {
                            resultAlias.AddRange(alias);
                            needConvert[rs.Name] = rs;
                            continue;
                        }
                    }
                    var value = "";
                    var secondvlaue = "";
                    var sel = alias.FirstOrDefault();
                    switch (rs.Mode)
                    {
                        case ResultMode.Self:
                            value = sel;
                            secondvlaue = sel;
                            break;
                        case ResultMode.Sum:
                            if (entityAgg)
                            {
                                keyAlias.Add(sel);
                                value = fieldmeta.IsEntityCollection ? string.Format("sum({0})", sel) : sel;
                                secondvlaue = string.Format("sum({0})", rs.Name);
                            }
                            else
                                value = string.Format("sum({0})", sel);
                            break;
                        case ResultMode.Count:
                            if (entityAgg)
                            {
                                value = fieldmeta.IsEntityCollection ? "count(1)" : sel;
                                secondvlaue = fieldmeta.IsEntityCollection ? string.Format("sum({0})", rs.Name) : "count(1)";
                            }
                            else
                                value = "count(1)";
                            break;
                        case ResultMode.Avg: value = string.Format("avg({0})", sel); break;
                        case ResultMode.Max: value = string.Format("max({0})", sel); break;
                        case ResultMode.Min: value = string.Format("min({0})", sel); break;
                    }
                    resultAlias.Add(string.Format("{0} as {1}", value, rs.Name));
                    secondResultAlias.Add(string.Format("{0} as {1}", secondvlaue, rs.Name));
                }
            }
            if (entityAgg)
                keyAlias.Add("Id");
            var tmptable = string.Format("select {0} from {1} where {2} "
                , unit.StrField, unit.StrTable, cmd.CommandText);
            var tsql = string.Format("select {0} from ({1}) as tmptable group by {2}", string.Join(",", resultAlias), tmptable, string.Join(",", keyAlias.Distinct()));
            if (entityAgg)
                tsql = string.Format("select {0} from ({1}) as ttable group by {2}", string.Join(",", secondResultAlias), tsql, string.Join(",", secondKeyAlias.Distinct()));

            var tmpsortby = new SortBy();
            foreach (var pair in sortby)
            {
                if (needConvert.ContainsKey(pair.Key))
                    tmpsortby[needConvert[pair.Key].Selector] = pair.Value;
                else
                    tmpsortby[pair.Key] = pair.Value;
            }
            var rsql = GetQueryString(SqlUnit.CreateUnit(new[] { string.Format("({0}) as gpage", tsql) }, "*"), new QueryCommand("1=1"), new SqlSort(Table, tmpsortby, translate, "gpage"), start, count, true);
            cmd.BuildText(rsql);
            var recordss = ReadContext.ExecuteReaderRecord(MetaData, CommandType.Text, cmd);
            List<TransferObject> result = new List<TransferObject>();

            foreach (var record in recordss)
            {
                var tmpr = new TransferObject();
                foreach (var rs in resultSelector)
                {
                    if (fieldInfos.ContainsKey(rs.Selector))
                    {
                        var fieldmeta = fieldInfos[rs.Selector];
                        if (translate)
                        {
                            RecordTranslator.Translate(fieldmeta.Item1, fieldmeta.Item2, record, null);
                            var rname = rs.Selector.Replace(".", "_");
                            if (!record.ContainsKey(rname) && record.ContainsKey(fieldmeta.Item1.Name))
                                rname = fieldmeta.Item1.Name;
                            if (record.ContainsKey(rname))
                            {
                                tmpr[rs.Name] = record[rname];
                                if (fieldmeta.Item1.Field_Type == FieldType.many2one && string.IsNullOrEmpty(fieldmeta.Item2))
                                    tmpr[rs.Name + "__org__"] = record[rname + "__org__"];
                            }
                            else
                                tmpr[rs.Name] = record[rs.Name];
                        }
                        else
                        {
                            if (record.ContainsKey(rs.Name))
                                tmpr[rs.Name] = record[rs.Name];
                            else
                                tmpr[rs.Name] = record[string.Format("{0}{1}", string.IsNullOrEmpty(fieldmeta.Item2) ? "" : fieldmeta.Item2.Replace(".", "_") + "_", fieldmeta.Item1.GetFieldname())];

                        }
                    }
                    else
                        tmpr[rs.Name] = record[rs.Name];
                }
                result.Add(tmpr);
            }

            #region 
            //var values = new Dictionary<string, FieldMetadata>();
            //foreach (var rs in resultSelector)
            //{
            //    var field = Table.GetField(rs.Selector);
            //    values[rs.Name] = field == null ? null : field.FieldMeta;
            //    var sel = rs.Name;
            //    var alias = rs.Name;
            //    if (field != null)
            //    {
            //        var tunit = field.GetUint(translate);
            //        unit.AddTable(tunit.Tables.ToArray());
            //        sel = Wrapper(field.FieldMeta.GetFieldname());
            //        alias = field.Name;
            //        if (rs.Mode == ResultMode.Self)
            //        {
            //            if (keySelector.Contains(rs.Name))
            //                unit.AddField(tunit.Fields.ToArray());
            //            continue;
            //        }
            //    }

            //    var value = "";
            //    switch (rs.Mode)
            //    {
            //        case ResultMode.Sum: value = string.Format("sum({0})", sel); break;
            //        case ResultMode.Count: value = "count(1)"; break;
            //        case ResultMode.Avg: value = string.Format("avg({0})", sel); break;
            //        case ResultMode.Max: value = string.Format("max({0})", sel); break;
            //        case ResultMode.Min: value = string.Format("min({0})", sel); break;
            //    }

            //    unit.AddField(string.Format("{0} as {1}", value, alias));

            //}

            //var keys = new SqlUnit();
            //foreach (var key in keySelector)
            //{
            //    var field = Table.GetField(key);
            //    if (field != null)
            //    {
            //        var tunit = field.GetUint(translate);
            //        unit.AddTable(tunit.Tables.ToArray());
            //        var join = tunit.Tables.Count() > 0;
            //        foreach (var tf in tunit.Fields)
            //        {
            //            var index = tf.LastIndexOf(" as ");
            //            if (index == -1)
            //                keys.AddField(tf);
            //            else if (join)
            //                keys.AddField(tf.Substring(0, index));
            //        }
            //    }
            //}

            //string from = string.Format("select {0} from {1} where {2} group by {3}"
            //    , unit.StrField, unit.StrTable, cmd.CommandText, keys.StrField);

            //SortBy tmp = new SortBy();
            //foreach (var pair in sortby)
            //{
            //    if (values.ContainsKey(pair.Key))
            //    {
            //        var field = values[pair.Key];
            //        if (field != null)
            //            tmp[field.Name] = pair.Value;
            //        else
            //            tmp[pair.Key] = pair.Value;
            //    }
            //}

            //var sql = GetQueryString(SqlUnit.CreateUnit(new[] { string.Format("({0}) as gpage", from) }, "*"), new QueryCommand("1=1"), new SqlSort(Table, tmp, translate, "gpage"), start, count, true);

            //cmd.BuildText(sql);
            //var records = ReadContext.ExecuteReaderRecord(MetaData, CommandType.Text, cmd);
            //List<TransferObject> result = new List<TransferObject>();
            //foreach (var record in records)
            //{
            //    var tmpr = new TransferObject();
            //    foreach (var rs in values)
            //    {
            //        if (rs.Value == null)
            //            tmpr[rs.Key] = record[rs.Key];
            //        else
            //        {
            //            if (translate)
            //            {
            //                RecordTranslator.Translate(rs.Value, "", record, null);
            //                //ReadContext.ResolveField(rs.Value, record);
            //                tmpr[rs.Key] = record[rs.Value.Name];
            //                if (rs.Value.Field_Type == FieldType.many2one)
            //                    tmpr[rs.Key + "__org__"] = record[rs.Value.Name + "__org__"];
            //            }
            //            else
            //                tmpr[rs.Key] = record[rs.Value.GetFieldname()];
            //        }
            //    }
            //    result.Add(tmpr);
            //}
            #endregion
            #endregion

            sw.Stop();
            LogFile.Write("sqltrace", $"DoGroupBy --耗时：{sw.ElapsedMilliseconds}ms " +
                $"\n 表名{typeof(TAggregateRoot).FullName} \n 查询条件:{expression}" +
                $"\n 查询参数：\n SoryBy：{ sortby} \n Start: { start} \n Count:{ count}" +
                $"\n Translate:{translate}" +
                $"\n keySelector:{string.Join(",", keySelector)}" +
                $"\n resultSelector:{string.Join(",", resultSelector)}");
            return result;
        }
        #endregion

        #region schema

        protected virtual string GetDbFieldType(FieldType fieldType, int size) { return ""; }

        protected virtual string SqlGetDbColumns
        {
            get { return ""; }
        }

        /// <summary>
        /// 获取数据表中已经存在的列
        /// </summary>
        /// <param name="tablename">表名称</param>
        /// <returns></returns>
        protected Dictionary<string, DBColumn> GetDbColumns(string tablename)
        {
            Dictionary<string, DBColumn> fields = new Dictionary<string, DBColumn>();
            if (!string.IsNullOrEmpty(SqlGetDbColumns))
            {
                var param = new TranslatorParameterCollection(new TranslatorParameter[] { new TranslatorParameter("table", tablename) });
                foreach (var dto in ReadContext.ExecuteReaderRecord(MetaData, CommandType.Text, new QueryCommand(SqlGetDbColumns, param)))
                {
                    var dbfield = new DBColumn();
                    dbfield.Write(dto);
                    fields[dbfield.Name] = dbfield;
                }
            }
            return fields;
        }
        protected Dictionary<string, Dictionary<string, DBColumn>> DbColumns
        {
            get
            {
                return Feature.Cache.Thread<Dictionary<string, Dictionary<string, DBColumn>>>("owl.domain.sqlrepositoryprovider.dbcolumns", () => new Dictionary<string, Dictionary<string, DBColumn>>());
            }
        }
        protected Dictionary<string, DBColumn> GetDbColumnsCacheThread(string tablename)
        {
            if (!DbColumns.ContainsKey(tablename))
                DbColumns[tablename] = GetDbColumns(tablename);
            return DbColumns[tablename];
        }
        /// <summary>
        /// 获取数据库字段
        /// </summary>
        /// <param name="meta">字段元数据</param>
        /// <param name="needdefault">是否返回缺省值</param>
        /// <returns></returns>
        protected string GetDbFieldSql(FieldMetadata meta, bool needdefault = true)
        {
            var dbfield = DbField.Create(meta);
            if (dbfield == null)
                return null;
            return GetDbFieldSql(dbfield.Name, dbfield.FieldType, dbfield.Size, dbfield.Precision, dbfield.Required, needdefault);
        }

        protected override IEnumerable<string> DoGetColumns()
        {
            return GetDbColumns(MetaData.TableName).Keys;
        }
        /// <summary>
        /// 获取数据库字段
        /// </summary>
        /// <param name="name">字段名称</param>
        /// <param name="fieldtype">字段类型</param>
        /// <param name="size">大小</param>
        /// <param name="precision">精度</param>
        /// <param name="required">是否必须</param>
        /// <param name="needdefault">是否需要缺省值</param>
        /// <returns></returns>
        protected abstract string GetDbFieldSql(string name, FieldType fieldtype, int size, int precision, bool required, bool needdefault = true);

        protected virtual string GetDbFieldDefault(FieldType fieldtype) { return ""; }
        /// <summary>
        /// 创建表
        /// </summary>
        /// <param name="table">表名称</param>
        /// <param name="primary">主键</param>
        /// <param name="fields">字段集合</param>
        /// <param name="force">是否强制创建</param>
        /// <returns></returns>
        protected abstract string doCreateTable(string table, string primary, IEnumerable<string> fields, bool force);

        protected string CreateTable(string table, string primary, IEnumerable<string> fields, bool force)
        {
            if (fields == null)
                return "";
            return doCreateTable(table, primary, fields.Where(s => s != null), force);
        }
        /// <summary>
        /// 表结构
        /// </summary>
        /// <param name="table">表名称</param>
        /// <returns></returns>
        protected abstract string doDropTable(string table);

        protected virtual string RenameTable(string table, string oldname)
        {
            return string.Format("alter table {0} rename to {1}", Wrapper(oldname), Wrapper(table));
        }

        protected virtual string RenameColumn(string table, string fieldname, string oldfieldname)
        {
            return string.Format("alter table {0} rename column {1} to {2};", Wrapper(table), Wrapper(oldfieldname), Wrapper(fieldname));
        }

        protected string _DoAlterTable(string table, FieldMetadata field, AlterTable mode = AlterTable.AddColumn, string oldname = "")
        {
            switch (mode)
            {
                case AlterTable.AlterTable: return RenameTable(table, oldname);
                case AlterTable.AddColumn: return string.Format("alter table {0} add {1};", table, GetDbFieldSql(field, true));
                case AlterTable.DropColumn:
                    return string.Format("alter table {0} drop column {1};", table, Wrapper(field.GetFieldname()));
                case AlterTable.AlterColumn:
                    StringBuilder res = new StringBuilder();
                    if (field.GetFieldname() != oldname)
                    {
                        res.Append(RenameColumn(table, field.GetFieldname(), oldname));
                    }
                    res.AppendFormat("alter table {0} alter column {1};", table, GetDbFieldSql(field, false));
                    return res.ToString();
            }
            return "";
        }
        protected string DoAlterTable(string table, FieldMetadata field, AlterTable mode = AlterTable.AddColumn, string oldname = "")
        {
            if (field != null)
            {
                var dbcolumns = GetDbColumnsCacheThread(table);
                if (dbcolumns.ContainsKey(field.GetFieldname()))
                {
                    if (mode == AlterTable.AddColumn)
                        return "";
                    else if (mode == AlterTable.DropColumn || mode == AlterTable.AlterColumn)
                    {
                        var dbcolumn = dbcolumns[field.GetFieldname()];
                        StringBuilder sb = new StringBuilder();
                        string dvalue = GetDbFieldDefault(field.Field_Type);
                        if (!string.IsNullOrWhiteSpace(dbcolumn.Constraint) && (mode == AlterTable.DropColumn || !field.Required || dbcolumn.Default != dvalue))
                        {
                            sb.AppendFormat("ALTER TABLE {0} DROP constraint {1};", Wrapper(table), Wrapper(dbcolumn.Constraint));
                        }

                        if (mode == AlterTable.AlterColumn && field.Required && dbcolumn.Default != dvalue)
                        {
                            if (dbcolumn.IsNull)
                                sb.AppendFormat("update {0} set {1} = {2} where {1} is null;", Wrapper(table), Wrapper(dbcolumn.Name), dvalue);
                            sb.AppendFormat("alter table {0} add default ({1}) for {2} with values;", Wrapper(table), dvalue, Wrapper(dbcolumn.Name));
                        }
                        sb.Append(_DoAlterTable(table, field, mode, oldname));
                        return sb.ToString();
                    }
                }
            }
            //if (field != null && (mode == AlterTable.DropColumn || mode == AlterTable.AlterColumn))
            //{
            //    var dbcolumns = GetDbColumnsCacheThread(table);
            //    if (dbcolumns.ContainsKey(field.GetFieldname()))
            //    {
            //        var dbcolumn = dbcolumns[field.GetFieldname()];
            //        StringBuilder sb = new StringBuilder();
            //        string dvalue = GetDbFieldDefault(field.Field_Type);
            //        if (!string.IsNullOrWhiteSpace(dbcolumn.Constraint) && (mode == AlterTable.DropColumn || !field.Required || dbcolumn.Default != dvalue))
            //        {
            //            sb.AppendFormat("ALTER TABLE {0} DROP constraint {1};", Wrapper(table), Wrapper(dbcolumn.Constraint));
            //        }

            //        if (mode == AlterTable.AlterColumn && field.Required && dbcolumn.Default != dvalue)
            //        {
            //            if (dbcolumn.IsNull)
            //                sb.AppendFormat("update {0} set {1} = {2} where {1} is null;", Wrapper(table), Wrapper(dbcolumn.Name), dvalue);
            //            sb.AppendFormat("alter table {0} add default ({1}) for {2} with values;", Wrapper(table), dvalue, Wrapper(dbcolumn.Name));
            //        }
            //        sb.Append(_DoAlterTable(table, field, mode, oldname));
            //        return sb.ToString();
            //    }
            //}
            return _DoAlterTable(table, field, mode, oldname);
        }

        #region 创建结构
        protected QueryCommand createm2m(Many2ManyField field)
        {
            var pfield = field.Metadata.GetField(field.PrimaryField);
            var rfield = field.RelationModelMeta.GetField(field.RelationField) as Many2ManyField;
            string[] fields = new string[] {
                GetDbFieldSql(field.MiddleField,pfield.Field_Type,0,0,pfield.Required,false),
                GetDbFieldSql(rfield.MiddleField,pfield.Field_Type,0,0,pfield.Required,false)
            };
            return new QueryCommand(CreateTable(field.MiddleTable, "", fields, false));
        }

        List<QueryCommand> createschema(ModelMetadata metadata, bool force = false)
        {
            List<QueryCommand> cmds = new List<QueryCommand>();
            List<string> fields = new List<string>();
            foreach (var field in metadata.GetFields())
            {
                if (field.Field_Type == FieldType.one2many)
                {
                    var rfield = field as NavigatField;
                    if (rfield.RelationModelMeta.ObjType == DomainType.Entity)
                        cmds.AddRange(createschema(rfield.RelationModelMeta, force));
                }
                else if (field.Field_Type == FieldType.many2many)
                {
                    cmds.Add(createm2m(field as Many2ManyField));
                }
                else
                    fields.Add(GetDbFieldSql(field, true));
            }
            cmds.Add(new QueryCommand(CreateTable(metadata.TableName, metadata.PrimaryField == null ? "" : metadata.PrimaryField.Name, fields, force)));
            return cmds;
        }
        protected virtual bool IsFieldChange(DbField field, DBColumn column)
        {
            var size = field.Size;
            var dbfieldtype = GetDbFieldType(field.FieldType, size);
            if (dbfieldtype != column.Type || field.Required == column.IsNull)
                return true;

            switch (field.FieldType)
            {
                case FieldType.password:
                case FieldType.select:
                case FieldType.time:
                case FieldType.str:
                case FieldType.datemonth:
                case FieldType.text:
                case FieldType.richtext:
                case FieldType.file:
                case FieldType.image:
                    if (size > 0 && size < 1024 && column.Length > 0 && column.Length < 1024 && size != column.Length)
                        return true;
                    return false;
            }
            if (field.FieldType == FieldType.number && size == 128 && field.Precision != column.Scale)
                return true;
            return false;
        }


        protected List<QueryCommand> SyncSchema(ModelMetadata metadata)
        {
            List<QueryCommand> cmds = new List<QueryCommand>();
            var dbcolumns = GetDbColumnsCacheThread(metadata.TableName);
            if (dbcolumns.Count > 0) //返回字段数如果为0的话表示表不存在
            {
                foreach (var field in metadata.GetFields())
                {
                    if (field.Field_Type == FieldType.one2many)
                    {
                        var rfield = field as NavigatField;
                        if (rfield.RelationModelMeta.ObjType == DomainType.Entity)
                            cmds.AddRange(SyncSchema(rfield.RelationModelMeta));
                    }
                    else if (field.Field_Type != FieldType.many2many)
                    {
                        var dbfield = DbField.Create(field);
                        if (dbfield == null)
                            continue;
                        if (dbcolumns.ContainsKey(dbfield.Name))
                        {
                            var dbcolumn = dbcolumns[field.GetFieldname()];
                            if (IsFieldChange(dbfield, dbcolumn))
                                cmds.Add(new QueryCommand(DoAlterTable(metadata.TableName, field, AlterTable.AlterColumn, dbfield.Name)));
                        }
                        else
                            cmds.Add(new QueryCommand(DoAlterTable(metadata.TableName, field, AlterTable.AddColumn, dbfield.Name)));
                    }
                }
            }
            return cmds;
        }
        protected override void DoCreateSchema(bool force)
        {
            List<QueryCommand> cmds = new List<QueryCommand>();
            if (!force && !string.IsNullOrEmpty(SqlGetDbColumns))
            {
                try
                {
                    cmds.AddRange(SyncSchema(MetaData));
                }
                catch
                {
                }
            }
            cmds.AddRange(createschema(MetaData, force));
            foreach (var cmd in cmds)
            {
                this.WriteContext.ExcuteTransaction(MetaData, cmd);
            }
        }

        List<QueryCommand> fixschema(ModelMetadata metadata, string oldname)
        {
            var cmds = new List<QueryCommand>();
            cmds.Add(new QueryCommand(DoAlterTable(metadata.TableName, null, AlterTable.AlterTable, metadata.TableName.Replace(MetaData.TableName, oldname))));
            foreach (var field in metadata.GetFields<One2ManyField>(s => s.RelationModelMeta.ObjType == DomainType.Entity))
            {
                cmds.AddRange(fixschema(field.RelationModelMeta, oldname));
            }
            foreach (var field in metadata.GetFields<Many2ManyField>(s => s.MiddleTable.StartsWith(metadata.TableName)))
            {
                cmds.Add(new QueryCommand(DoAlterTable(field.MiddleTable, null, AlterTable.AlterTable, field.MiddleTable.Replace(MetaData.TableName, oldname))));
            }
            return cmds;
        }

        protected override void DoFixSchema(string oldname)
        {
            foreach (var cmd in fixschema(MetaData, oldname))
                WriteContext.ExcuteTransaction(MetaData, cmd);
        }

        #endregion

        #region 删除表结构
        List<QueryCommand> dropschema(ModelMetadata metadata)
        {
            List<QueryCommand> cmds = new List<QueryCommand>();
            foreach (var fied in metadata.GetFields())
            {
                if (fied.Field_Type == FieldType.one2many)
                {
                    var rfield = fied as NavigatField;
                    if (rfield.RelationModelMeta.ObjType == DomainType.Entity)
                        cmds.AddRange(dropschema(rfield.RelationModelMeta));
                }
                else if (fied.Field_Type == FieldType.many2many)
                {
                    var rfield = fied as Many2ManyField;
                    cmds.Add(new QueryCommand(doDropTable(rfield.MiddleTable)));
                }
            }
            cmds.Add(new QueryCommand(doDropTable(metadata.TableName)));
            return cmds;
        }

        protected override void DoDropSchema()
        {
            var cmds = dropschema(MetaData);
            foreach (var cmd in cmds)
                this.WriteContext.ExcuteTransaction(MetaData, cmd);
        }
        #endregion

        #region 变更
        List<QueryCommand> addcolumn(ModelMetadata metadata, string field)
        {
            List<QueryCommand> cmds = new List<QueryCommand>();
            if (!string.IsNullOrEmpty(field))
            {
                var mfield = field.Split('.')[0];
                if (metadata.ContainField(mfield))
                {
                    var tmp = metadata.GetField(mfield);
                    if (tmp.Field_Type == FieldType.one2many)
                    {
                        var nfield = tmp as One2ManyField;
                        if (nfield.RelationModelMeta.ObjType == DomainType.Entity)
                        {
                            if (field.Trim() == mfield.Trim())
                                cmds.AddRange(createschema(nfield.RelationModelMeta, true));
                            else
                                cmds.AddRange(addcolumn(nfield.RelationModelMeta, field.Substring(mfield.Length + 1)));
                        }
                    }
                    else if (tmp.Field_Type == FieldType.many2many)
                        cmds.Add(createm2m(tmp as Many2ManyField));
                    else
                    {
                        var dbcolumns = GetDbColumnsCacheThread(metadata.TableName);
                        if (dbcolumns.Count > 0 && !dbcolumns.ContainsKey(tmp.GetFieldname()))
                            cmds.Add(new QueryCommand(DoAlterTable(metadata.TableName, tmp)));
                    }
                }
            }
            return cmds;
        }

        protected override void DoAddColumn(string field)
        {
            foreach (var cmd in addcolumn(MetaData, field))
                WriteContext.ExcuteTransaction(MetaData, cmd);
        }

        List<QueryCommand> dropcolumn(ModelMetadata metadata, string field)
        {
            List<QueryCommand> cmds = new List<QueryCommand>();
            if (!string.IsNullOrEmpty(field))
            {
                var mfield = field.Split('.')[0];
                if (metadata.ContainField(mfield))
                {
                    var tmp = metadata.GetField(mfield);
                    if (tmp.Field_Type == FieldType.one2many)
                    {
                        var nfield = tmp as One2ManyField;
                        if (nfield.RelationModelMeta.ObjType == DomainType.Entity)
                        {
                            if (field.Trim() == mfield.Trim())
                                cmds.AddRange(dropschema(nfield.RelationModelMeta));
                            else
                                cmds.AddRange(dropcolumn(nfield.RelationModelMeta, field.Substring(mfield.Length + 1)));
                        }
                    }
                    else if (tmp.Field_Type != FieldType.many2many)
                        cmds.Add(new QueryCommand(DoAlterTable(metadata.TableName, tmp, AlterTable.DropColumn)));
                }
            }
            return cmds;
        }

        protected override void DoDropCoumn(string field)
        {
            foreach (var cmd in dropcolumn(MetaData, field))
                WriteContext.ExcuteTransaction(MetaData, cmd);
        }


        QueryCommand changecolumn(ModelMetadata metadata, string field, string newfield)
        {
            QueryCommand cmd = null;
            var mfield = newfield.Split('.')[0];
            if (metadata.ContainField(mfield))
            {
                var tmp = metadata.GetField(mfield);
                if (tmp.Field_Type == FieldType.many2many || tmp.Field_Type == FieldType.many2one)
                    return null;
                else if (tmp.Field_Type == FieldType.one2many)
                {
                    var nfield = tmp as One2ManyField;
                    if (nfield.RelationModelMeta.ObjType == DomainType.Entity && field.Trim() != mfield.Trim())
                        return changecolumn(nfield.RelationModelMeta, field, newfield.Substring(mfield.Length + 1));
                }
                else
                {
                    field = string.IsNullOrEmpty(field) ? newfield : field.Split('.').LastOrDefault();
                    cmd = new QueryCommand(DoAlterTable(metadata.TableName, tmp, AlterTable.AlterColumn, field));
                }
            }
            return cmd;
        }

        protected override void DoChangeColumn(string field, string newfield)
        {
            if (string.IsNullOrEmpty(newfield))
                return;
            var cmd = changecolumn(MetaData, field, newfield);
            if (cmd != null)
                WriteContext.ExcuteTransaction(MetaData, cmd);
        }

        #endregion

        #endregion

        #region 恢复
        protected override void DoRestore(IEnumerable<TAggregateRoot> roots)
        {
            List<string> cmds = new List<string>();
            foreach (var root in roots)
            {
                cmds.AddRange(ReadContext.getaddcmd(MetaData, root.Read(), true).Select(s => s.CommandText));
            }
            ReadContext.ExecuteNoTransaction(MetaData, new QueryCommand(string.Join(";", cmds)));
        }
        #endregion
    }

}
