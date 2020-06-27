using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Domain;
using System.Reflection;
using Owl.Feature.iImport;

namespace Owl.Util
{
    public abstract class ImportHandler
    {
        #region 静态方法
        static Dictionary<string, Type> handlertype = new Dictionary<string, Type>();
        static void loadfromasm(string asmname, Assembly asm)
        {
            foreach (var type in TypeHelper.LoadTypeFromAsm<ImportHandler>(asm))
            {
                if (type.BaseType.IsGenericType)
                {
                    string modelname = type.BaseType.GetGenericArguments()[0].MetaName();
                    handlertype[modelname] = type;
                }
            }
        }
        static void unloadasm(string asmname, Assembly asm)
        {
            foreach (var key in handlertype.Where(s => s.Value.Assembly.FullName == asm.FullName).Select(s => s.Key))
            {
                handlertype.Remove(key);
            }
        }
        static ImportHandler()
        {
            AsmHelper.RegisterResource(loadfromasm, unloadasm);
        }
        /// <summary>
        /// 根据对象名称获取导入处理器
        /// </summary>
        /// <param name="modelname"></param>
        /// <returns></returns>
        public static ImportHandler GetHandler(string modelname)
        {
            return GetHandler(ModelMetadataEngine.GetModel(modelname));
        }

        /// <summary>
        /// 根据对象元数据获取导入处理器
        /// </summary>
        /// <param name="meta"></param>
        /// <returns></returns>
        public static ImportHandler GetHandler(ModelMetadata meta)
        {
            Type type = null;
            if (handlertype.ContainsKey(meta.Name))
                type = handlertype[meta.Name];
            else
                type = typeof(CommonImportHandler);
            ImportHandler handler = Activator.CreateInstance(type) as ImportHandler;
            handler.Meta = meta;
            return handler;
        }
        /// <summary>
        /// 根据对象名称执行导入操作
        /// </summary>
        /// <param name="modelname"></param>
        /// <param name="file"></param>
        /// <param name="delimiter"></param>
        public static void Import(string modelname, string file, char delimiter)
        {
            Import(ModelMetadataEngine.GetModel(modelname), file, delimiter);
        }

        /// <summary>
        /// 执行导入操作
        /// </summary>
        /// <param name="meta">导入对象元数据</param>
        /// <param name="file">导入文件路径</param>
        /// <param name="delimiter">分隔符</param>
        public static void Import(ModelMetadata meta, string file, char delimiter)
        {
            var handler = GetHandler(meta);
            handler.Execute(file, delimiter);
        }

        #endregion

        protected ModelMetadata Meta { get; set; }

        #region 引用相关
        protected Dictionary<string, Dictionary<string, AggRoot>> relations = new Dictionary<string, Dictionary<string, AggRoot>>();
        protected Dictionary<string, List<string>> predisplay = new Dictionary<string, List<string>>();
        /// <summary>
        /// 获取关系数据
        /// </summary>
        /// <param name="field">字段</param>
        /// <returns></returns>
        protected Dictionary<string, AggRoot> GetRelation(Many2OneField field)
        {
            string key = field.Name;
            if (!relations.ContainsKey(key))
            {
                relations[key] = new Dictionary<string, AggRoot>();
                var display = new List<string>();
                var predis = new List<string>();
                predisplay[key] = predis;
                var skipspec = false;
                if (field.Specific != null)
                {
                    foreach (var context in field.Specific.Context)
                    {
                        if (context.Value is string)
                        {
                            var vfield = (string)context.Value;
                            if (vfield.StartsWith("@"))
                            {
                                skipspec = true;
                                var fmeta = Meta.GetField(vfield.Substring(1));
                                if (fmeta != null)
                                {
                                    display.Add(field.RelationModelMeta.GetField(context.Key).GetFieldname());
                                    predis.Add(fmeta.GetFieldname());
                                }
                            }
                        }
                    }
                }
                foreach (var root in Repository.FindAll(field.RelationModelMeta, skipspec ? null : field.FilterExp))
                {
                    var dk = string.Join("", display.Select(s => root[s]));

                    relations[key][dk + (string)root[field.RelationDisField.FirstOrDefault()]] = root;
                    if (field.RelationFieldType != typeof(Guid))
                        relations[key][dk + root[field.RelationField].ToString()] = root;
                }
            }
            return relations[key];
        }

