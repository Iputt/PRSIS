using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml;
using Owl.Domain;
using System.Reflection;
using System.Collections;
using System.IO;

namespace Owl.Util
{
    /// <summary>
    /// xml 元数据
    /// </summary>
    public abstract class XmlMeta : SmartObject
    {
        #region xmlmeta 注册
        static Dictionary<string, Type> types = new Dictionary<string, Type>(10);
        static void loadFromAsm(string name, Assembly asm)
        {
            foreach (var type in TypeHelper.LoadTypeFromAsm<XmlMeta>(asm))
            {
                types[type.Name.Replace("XmlMeta", "")] = type;
            }
        }

        static void unloadAsm(string name, Assembly asm) { }

        static XmlMeta()
        {
            AsmHelper.RegisterResource(loadFromAsm, unloadAsm);
        }

        #endregion

        /// <summary>
        /// 标识符
        /// </summary>
        public string Key { get; set; }

        string m_metatype;
        /// <summary>
        /// 元数类型
        /// </summary>
        [IgnoreField]
        public string MetaType
        {
            get
            {
                if (string.IsNullOrEmpty(m_metatype))
                    m_metatype = GetType().Name.Replace("XmlMeta", "");
                return m_metatype;
            }
            private set { m_metatype = value; }
        }

        HashSet<string> m_attrnames;
        [IgnoreField]
        protected HashSet<string> AttrNames
        {
            get
            {
                if (m_attrnames == null)
                    m_attrnames = new HashSet<string>() { "Name", "Model", "Type", "Ordinal" };
                return m_attrnames;
            }
        }

        protected virtual HashSet<string> ExtraAttr { get { return null; } }
        /// <summary>
        /// 解析Node
        /// </summary>
        /// <param name="node"></param>
        protected virtual void ParseNode(XmlNode node)
        {
            string value = "";
            switch (node.NodeType)
            {
                case XmlNodeType.Attribute: value = node.Value; break;
                case XmlNodeType.Element: value = node.InnerText; break;
            }
            this[node.LocalName] = value;
        }

        /// <summary>
        /// 从xml中解析
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        protected void ParseXml(XmlElement element)
        {
            foreach (XmlAttribute attr in element.Attributes)
                ParseNode(attr);

            foreach (XmlElement child in element.ChildNodes)
                ParseNode(child);
        }

        /// <summary>
        /// 转为xml node
        /// </summary>
        /// <param name="name">字段名称</param>
        /// <param name="value">值</param>
        /// <returns></returns>
        protected virtual XObject ToNode(string name, object value)
        {
            if (value == null)
                return null;
            if (value is IEnumerable && !(value is string))
            {
                return null;
            }
            if (AttrNames.Contains(name) || (ExtraAttr != null && ExtraAttr.Contains(name)))
                return new XAttribute(name, value);
            else
                return new XElement(name, value);
        }

        /// <summary>
        /// 转为XElement
        /// </summary>
        /// <returns></returns>
        public XElement ToElement()
        {
            List<XObject> objs = new List<XObject>();
            foreach (var key in Properties.Keys)
            {
                var value = GetValue(key);
                if (value == null)
                    continue;
                var obj = ToNode(key, value);
                if (obj != null)
                    objs.Add(obj);
            }
            return new XElement(MetaType, objs.ToArray());
        }
        public override string ToString()
        {
            return ToElement().ToString();
        }
        /// <summary>
        /// 转为经过转移的xml字符串
        /// </summary>
        /// <returns></returns>
        public string ToPlain()
        {
            var ele = new XElement("Meta", ToString());
            return ele.FirstNode.ToString();
        }

        /// <summary>
        /// 根据xml元素创建
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public static XmlMeta Create(XmlElement element)
        {
            if (!types.ContainsKey(element.LocalName))
                return null;
            XmlMeta meta = Activator.CreateInstance(types[element.LocalName]) as XmlMeta;
            meta.MetaType = element.LocalName;
            meta.ParseXml(element);
            return meta;
        }

        public static T Create<T>()
            where T : XmlMeta, new()
        {
            var t = new T();
            t.MetaType = typeof(T).Name.Replace("XmlMeta", "");
            return t;
        }

        /// <summary>
        /// 从程序集中加载元数据
        /// </summary>
        /// <param name="assembly"></param>
        /// <returns></returns>
        public static IEnumerable<XmlMeta> Load(Assembly assembly)
        {
            var objs = new List<XmlMeta>();
            foreach (var pair in assembly.LoadEmbed(filter: s => s.EndsWith(".meta.xml")))
            {
                using (var stream = new MemoryStream(pair.Value))
                {
                    var doc = new XmlDocument();
                    doc.Load(stream);
                    foreach (XmlElement child in doc.DocumentElement.ChildNodes)
                    {
                        var obj = XmlMeta.Create(child);
                        if (obj != null)
                            objs.Add(obj);
                    }
                }
            }
            return objs;
        }
    }
    /// <summary>
    /// xmlmeta 解析器
    /// </summary>
    public abstract class XmlMetaParser : AsmLoader<XmlMetaParser>
    {
        /// <summary>
        /// 元数据类型
        /// </summary>
        protected abstract string MetaType { get; }
        /// <summary>
        /// 执行解析
        /// </summary>
        /// <param name="metas"></param>
        protected abstract void Parse(IEnumerable<XmlMeta> metas);

        /// <summary>
        /// 获取指定对象的xml元数据
        /// </summary>
        /// <returns></returns>
        protected abstract IEnumerable<XmlMeta> ToMeta(IEnumerable<string> models);

        public static void ParseAll(IEnumerable<XmlMeta> metas)
        {
            var dmetas = metas.GroupBy(s => s.MetaType).ToDictionary(s => s.Key, s => s.ToList());
            foreach (var parser in Loaded)
            {
                if (dmetas.ContainsKey(parser.MetaType))
                {
                    parser.Parse(dmetas[parser.MetaType]);
                }
            }
        }

        public static IEnumerable<XmlMeta> ToAllMeta(IEnumerable<string> models)
        {
            List<XmlMeta> metas = new List<XmlMeta>();
            foreach (var parser in Loaded)
            {
                metas.AddRange(parser.ToMeta(models));
            }
            return metas;
        }

    }
}
