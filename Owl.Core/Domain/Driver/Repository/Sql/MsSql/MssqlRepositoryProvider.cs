using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Util;

namespace Owl.Domain.Driver.Repository.Sql
{
    public class MssqlRepositoryProvider<TEntity> : SqlRepositoryProvider<TEntity>
        where TEntity : AggRoot
    {

        protected override string Wrapper(string name)
        {
            return string.Format("[{0}]", name);
        }

        protected override SqlTranslator Builder
        {
            get
            {
                return new MssqlTranslator();
            }
        }
        //SELECT sp.name AS [Name],SCHEMA_NAME(sp.schema_id) AS [Schema] FROM sys.all_objects AS sp LEFT OUTER JOIN sys.sql_modules AS smsp ON smsp.object_id = sp.object_id LEFT OUTER JOIN sys.system_sql_modules AS ssmsp ON ssmsp.object_id = sp.object_id WHERE (sp.type = N'P' OR sp.type = N'RF' OR sp.type='PC')and(CAST(case when sp.is_ms_shipped = 1 then 1 when (select major_id from     sys.extended_properties where major_id = sp.object_id and minor_id = 0 and class = 1 and name = N'microsoft_database_tools_support') is not null then 1 else 0 end AS bit)=0) ORDER BY [Schema] ASC,[Name] ASC

        protected override Type ContextType
        {
            get { return typeof(MssqlRepositoryContext); }
        }

        protected override string BuildM2OMultiple(Many2OneField field, string exp)
        {
            string relationfield = Wrapper(field.RelationField);
            if (field.PropertyType == typeof(string) && field.PropertyType != field.RelationFieldMeta.PropertyType)
            {
                relationfield = string.Format("CAST({0} as nvarchar(4000))", Wrapper(field.RelationField));
            }
            return string.Format("STUFF(REPLACE(REPLACE((SELECT {0} FROM {1} N where charindex(','+{3} +',',','+it.{2}+',') >0 and {4} FOR XML AUTO), '<N name=\"', ','), '\"/>', ''), 1, 1, '')", Wrapper(field.RelationDisField[0]), Wrapper(field.RelationModelMeta.TableName), Wrapper(field.GetFieldname()), relationfield, exp);
        }
        protected override string[] createTemp()
        {
            string tmp = string.Format("@{0}", Serial.GetRandom(4, false));
            string table = string.Format("declare {0} table({1} uniqueidentifier);", tmp, Wrapper("Id"));
            return new string[] { tmp, table };
        }
        protected override string GetQueryString(SqlUnit fields, QueryCommand where, SqlSort sortby, int start, int limit, bool childquery)
        {
            SqlUnit unit = sortby.GetUnit(childquery ? true : false);
            string orderbys = unit.StrOrder;
            var topstr = limit == 0 ? "" : string.Format("top {0}", start + limit);
            string whereexp = where.CommandText;
            var wheretables = where.Tables;
            if (start == 0 && (string.IsNullOrEmpty(orderbys) || (fields.Tables.Count() == 1 && wheretables.Count() == 0 && (limit == 0 || limit > 10000))))
            {
                fields.AddTable(wheretables.ToArray());
                fields.AddTable(unit.Tables.ToArray());
                return string.Format("select {4} {0} from {1} where {2} {3}", fields.StrField, fields.StrTable, whereexp, orderbys, topstr);
            }

            var whereunit = SqlUnit.CreateUnit(null);
            if (childquery)
                whereunit.AddTable(fields.Tables.ToArray());
            else
                whereunit.AddTable(DefaultTable);
            whereunit.AddTable(wheretables.ToArray());
            whereunit.AddTable(unit.Tables.ToArray());

            if (childquery)
                return string.Format("select * from (select {5} {0},row_number() over({3}) -1 as rowid from {1} where {2}) as page where page.rowid >= {4} order by page.rowid", fields.StrField, whereunit.StrTable, whereexp, orderbys, start, topstr);

            return string.Format("select {8} {0} from {1},(select {9} {5} row_number() over({3}) -1 as rowid,it.{6} from {7} where {2}) as page where it.{6} = page.{6} and page.rowid >= {4} order by page.rowid",
                fields.StrField, fields.StrTable, whereexp, orderbys, start, topstr, MetaData.PrimaryField.Name, whereunit.StrTable, fields.Distinct ? "distinct page.rowid," : "", where.Distinct ? "distinct" : "");
        }

        protected override string GetDbFieldType(FieldType fieldType, int size)
        {
            switch (fieldType)
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
                    return "nvarchar";
                case FieldType.binary:
                    return "varbinary";
                case FieldType.bollean:
                    return "bit";
                case FieldType.date:
                case FieldType.datetime:
                    return "datetime";
                case FieldType.digits:
                    switch (size)
                    {
                        case 8: return "tinyint";
                        case 16: return "smallint";
                        case 32: return "int";
                        case 64: return "bigint";
                        default: return "int";
                    }
                case FieldType.number:
                    switch (size)
                    {
                        case 32: return "float";
                        case 64: return "real";
                        case 128: return "decimal";
                        default: return "float";
                    }
                case FieldType.uniqueidentifier:
                    return "uniqueidentifier";
            }
            return "";
        }
        protected override string SqlGetDbColumns
        {
            get
            {
                return @"SELECT a.name Name,
(case when COLUMNPROPERTY( a.id,a.name,'IsIdentity')=1 then 'true' else 'false' end) [Identity], 
(case when (SELECT count(*) FROM sysobjects  
WHERE (name in (SELECT name FROM sysindexes  
WHERE (id = a.id) AND (indid in  
(SELECT indid FROM sysindexkeys  
WHERE (id = a.id) AND (colid in  
(SELECT colid FROM syscolumns WHERE (id = a.id) AND (name = a.name)))))))  
AND (xtype = 'PK'))>0 then 'true' else 'false' end) PK,b.name [Type],
COLUMNPROPERTY(a.id,a.name,'PRECISION') as [Length],  
isnull(COLUMNPROPERTY(a.id,a.name,'Scale'),0) as Scale,(case when a.isnullable=1 then 'true' else 'false' end) [IsNull],  
isnull(e.text,'') [Default],isnull(g.[value], ' ') AS [Memo],isnull(k.[name],' ') as [Constraint]
FROM  syscolumns a 
left join systypes b on a.xtype=b.xusertype  
inner join sysobjects d on a.id=d.id and d.xtype='U' and d.name<>'dtproperties' 
left join syscomments e on a.cdefault=e.id  
left join sys.extended_properties g on a.id=g.major_id AND a.colid=g.minor_id
left join sys.extended_properties f on d.id=f.class and f.minor_id=0
left join sysobjects as k on k.id = a.cdefault
where b.name is not null
and d.name=@table
order by a.id,a.colorder";
            }
        }
        protected override string GetDbFieldSql(string name, FieldType fieldtype, int size, int precision, bool required, bool needdefault = true)
        {
            string dbtype = "";
            string dvalue = "";
            switch (fieldtype)
            {
                case FieldType.password:
                case FieldType.select:
                case FieldType.time:
                case FieldType.str:
                case FieldType.datemonth:
                case FieldType.text:
                case FieldType.richtext:
                case FieldType.image:
                case FieldType.file:
                    dbtype = string.Format("[nvarchar]({0})", size > 512 ? "MAX" : size.ToString());
                    dvalue = "''";
                    break;
                case FieldType.binary:
                    dbtype = "[varbinary](MAX)";
                    break;
                case FieldType.bollean:
                    dbtype = "[bit]";
                    dvalue = "0"; break;
                case FieldType.date:
                case FieldType.datetime:
                    dbtype = "[datetime]";
                    dvalue = "getdate()";
                    break;
                case FieldType.digits:
                    switch (size)
                    {
                        case 8: dbtype = "[tinyint]"; break;
                        case 16: dbtype = "[smallint]"; break;
                        case 32: dbtype = "[int]"; break;
                        case 64: dbtype = "[bigint]"; break;
                    }
                    dvalue = "0"; break;
                case FieldType.number:
                    switch (size)
                    {
                        case 32: dbtype = "[float]"; break;
                        case 64: dbtype = "[real]"; break;
                        case 128: dbtype = string.Format("[decimal](18,{0})", precision); break;
                    }
                    dvalue = "0"; break;
                case FieldType.uniqueidentifier:
                    dbtype = "[uniqueidentifier]";
                    dvalue = "newid()";
                    break;
            }
            if (required && needdefault && dvalue != "")
                dvalue = string.Format("default {0}", dvalue);
            else
                dvalue = "";
            return string.Format("{0} {1} {2} {3}", Wrapper(name), dbtype, required ? "NOT NULL" : "", dvalue);
        }