        protected Dictionary<string, AggRoot> GetRelation(string field)
        {
            if (!Meta.ContainField(field))
                return null;
            var fieldmeta = Meta.GetField(field) as Many2OneField;
            if (fieldmeta == null)
                return null;
            return GetRelation(fieldmeta);
        }

        /// <summary>
        /// 获取关系数据的值
        /// </summary>
        /// <param name="field">关系字段</param>
        /// <param name="key"></param>
        /// <returns></returns>
        protected AggRoot GetValue(Many2OneField field, string key)
        {
            if (string.IsNullOrEmpty(key))
                return null;
            var dict = GetRelation(field);
            var diskey = string.Join("", predisplay[field.Name].Select(s => m_current[s])) + key;
            if (!dict.ContainsKey(diskey))
            {
                var root = BuildRelation(field, key);
                if (root != null)
                    dict[diskey] = root;
                return root;
            }
            return dict[diskey];
        }

        /// <summary>
        /// 构建引用的数据
        /// </summary>
        /// <param name="field"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        protected virtual AggRoot BuildRelation(Many2OneField field, string value)
        {
            return null;
        }
        #endregion

        #region 字段映射
        IDictionary<string, string> m_map;
        protected IDictionary<string, string> Map
        {
            get
            {
                if (m_map == null)
                    m_map = MapEngine.GetMap(Meta.Name, null);
                return m_map;
            }
        }

        /// <summary>
        /// 影射字段
        /// </summary>
        /// <param name="label"></param>
        /// <returns></returns>
        protected virtual string MapField(string label)
        {
            if (Map.ContainsKey(label))
                return Map[label];
            return null;
        }
        #endregion

        /// <summary>
        /// 预解析字段
        /// </summary>
        protected virtual HashSet<string> PreFileds { get { return null; } }

        /// <summary>
        /// 解析字段
        /// </summary>
        /// <param name="field">字段名称</param>
        /// <param name="label">标签名称</param>
        /// <param name="value">值</param>
        /// <returns></returns>
        protected virtual bool ParseField(string field, string value) { return false; }

        /// <summary>
        /// 根据标签解析
        /// </summary>
        /// <param name="label"></param>
        /// <param name="value"></param>
        protected virtual void ParseLabel(string label, string value) { }

        /// <summary>
        /// 文件加载完成之后
        /// </summary>
        /// <param name="records"></param>
        protected virtual void OnLoaded(List<Dictionary<string, string>> records) { }

        /// <summary>
        /// 过滤数据
        /// </summary>
        /// <param name="record"></param>
        /// <returns></returns>
        protected virtual bool IsValid(Dictionary<string, string> record) { return true; }

        /// <summary>
        /// 执行导入
        /// </summary>
        /// <param name="roots"></param>
        protected virtual void _Execute(List<Entity> roots)
        {
            foreach (var root in roots.OfType<AggRoot>())
                root.Push();
        }

        HashSet<string> prelabels;
        HashSet<string> PreLabels(IEnumerable<string> labels)
        {
            if (prelabels == null)
            {
                prelabels = new HashSet<string>();
                if (PreFileds != null && PreFileds.Count > 0)
                {
                    foreach (var field in PreFileds)
                    {
                        foreach (var label in labels)
                        {
                            var key = MapField(label);
                            if (key == field)
                                prelabels.Add(label);
                        }
                    }
                }
            }
            return prelabels;
        }

        protected Entity m_current;
        protected Dictionary<string, string> Record;

        /// <summary>
        /// 转换值为符合对象字段要求的值
        /// </summary>
        /// <param name="field"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        protected virtual string ConvertValue(string field, string value)
        {
            return value;
        }
        void Parse(string label, string value)
        {
            var key = MapField(label);
            value = ConvertValue(key, value);
            if (!string.IsNullOrEmpty(key) && !ParseField(key, value) && Meta.ContainField(key))
            {
                try
                {
                    var field = Meta.GetField(key);
                    if (field.Field_Type == FieldType.many2one)
                    {
                        var rfield = (Many2OneField)field;
                        var rvalue = GetValue(rfield, value);
                        if (rvalue != null)
                        {
                            if (rfield.Name != rfield.GetFieldname())
                                m_current[rfield.Name] = rvalue;
                            else
                                m_current[rfield.Name] = rvalue[rfield.RelationField];
                        }
                    }
                    else if (field.Field_Type == FieldType.select)
                    {
                        if (field.GetDomainField().Multiple)
                            m_current[key] = string.Join(",", value.Split(',').Select(s => ((SelectField)field).GetValue(s, "")));
                        else
                            m_current[key] = ((SelectField)field).GetValue(value, "");
                    }
                    else
                        m_current[key] = value;
                }
                catch (Exception ex)
                {
                    throw new AlertException("error.owl.util.import.value.novalid", "列 {0}，值:{1} 无效，请检查你导入的数据是否正常", label, value);
                }
            }
        }

