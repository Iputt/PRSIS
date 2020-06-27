using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using System.Data;
using System.Collections;
using Owl.Util;
using Owl.Feature;
using System.Data.SqlClient;
using System.Data.SqlTypes;

namespace Owl.Domain.Driver.Repository.Sql
{
    public abstract class SqlRepositoryContext : RealRepositoryContext
    {

        #region 连接字符串
        /// <summary>
        /// 创建连接
        /// </summary>
        /// <param name="connectionstring">连接字符串</param>
        /// <returns></returns>
        protected abstract DbConnection CreateConnection(string connectionstring);

        /// <summary>
        /// 连接名称前缀
        /// </summary>
        protected abstract string PrefixName { get; }

        static ConnectionConfigElement Config = AppConfig.Section.GetConfig<ConnectionConfigElement>();
        static Dictionary<string, string> m_connectionname = new Dictionary<string, string>(100);
        /// <summary>
        /// 根据对象名获取连接名称
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        protected string GetConnectionName(string model)
        {
            if (m_connectionname.ContainsKey(model))
                return m_connectionname[model];

            var tmp = "main";
            if (Config != null)
                tmp = Config.GetConnectionName(model, tmp);
            var connectionname = string.Format("{0}{1}", PrefixName, tmp);
            m_connectionname[model] = connectionname;
            return connectionname;
        }

        /// <summary>
        /// 根据连接名称创建连接
        /// </summary>
        /// <param name="connectionname">链接名称</param>
        /// <param name="rwsplit">使用读写分离</param>
        /// <returns></returns>
        protected DbConnection CreateConnectionByName(string connectionname, bool rwsplit = false)
        {
            return CreateConnection(AppConfig.GetConnectionString(connectionname, rwsplit));
        }
        /// <summary>
        /// 根据对象名称创建连接
        /// </summary>
        /// <param name="modelname"></param>
        /// <param name="rwsplit">使用读写分离</param>
        /// <returns></returns>
        protected DbConnection CreateConnectionByModel(string modelname, bool rwsplit = false)
        {
            return CreateConnectionByName(GetConnectionName(modelname), rwsplit);
        }
        #endregion
        /// <summary>
        /// 包装列
        /// </summary>
        /// <param name="column"></param>
        /// <returns></returns>
        protected virtual string Wrapper(string column)
        {
            return string.Format("\"{0}\"", column);
        }

        #region 多对多

        static List<ManyToManyTable> getmanytomanytable(Entity entity)
        {
            List<ManyToManyTable> tables = new List<ManyToManyTable>();
            foreach (var field in entity.Metadata.GetFields(s => s.Field_Type == FieldType.many2many).Cast<Many2ManyField>())
            {
                var end = entity[field.Name];
                if (end != null)
                    tables.Add(((dynamic)end).GetManyToMany());
            }
            return tables;
        }

        List<ManyToManyTable> manytomanytables = new List<ManyToManyTable>();

        void handlemanytomanytable()
        {
            Dictionary<string, ManyToManyTable> tables = new Dictionary<string, ManyToManyTable>();
            foreach (var tgroup in manytomanytables.GroupBy(s => s.TableName))
            {
                ManyToManyTable table = new ManyToManyTable()
                {
                    TableName = tgroup.Key,
                    ForAdd = new List<ManyToManyPair>(),
                    ForRemove = new List<ManyToManyPair>()
                };
                tables[tgroup.Key] = table;
                foreach (var t in tgroup)
                {
                    foreach (var a in t.ForAdd)
                    {
                        if (!table.ForAdd.Any(s => s.Value1 == a.Value1 && s.Value2 == a.Value2))
                            table.ForAdd.Add(a);
                    }
                    foreach (var r in t.ForRemove)
                    {
                        if (!table.ForRemove.Any(s => s.Value1 == r.Value1 && s.Value2 == r.Value2))
                            table.ForRemove.Add(r);
                    }
                }
            }
            foreach (var key in tables.Keys)
            {
                foreach (var add in tables[key].ForAdd)
                {
                    ExcuteTransaction(key, new QueryCommand(string.Format("if not exists(select 1 from {0} where {1} = '{3}' and {2}='{4}') insert into {0} ({1},{2}) values ('{3}','{4}')", Wrapper(key), Wrapper(add.Key1), Wrapper(add.Key2), add.Value1, add.Value2)));
                }
                foreach (var remove in tables[key].ForRemove)
                {
                    ExcuteTransaction(key, new QueryCommand(string.Format("delete from {0} where {1}='{2}' and {3}='{4}'", Wrapper(key), Wrapper(remove.Key1), remove.Value1, Wrapper(remove.Key2), remove.Value2)));
                }
            }
        }