        protected override string GetDbFieldDefault(FieldType fieldtype)
        {
            string dvalue = "";
            switch (fieldtype)
            {
                case FieldType.password:
                case FieldType.select:
                case FieldType.time:
                case FieldType.str:
                case FieldType.datemonth:
                case FieldType.text:
                case FieldType.richtext:
                case FieldType.image:
                case FieldType.file:
                    dvalue = "''";
                    break;
                case FieldType.binary:
                    break;
                case FieldType.bollean:
                case FieldType.digits:
                case FieldType.number:
                    dvalue = "0"; break;
                case FieldType.date:
                case FieldType.datetime:
                    dvalue = "getdate()";
                    break;
                case FieldType.uniqueidentifier:
                    dvalue = "newid()";
                    break;
            }
            return dvalue;
        }
        protected override string doCreateTable(string table, string primary, IEnumerable<string> fields, bool force)
        {
            StringBuilder sb = new StringBuilder();
            if (force)
                sb.AppendFormat("if object_id(N'{0}',N'U') is not null\n drop table {0} \n", table);
            else
                sb.AppendFormat("if object_id(N'{0}',N'U') is  null\n", table);
            sb.AppendFormat("CREATE TABLE [dbo].[{0}](", table);
            List<string> statements = new List<string>(fields);
            if (!string.IsNullOrEmpty(primary))
                statements.Add(string.Format("CONSTRAINT [pk_{0}] PRIMARY KEY NONCLUSTERED ([{1}] ASC)", table, primary));
            sb.Append(string.Join(",", statements));
            sb.AppendFormat(")");
            if (!string.IsNullOrEmpty(primary))
                sb.AppendFormat(" ON [PRIMARY]");
            return sb.ToString();
        }
        protected override string doDropTable(string table)
        {
            return string.Format("if object_id(N'{0}',N'U') is not null\n drop table [{0}] \n", table);
        }
        protected override string RenameTable(string table, string oldname)
        {
            return string.Format("exec sp_rename '{0}','{1}'", oldname, table);
        }
        protected override string RenameColumn(string table, string fieldname, string oldfieldname)
        {
            return string.Format("exec sp_rename '{0}.{1}','{2}','column'", table, oldfieldname, fieldname);
        }
    }
}