        /// <summary>
        /// 创建当前聚合根
        /// </summary>
        /// <param name="record"></param>
        /// <returns></returns>
        protected virtual Entity CreateRoot(Dictionary<string, string> record)
        {
            var root = DomainFactory.Create<Entity>(Meta, true);
            root.Id = Guid.NewGuid();
            return root;
        }

        /// <summary>
        /// 传递到前端的返回结果
        /// </summary>
        public object Response { get; set; }
        /// <summary>
        /// 导入
        /// </summary>
        /// <param name="file"></param>
        /// <param name="delemiter"></param>
        /// <returns></returns>
        public IEnumerable<Entity> Execute(string file, char delemiter)
        {
            if (string.IsNullOrEmpty(file))
                throw new AlertException("error.owl.util.import.file.noselect", "没有选中任何文件，请刷新浏览器重试一下！");
            var paths = file.Split(',');
            List<Dictionary<string, string>> result = new List<Dictionary<string, string>>();

            #region 读取数据
            List<string> donefile = new List<string>();
            foreach (var path in paths)
            {
                if (string.IsNullOrEmpty(path) || donefile.Contains(path))
                    continue;
                donefile.Add(path);
                result.AddRange(FileHelper.Load(path, delemiter));
            }
            #endregion

            OnLoaded(result);

            #region 解析数据
            List<Entity> parseroots = new List<Entity>();
            foreach (var record in result)
            {
                if (!IsValid(record))
                    continue;
                try
                {
                    Record = record;
                    var root = CreateRoot(record);
                    if (root == null)
                        continue;
                    m_current = root;
                    var plabels = PreLabels(record.Keys);
                    foreach (var label in plabels)
                        Parse(label, record[label]);
                    foreach (var pair in record)
                    {
                        if (!plabels.Contains(pair.Key))
                            Parse(pair.Key, pair.Value);
                    }
                    foreach (var pair in record)
                        ParseLabel(pair.Key, pair.Value);
                    parseroots.Add(root);
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
            #endregion

            _Execute(parseroots);
            return parseroots;
        }
        /// <summary>
        /// 执行成功后
        /// </summary>
        /// <param name="roots"></param>
        public virtual void Successs(IEnumerable<AggRoot> roots) { }
    }

    public sealed class CommonImportHandler : ImportHandler
    {

    }

    public abstract class ImportHandler<T> : ImportHandler
        where T : AggRoot
    {
        /// <summary>
        /// 当前正在解析的对象
        /// </summary>
        protected T Current
        {
            get { return (T)m_current; }
        }


        protected sealed override void _Execute(List<Entity> roots)
        {
            _Execute(roots.Cast<T>());
        }

        public sealed override void Successs(IEnumerable<AggRoot> roots)
        {
            Success(roots.Cast<T>());
        }

        protected virtual void _Execute(IEnumerable<T> roots)
        {
            foreach (var root in roots)
                root.Push();
        }

        protected virtual void Success(IEnumerable<T> roots) { }

        protected override Entity CreateRoot(Dictionary<string, string> record)
        {
            return _CreateRoot(record);
        }

        protected virtual AggRoot _CreateRoot(Dictionary<string, string> record)
        {
            return base.CreateRoot(record) as AggRoot;
        }
    }

    public abstract class LineImportHandler<T> : ImportHandler
        where T : Entity
    {
        /// <summary>
        /// 当前正在解析的对象
        /// </summary>
        protected T Current
        {
            get { return (T)m_current; }
        }
        protected override void _Execute(List<Entity> roots)
        {

        }
    }
}