        #endregion

        #region RepositoryContext 接口实现

        string buildvalue(QueryCommand cmd, FieldMetadata meta, object value, bool plain)
        {
            if (value == null)
                return "null";
            if (!plain)
                return string.Format("@{0}", cmd.CreateParameter(value, meta.Name).Name);
            switch (meta.Field_Type)
            {
                case FieldType.date:
                    value = DTHelper.ToLocalTime((DateTime)value).ToString("yyyy-MM-dd");
                    break;
                case FieldType.datetime:
                    value = DTHelper.ToLocalTime((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss");
                    break;
                case FieldType.number:
                case FieldType.digits:
                    return value.ToString();
                case FieldType.str:
                case FieldType.text:
                case FieldType.richtext:
                case FieldType.password:
                    value = ((string)value).Replace("'", "''");
                    break;
                case FieldType.binary:
                    break;
            }
            return string.Format("N'{0}'", value);
        }

        /// <summary>
        /// 获取添加sql
        /// </summary>
        /// <param name="metadata"></param>
        /// <param name="record"></param>
        /// <param name="plain">是否不包含参数</param>
        /// <returns></returns>
        internal List<QueryCommand> getaddcmd(ModelMetadata metadata, TransferObject record, bool plain = false)
        {
            List<QueryCommand> cmds = new List<QueryCommand>();
            QueryCommand cmd = new QueryCommand();
            var navigat = new Dictionary<string, List<TransferObject>>();
            Dictionary<string, string> pairs = new Dictionary<string, string>();
            foreach (var field in metadata.GetFields())
            {
                var fieldname = field.GetFieldname();
                if (!record.ContainsKey(fieldname))
                    continue;
                var value = record[fieldname];
                if (field is ScalerField || field is Many2OneField)
                {
                    pairs[Wrapper(fieldname)] = buildvalue(cmd, field, value, plain);
                }
                else
                    navigat[fieldname] = value as List<TransferObject>;
            }


            cmd.BuildText(string.Format("insert into {0} ({1}) values({2})", Wrapper(metadata.TableName),
                string.Join(",", pairs.Keys),
                string.Join(",", pairs.Values))
            );
            cmds.Add(cmd);

            object primary = record["Id"];

            foreach (var rela in navigat)
            {
                NavigatField field = metadata.GetField(rela.Key) as NavigatField;
                var navtable = field.RelationModelMeta;
                foreach (var relar in rela.Value)
                {
                    relar[field.RelationField] = primary;
                    cmds.AddRange(getaddcmd(navtable, relar, plain));
                    relar.Remove(field.RelationField);
                }
            }
            return cmds;
        }

        protected override TransferObject executeAdd(AggRoot entity)
        {
            manytomanytables.AddRange(getmanytomanytable(entity));
            var record = entity.Read();
            List<QueryCommand> insert = getaddcmd(entity.Metadata, record);
            foreach (var cmd in insert)
                ExcuteTransaction(entity.Metadata, cmd);
            return record;
        }

        List<string> getremovecmd(Entity data, object foreignkey)
        {
            List<string> removes = new List<string>();
            ModelMetadata metadata = data.Metadata;
            object primary = data.Id;

            foreach (var field in metadata.GetEntityRelated())
            {
                foreach (Entity valueobject in (IEnumerable)data[field.Name])
                {
                    removes.AddRange(getremovecmd(valueobject, primary));
                }
            }

            removes.Add(string.Format("delete from {0} where {1}='{2}'", Wrapper(metadata.TableName), Wrapper("Id"), data.Id));
            return removes;
        }

        protected override void executeRemove(AggRoot entity)
        {
            manytomanytables.AddRange(getmanytomanytable(entity));
            List<string> removes = getremovecmd(entity, null);

            ExcuteTransaction(entity.Metadata, new QueryCommand(string.Join(";", removes)));
        }

        List<QueryCommand> getupdatecmd(ModelMetadata metadata, ModelChange change)
        {
            List<QueryCommand> cmds = new List<QueryCommand>();

            List<QueryCommand> removecmds = new List<QueryCommand>();
            if (change.Status == ChangeStatus.Update)
            {
                StringBuilder sb = new StringBuilder();
                QueryCommand cmd = new QueryCommand();
                sb.AppendFormat("update {0} set {1} where {2}='{3}'", Wrapper(metadata.TableName),
                                string.Join(",", change.Changes.Select(s => string.Format("{0}={1}", Wrapper(s.Key), s.Value == null ? "null" : (metadata.GetField(s.Key).IncUpate ? string.Format("{0} + @{1}", Wrapper(s.Key), cmd.CreateParameter(s.Value, s.Key).Name) : string.Format("@{0}", cmd.CreateParameter(s.Value,s.Key).Name))))),
                    Wrapper("Id"), change.Key
                );
                cmd.BuildText(sb.ToString());
                cmds.Add(cmd);
            }
            else if (change.Status == ChangeStatus.Insert)
            {
                //change.Changes[metadata.ForeignKey] = change.ForignKey;
                QueryCommand cmd = new QueryCommand();
                cmd.BuildText(string.Format("insert into {0} ({1}) values({2})", Wrapper(metadata.TableName),
                    string.Join(",", change.Changes.Keys.Select(s => Wrapper(s))),
                    string.Join(",", change.Changes.Select(s => s.Value == null ? "null" : string.Format("@{0}", cmd.CreateParameter(s.Value, s.Key).Name))))
                );
                cmds.Add(cmd);
            }
            else if (change.Status == ChangeStatus.Remove)
            {
                QueryCommand cmd = new QueryCommand(string.Format("delete from {0} where {1}='{2}'", Wrapper(metadata.TableName), Wrapper("Id"), change.Key));
                removecmds.Add(cmd);
            }
            foreach (var ckey in change.Children.Keys)
            {
                One2ManyField navfield = ((One2ManyField)metadata.GetField(ckey));
                var cmetadata = navfield.RelationModelMeta;
                foreach (var child in change.Children[ckey])
                {
                    child.Changes[navfield.RelationField] = child.ForignKey;
                    cmds.AddRange(getupdatecmd(cmetadata, child));
                }
            }
            removecmds.Reverse();
            cmds.AddRange(removecmds);
            return cmds;
        }

        protected override TransferObject executeUpdate(AggRoot entity)
        {
            manytomanytables.AddRange(getmanytomanytable(entity));
            ModelChange modelchanges = entity.BuildChanges();
            var res = modelchanges.ToDict();
            List<QueryCommand> cmds = getupdatecmd(entity.Metadata, modelchanges);
            foreach (var cmd in cmds)
                ExcuteTransaction(entity.Metadata, cmd);
            return res;
        }

        // Dictionary<DbConnection, DbTransaction> m_CT = new Dictionary<DbConnection, DbTransaction>();
        List<DbConnection> m_Connections = new List<DbConnection>();
        List<DbTransaction> m_Transactions = new List<DbTransaction>();
        protected override void _Execute(bool transaction)
        {
            handlemanytomanytable();
            foreach (var key in TransactionCmds.Keys)
            {
                DbConnection connection = CreateConnectionByName(key);
                DbTransaction dbtransaction = null;
                m_Connections.Add(connection);
                connection.Open();
                var cmds = TransactionCmds[key];
                if (transaction && cmds.Count > 1)
                {
                    dbtransaction = connection.BeginTransaction();
                    m_Transactions.Add(dbtransaction);
                    //m_CT[connection] = transaction;
                }
                foreach (var cmd in cmds)
                {
                    using (DbCommand command = CreateCommand(connection, cmd))
                    {
                        command.Transaction = dbtransaction;
                        try
                        {
                            command.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            throw new Exception(ex.Message, ex);
                        }

                    }
                }
            }
        }

        public override void Commit()
        {
            foreach (var transaction in m_Transactions)
                transaction.Commit();
        }

        public override void RollBack()
        {
            foreach (var transaction in m_Transactions)
                transaction.Rollback();
        }
        public override void Dispose()
        {
            foreach (var transaction in m_Transactions)
            {
                transaction.Dispose();
            }
            foreach (var connection in m_Connections)
            {
                connection.Close();
                connection.Dispose();
            }
        }
        #endregion


        private static DbCommand CreateCommand(DbConnection connection, QueryCommand command)
        {
            DbCommand cmd = connection.CreateCommand();
            cmd.CommandText = command.CommandText;
            cmd.CommandTimeout = 120;
            foreach (var param in command.Parameters)
            {
                DbParameter parameter = cmd.CreateParameter();
                parameter.ParameterName = param.Name;
                object value = param.Value;
                if (value == null)
                    value = Convert.DBNull;
                else if (value is Enum)
                {
                    value = EnumHelper.ToString((Enum)value);//.ToString();
                }
                else if (value is DateTime)
                {
                    parameter.DbType = DbType.DateTime;
                    try
                    {
                        var tmp = new SqlDateTime((DateTime)value);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception2("字段 {0} 日期 {1} 溢出,必须介于 1/1/1753 12:00:00 AM 和 12/31/9999 11:59:59 PM 之间",param.Alias, value);
                    }
                }

                parameter.Value = value;
                cmd.Parameters.Add(parameter);
            }
            return cmd;
        }
        #region 执行数据库操作

        Dictionary<string, List<TransferObject>> readrecord(ModelMetadata metadata, CommandType commandtype, QueryCommand command, List<string> models)
        {
            Dictionary<string, List<TransferObject>> allrecords = new Dictionary<string, List<TransferObject>>();
            #region 取数据
            DbConnection Connection = CreateConnectionByModel(metadata.Name, true);
            try
            {
                using (DbCommand cmd = CreateCommand(Connection, command))
                {
                    cmd.CommandType = commandtype;
                    Connection.Open();
                    using (DbDataReader reader = cmd.ExecuteReader())
                    {
                        string name = "";
                        object value = null;
                        int count = 0;
                        do
                        {
                            List<TransferObject> records = new List<TransferObject>();
                            allrecords[models[count]] = records;
                            while (reader.Read())
                            {
                                TransferObject record = new TransferObject();
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    name = reader.GetName(i);
                                    value = reader.GetValue(i);
                                    if (value == DBNull.Value)
                                        value = null;
                                    record[name] = value;
                                }
                                records.Add(record);
                            }
                            count += 1;
                        } while (reader.NextResult());
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
            finally
            {

                Connection.Close();
                Connection.Dispose();
            }

            #endregion
            return allrecords;
        }

        void parseentity(ModelMetadata metadata, Dictionary<Guid, Entity> entities, Dictionary<string, List<TransferObject>> allrecords)
        {
            foreach (var navfield in metadata.GetEntityRelated())
            {
                var navmeta = navfield.RelationModelMeta;
                if (allrecords.ContainsKey(navmeta.Name))
                {
                    var tentities = new Dictionary<Guid, Entity>();
                    foreach (var agroup in allrecords[navmeta.Name].GroupBy(s => (Guid)s[navfield.RelationField]))
                    {
                        if (entities.ContainsKey(agroup.Key))
                        {
                            var parent = entities[agroup.Key];
                            foreach (var record in agroup)
                            {
                                var entity = DomainFactory.Create<Entity>(navmeta);
                                entity.FromDb(record);
                                ((dynamic)parent[navfield.Name]).Add((dynamic)entity);
                                tentities[entity.Id] = entity;
                            }
                        }
                    }
                    allrecords.Remove(navmeta.Name);
                    parseentity(navmeta, tentities, allrecords);
                }
            }
            if (metadata.ObjType == DomainType.AggRoot)
            {
                foreach (var m2m in metadata.GetFields<Many2ManyField>())
                {
                    var mk = string.Format("{0}.{1}", metadata.Name, m2m.Name);
                    if (allrecords.ContainsKey(mk))
                    {
                        foreach (var mgroup in allrecords[mk].GroupBy(s => (Guid)s[m2m.MiddleField]))
                        {
                            if (entities.ContainsKey(mgroup.Key))
                            {
                                var root = entities[mgroup.Key] as AggRoot;
                                root.M2mKeys[m2m.Name] = new HashSet<Guid>(mgroup.Select(s => (Guid)s[m2m.TargetMiddleField]));
                            }
                        }
                    }
                }
            }
        }

        public IEnumerable<TEntity> ExecuteReader<TEntity>(ModelMetadata metadata, CommandType commandtype, QueryCommand command, List<string> models)
            where TEntity : AggRoot
        {
            var allrecords = readrecord(metadata, commandtype, command, models);
            var tmp = new Dictionary<Guid, Entity>();
            foreach (var record in allrecords[metadata.Name])
            {
                var entity = DomainFactory.Create<TEntity>(metadata);
                entity.FromDb(record);
                if (entity.Id == Guid.Empty)
                    entity.Id = Guid.NewGuid();
                tmp[entity.Id] = entity;
            }
            allrecords.Remove(metadata.Name);
            parseentity(metadata, tmp, allrecords);
            List<TEntity> results = new List<TEntity>();
            if (allrecords.ContainsKey("sorted"))
            {
                var sorted = allrecords["sorted"];
                allrecords.Remove("sorted");
                foreach (var keys in sorted)
                    results.Add((TEntity)tmp[(Guid)keys["Id"]]);
            }
            else
                results.AddRange(tmp.Values.Cast<TEntity>());
            return results;
        }
        string parseV(AggRoot root, string key)
        {
            var value = root[key];
            if (value is AggRoot)
            {
                var field = root.Metadata.GetField(key) as Many2OneField;
                List<string> vals = new List<string>();
                foreach (var dis in field.RelationDisField)
                {
                    vals.Add(parseV(value as AggRoot, dis));
                }
                return string.Join(",", vals);
            }
            return value == null ? "" : value.ToString();
        }
        void resolvetranslate(ModelMetadata metadata, string[] selectors, TransferObject record, TransferObject parent)
        {
            foreach (var field in metadata.GetFields())
            {
                if (selectors.Length == 0 || selectors.Contains(field.Name))
                    RecordTranslator.Translate(field, "", record, parent);
                if (field.Field_Type == FieldType.many2one && selectors.Length > 0 && selectors.Any(s => s.StartsWith(field.Name + ".")))
                {
                    foreach (var selector in selectors.Where(s => s.StartsWith(field.Name + ".")))
                    {
                        RecordTranslator.Translate(field, selector.Substring(field.Name.Length + 1), record, parent);
                    }
                }
            }
        }

        void parseentity(ModelMetadata metadata, IEnumerable<TransferObject> entities, Dictionary<string, List<TransferObject>> allrecords, bool translate = false)
        {
            foreach (var navfield in metadata.GetEntityRelated())
            {
                var navmeta = navfield.RelationModelMeta;
                if (allrecords.ContainsKey(navmeta.Name))
                {
                    var tentities = new List<TransferObject>(allrecords[navmeta.Name]);
                    var navs = allrecords[navmeta.Name].GroupBy(s => (Guid)s[navfield.RelationField]).ToDictionary(s => s.Key);
                    foreach (var parent in entities)
                    {
                        var objs = new List<TransferObject>();
                        var pkey = (Guid)parent["Id"];
                        if (navs.ContainsKey(pkey))
                        {
                            foreach (var record in navs[pkey])
                            {
                                objs.Add(record);
                                if (translate)
                                    resolvetranslate(navmeta, new string[0], record, parent);
                            }
                        }
                        parent[navfield.Name] = objs;
                    }
                    allrecords.Remove(navmeta.Name);
                    parseentity(navmeta, tentities, allrecords, translate);
                }
            }
            foreach (var navfield in metadata.GetFields<Many2ManyField>())
            {
                var navmetaname = string.Format("{0}.{1}", metadata.Name, navfield.Name);
                if (allrecords.ContainsKey(navmetaname))
                {
                    var tentities = new List<TransferObject>(allrecords[navmetaname]);
                    var navs = allrecords[navmetaname].GroupBy(s => (Guid)s[navfield.MiddleField]).ToDictionary(s => s.Key);
                    foreach (var parent in entities)
                    {
                        var pkey = (Guid)parent["Id"];
                        List<Guid> rids = new List<Guid>();
                        if (navs.ContainsKey(pkey))
                        {
                            foreach (var record in navs[pkey])
                            {
                                rids.Add(record.GetRealValue<Guid>(navfield.TargetMiddleField));
                            }
                        }
                        parent[navfield.Name] = rids.ToArray();
                    }
                    allrecords.Remove(navmetaname);
                }
            }
        }

        public IEnumerable<TransferObject> ExecuteReader2(ModelMetadata metadata, CommandType commandtype, QueryCommand command, List<string> models, string[] selectors, bool translate = false)
        {
            var allrecords = readrecord(metadata, commandtype, command, models);

            var tmp = new Dictionary<Guid, TransferObject>();
            //List<TransferObject> objs = new List<TransferObject>();
            foreach (var record in allrecords[metadata.Name])
            {
                tmp[record.GetRealValue<Guid>("Id")] = record;
                //objs.Add(record);
                if (translate)
                    resolvetranslate(metadata, selectors, record, null);
            }
            allrecords.Remove(metadata.Name);
            parseentity(metadata, tmp.Values, allrecords, translate);
            List<TransferObject> results = new List<TransferObject>();
            if (allrecords.ContainsKey("sorted"))
            {
                var sorted = allrecords["sorted"];
                allrecords.Remove("sorted");
                foreach (var keys in sorted)
                    results.Add(tmp[(Guid)keys["Id"]]);
            }
            else
                results.AddRange(tmp.Values);
            return results;
        }
        public IEnumerable<TransferObject>[] ExecuteReaderRecords(ModelMetadata metadata, CommandType commandtype, QueryCommand command)
        {
            DbConnection Connection = CreateConnectionByModel(metadata.Name, true);
            Connection.Open();
            try
            {
                List<List<TransferObject>> allrecords = new List<List<TransferObject>>();
                using (DbCommand cmd = CreateCommand(Connection, command))
                {
                    cmd.CommandType = commandtype;
                    using (DbDataReader reader = cmd.ExecuteReader())
                    {
                        string name = "";
                        object value = null;
                        int count = 0;
                        do
                        {
                            List<TransferObject> records = new List<TransferObject>();
                            allrecords.Add(records);
                            while (reader.Read())
                            {
                                TransferObject record = new TransferObject();
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    name = reader.GetName(i);
                                    value = reader.GetValue(i);
                                    if (value == DBNull.Value)
                                        value = null;
                                    record[name] = value;
                                }
                                records.Add(record);
                            }
                            count += 1;
                        } while (reader.NextResult());
                    }
                }
                return allrecords.ToArray();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
            finally
            {
                Connection.Close();
                Connection.Dispose();
            }
        }
        public IEnumerable<TransferObject> ExecuteReaderRecord(ModelMetadata metadata, CommandType commandtype, QueryCommand command)
        {
            DbConnection Connection = CreateConnectionByModel(metadata.Name, true);
            Connection.Open();
            try
            {
                List<TransferObject> records = new List<TransferObject>();
                using (DbCommand cmd = CreateCommand(Connection, command))
                {
                    cmd.CommandType = commandtype;
                    using (DbDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string name = "";
                            object value = null;
                            TransferObject record = new TransferObject();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                name = reader.GetName(i);
                                value = reader.GetValue(i);
                                if (value == DBNull.Value)
                                    value = null;
                                record[name] = value;
                            }
                            records.Add(record);
                        }
                    }
                }
                return records;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
            finally
            {
                Connection.Close();
                Connection.Dispose();
            }
        }
        //List<QueryCommand> cmds = new List<QueryCommand>();
        Dictionary<string, List<QueryCommand>> TransactionCmds = new Dictionary<string, List<QueryCommand>>();
        Dictionary<string, QueryCommand> CmdNoParameters = new Dictionary<string, QueryCommand>();
        protected void ExcuteTransaction(string model, QueryCommand command)
        {
            if (string.IsNullOrEmpty(command.CommandText))
                return;
            var cname = GetConnectionName(model);
            if (!TransactionCmds.ContainsKey(cname))
                TransactionCmds[cname] = new List<QueryCommand>();
            var tmpcmds = TransactionCmds[cname];
            if (!command.HasParameter)
            {
                if (!CmdNoParameters.ContainsKey(cname) || CmdNoParameters[cname].CommandLength > 1000000)
                {
                    CmdNoParameters[cname] = new QueryCommand();
                    tmpcmds.Add(CmdNoParameters[cname]);
                }
                CmdNoParameters[cname].AppendText(command.CommandText + ";");
            }
            else
                tmpcmds.Add(command);
        }

        public void ExcuteTransaction(ModelMetadata metadata, QueryCommand command)
        {
            ExcuteTransaction(metadata.Name, command);
        }

        public void ExecuteNoTransaction(ModelMetadata metadata, params QueryCommand[] cmds)
        {
            var connection = CreateConnectionByModel(metadata.Name);
            connection.Open();
            try
            {
                foreach (var cmd in cmds)
                {
                    using (var command = CreateCommand(connection, cmd))
                    {
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
            finally
            {
                connection.Close();
                connection.Dispose();
            }
        }

        #endregion

    }
}
