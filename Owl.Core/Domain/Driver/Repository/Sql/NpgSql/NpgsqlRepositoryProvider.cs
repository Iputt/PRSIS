using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Util;

namespace Owl.Domain.Driver.Repository.Sql
{
    internal class NpgsqlState
    {
        public static bool Inited = false;
    }
    public class NpgsqlRepositoryProvider<TEntity> : SqlRepositoryProvider<TEntity>
        where TEntity : AggRoot
    {
        protected override SqlTranslator Builder
        {
            get { return new NpgsqlTranslator(); }
        }
        protected override string Wrapper(string name)
        {
            return string.Format("\"{0}\"", name);
        }
        protected override Type ContextType
        {
            get { return typeof(NpgsqlRepositoryContext); }
        }
        protected override void onInit()
        {
            base.onInit();
            if (!NpgsqlState.Inited)
            {
                NpgsqlState.Inited = true;
                string cmd = @"CREATE OR REPLACE FUNCTION _concat(text, text)
RETURNS text AS $$
SELECT CASE
WHEN $2 IS NULL THEN $1
WHEN $1 IS NULL THEN $2
ELSE $1 operator(pg_catalog.||) ',' operator(pg_catalog.||) $2
END
$$ IMMUTABLE LANGUAGE SQL;

CREATE AGGREGATE sum (
BASETYPE = text,
SFUNC = _concat,
STYPE = text
);";
                try
                {
                    ReadContext.ExecuteNoTransaction(MetaData, new QueryCommand(cmd));
                }
                catch
                {
                }
            }
        }
        protected override string[] createTemp()
        {
            string tmp = Serial.GetRandom(4, false);
            string table = string.Format("create global temp table {0} (\"Id\" uuid) on commit delete rows;", tmp);
            return new string[] { tmp, table };
        }
        protected override string BuildM2OMultiple(Many2OneField field, string exp)
        {
            return string.Format("(select sum({0}) from {1} where position(concat(',',{3},',') in concat(',',it.{2},','))>0 and {4})",
                        Wrapper(field.RelationDisField[0]), Wrapper(field.RelationModelMeta.TableName), Wrapper(field.GetFieldname()), Wrapper(field.RelationField), exp);
        }
        protected override string BuildTop1(string exp)
        {
            return string.Format("{0} limit 1",exp);
        }
        //protected override string buildm2o(Many2OneField mfield)
        //{
        //    string dcon = "";
        //    if (mfield.FilterExp != null)
        //    {
        //        var cmd = Builder.Translate(mfield.RelationMetadata, mfield.FilterExp);
        //        if (cmd.Parameters.Count() > 0)
        //        {
        //            dcon = cmd.CommandText.Trim().Replace("it.", "");
        //            foreach (var par in cmd.Parameters)
        //            {
        //                if (par.Value != null)
        //                    dcon = dcon.Replace("@" + par.Name, "'" + par.Value.ToString() + "'");
        //            }
        //        }
        //    }
        //    if (string.IsNullOrEmpty(dcon))
        //        dcon = "1=1";
        //    var exp = mfield.FilterExp;
        //    if (mfield.RelationDisField.All(s => mfield.RelationMetadata.ContainField(s)))
        //    {
        //        string alias = Wrapper(mfield.Name + "Name");
        //        if (mfield.Multiple)
        //        {
        //            return string.Format("(select sum({0}) from {2} where position(concat(',',{4},',') in concat(',',it.{3},','))>0 and {5}) as {1}",
        //                Wrapper(mfield.RelationDisField[0]), alias, Wrapper(mfield.RelationMetadata.TableName), Wrapper(mfield.GetFieldname()), Wrapper(mfield.RelationField), dcon);
        //        }
        //        else
        //        {
        //            var relname = string.Join(",',',", mfield.RelationDisField.Select(s => Wrapper(s)));
        //            return string.Format("COALESCE((select concat({5}) from {0} where {0}.{2} = it.{1} and {4} order by \"Created\" desc offset 0 limit 1),'') as {3}",
        //                Wrapper(mfield.RelationMetadata.TableName), Wrapper(mfield.GetFieldname()), Wrapper(mfield.RelationField), alias, dcon, relname);
        //        }
        //    }
        //    return "";
        //}
        protected override string GetQueryString(string fields, string fromexp, string whereexp, SqlSort sortby, int start, int limit, bool child)
        {
            var unit = sortby.GetUnit(false);
            string orderbys = string.IsNullOrEmpty(unit.StrField) ? string.Empty : " order by " + unit.StrField;
            return string.Format("select {0} from {1} where {2} {3} limit {4} offset {5}", fields, fromexp, whereexp, orderbys, limit == 0 ? "All" : limit.ToString(), start);
        }

        protected override string getDbField(string name, FieldType fieldtype, int size, int precision, bool required, bool needdefault = true)
        {
            string dbtype = "";
            string _default = "";
            switch (fieldtype)
            {
                case FieldType.password:
                case FieldType.select:
                case FieldType.time:
                case FieldType.datemonth:
                case FieldType.str:
                case FieldType.text:
                case FieldType.richtext:
                    dbtype = size > 512 ? "character varying" : string.Format("character varying({0})", size);
                    _default = "''";
                    break;
                case FieldType.binary: dbtype = "bytea"; break;
                case FieldType.bollean: dbtype = "boolean"; _default = "false"; break;
                case FieldType.date: dbtype = "date"; break;
                case FieldType.datetime: dbtype = "timestamp without time zone"; break;
                case FieldType.digits:
                    switch (size)
                    {
                        case 8: dbtype = "tinyint"; break;
                        case 16: dbtype = "smallint"; break;
                        case 32: dbtype = "integer"; break;
                        case 64: dbtype = "bigint"; break;
                    }
                    _default = "0"; break;
                case FieldType.number:
                    switch (size)
                    {
                        case 32: dbtype = "real"; break;
                        case 64: dbtype = "double precision"; break;
                        case 128: dbtype = string.Format("numeric(18,{0})", precision); break;
                    }
                    _default = "0"; break;
                case FieldType.uniqueidentifier: dbtype = "uuid"; break;
            }
            if (required && needdefault && _default != "")
                _default = string.Format("default {0}", _default);
            else
                _default = "";
            return string.Format("{0} {1} {2} {3}", Wrapper(name), dbtype, required ? "NOT NULL" : "", _default);
        }

        /// <summary>
        /// 创建表
        /// </summary>
        /// <param name="table">表名称</param>
        /// <param name="primary">主键</param>
        /// <param name="fields">字段集合</param>
        /// <param name="force">是否强制创建</param>
        /// <returns></returns>
        protected override string doCreateTable(string table, string primary, IEnumerable<string> fields, bool force)
        {
            StringBuilder sb = new StringBuilder();
            if (force)
                sb.AppendFormat("DROP TABLE IF EXISTS {0};", Wrapper(table));
            sb.AppendFormat("CREATE TABLE IF NOT EXISTS {0}(", Wrapper(table));
            List<string> statements = new List<string>(fields);
            if (!string.IsNullOrEmpty(primary))
                statements.Add(string.Format("CONSTRAINT pk_{0} PRIMARY KEY ({1})", table, Wrapper(primary)));
            sb.Append(string.Join(",", statements));
            sb.AppendFormat(")WITH (OIDS=FALSE);ALTER TABLE {0} OWNER TO postgres;", Wrapper(table));
            return sb.ToString();
        }
        /// <summary>
        /// 删除表结构
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        protected override string doDropTable(string table)
        {
            return string.Format("DROP TABLE IF EXISTS {0};", Wrapper(table));
        }

        protected override string doAlterTable(string table, FieldMetadata field, AlterTable mode = AlterTable.AddColumn, string newfield = "")
        {
            return base.doAlterTable(Wrapper(table), field, mode, newfield);
        }
    }
}
