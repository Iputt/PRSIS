using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Xml;
using System.IO;

namespace Owl.Util.iAppConfig
{

    #region 配置元素
    /// <summary>
    /// 配置元素注册器
    /// </summary>
    public class RegisterConfigElement : ConfigurationElement
    {
        [ConfigurationProperty("name", IsRequired = true)]
        public string Name
        {
            get { return (string)this["name"]; }
            set { this["name"] = value; }
        }

        [ConfigurationProperty("type", IsRequired = true)]
        protected string type
        {
            get { return (string)this["type"]; }
            set { this["type"] = value; }
        }

        Type m_type;
        /// <summary>
        /// 类型
        /// </summary>
        public Type Type
        {
            get
            {
                if (m_type == null)
                    m_type = Type.GetType(type);
                return m_type;
            }
        }

        public RegisterConfigElement() { }

        public RegisterConfigElement(XmlReader reader)
        {
            DeserializeElement(reader, false);
        }
    }

    /// <summary>
    /// 配置元素注册绑定
    /// </summary>
    public class BindConfigElement : ConfigurationElement
    {
        Dictionary<string, RegisterConfigElement> m_elements = new Dictionary<string, RegisterConfigElement>();
        protected override bool OnDeserializeUnrecognizedElement(string elementName, XmlReader reader)
        {
            if (elementName == "register")
            {
                var register = new RegisterConfigElement(reader); ;
                m_elements[register.Name] = register;
                return true;
            }
            return base.OnDeserializeUnrecognizedElement(elementName, reader);
        }
        /// <summary>
        /// 元素配置
        /// </summary>
        public Dictionary<string, RegisterConfigElement> Binders
        {
            get
            {
                return m_elements;
            }
        }
    }
    #endregion

    #region 类型映射
    public class MapRegisterElement : ConfigurationElement
    {
        /// <summary>
        /// 类别
        /// </summary>
        [ConfigurationProperty("category")]
        public string Category
        {
            get { return (string)this["category"]; }
            set { this["category"] = value; }
        }
        /// <summary>
        /// 名称 * 表示所有
        /// </summary>
        [ConfigurationProperty("name")]
        public string Name
        {
            get { return (string)this["name"]; }
            set { this["name"] = value; }
        }

        [ConfigurationProperty("type")]
        public string Type
        {
            get { return (string)this["type"]; }
            set { this["type"] = value; }
        }

        /// <summary>
        /// 类型
        /// </summary>
        [ConfigurationProperty("mapto", IsRequired = true)]
        public string MapTo
        {
            get { return (string)this["mapto"]; }
            set { this["mapto"] = value; }
        }


        public MapRegisterElement() { }

        public MapRegisterElement(XmlReader reader)
        {
            DeserializeElement(reader, false);
        }
    }
    public class MapElement : ConfigurationElement
    {
        Dictionary<string, Dictionary<string, string>> m_elements = new Dictionary<string, Dictionary<string, string>>();
        List<MapRegisterElement> elements = new List<MapRegisterElement>();
        protected override bool OnDeserializeUnrecognizedElement(string elementName, XmlReader reader)
        {
            if (elementName == "register")
            {
                var register = new MapRegisterElement(reader);
                if (!string.IsNullOrEmpty(register.Category))
                {
                    if (!m_elements.ContainsKey(register.Category))
                        m_elements[register.Category] = new Dictionary<string, string>();
                    m_elements[register.Category][register.Name ?? "*"] = register.MapTo;
                }
                else
                    elements.Add(register);
                return true;
            }
            return base.OnDeserializeUnrecognizedElement(elementName, reader);
        }

        /// <summary>
        /// 获取映射
        /// </summary>
        /// <param name="category"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public string GetMapTo(string category, string name)
        {
            if (m_elements.ContainsKey(category))
            {
                var tmp = m_elements[category];
                if (tmp.ContainsKey(name))
                    return tmp[name];
                else if (tmp.ContainsKey("*"))
                    return tmp["*"];
            }
            return "";
        }
        public IEnumerable<MapRegisterElement> GetMaps()
        {
            return elements;
        }
    }
    #endregion

    public class NameConfigElement : ConfigurationElement
    {
        [ConfigurationProperty("name", IsRequired = true)]
        public string Name
        {
            get { return (string)this["name"]; }
            set { this["name"] = value; }
        }
        public NameConfigElement() { }

        public NameConfigElement(XmlReader reader)
        {
            DeserializeElement(reader, false);
        }
    }

    /// <summary>
    /// 用户自定义配置基类
    /// </summary>
    public abstract class CustomeConfigElement : ConfigurationElement
    {
        public static CustomeConfigElement Create(Type type, XmlReader reader)
        {
            var config = Activator.CreateInstance(type) as CustomeConfigElement;
            config.DeserializeElement(reader, false);
            return config;
        }
    }

    public class OwlConfigSection : ConfigurationSection
    {
        public static OwlConfigSection Current = System.Configuration.ConfigurationManager.GetSection("owl") as OwlConfigSection;

        /// <summary>
        /// 元素容器
        /// </summary>
        [ConfigurationProperty("container")]
        protected BindConfigElement Container
        {
            get { return (BindConfigElement)this["container"]; }
            set { this["container"] = value; }
        }

        [ConfigurationProperty("map")]
        protected MapElement Maps
        {
            get { return (MapElement)this["map"]; }
            set { this["map"] = value; }
        }

        List<string> assemblies = new List<string>();
        List<string> namespaces = new List<string>();
        List<CustomeConfigElement> m_elements = new List<CustomeConfigElement>();

        protected override bool OnDeserializeUnrecognizedElement(string elementName, XmlReader reader)
        {
            if (elementName == "assembly")
            {
                assemblies.Add(new NameConfigElement(reader).Name);
                return true;
            }
            else if (elementName == "namespace")
            {
                namespaces.Add(new NameConfigElement(reader).Name);
                return true;
            }
            if (Container.Binders.ContainsKey(elementName))
            {
                m_elements.Add(CustomeConfigElement.Create(Container.Binders[elementName].Type, reader));
                return true;
            }

            return base.OnDeserializeUnrecognizedElement(elementName, reader);
        }
        /// <summary>
        /// 包含的程序集
        /// </summary>
        public IEnumerable<string> Assemblies
        {
            get { return assemblies.AsReadOnly(); }
        }

        public IEnumerable<string> Namespaces
        {
            get { return namespaces.AsReadOnly(); }
        }
        /// <summary>
        /// 获取相关元素的配置集合
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public IEnumerable<T> GetConfigs<T>()
            where T : CustomeConfigElement
        {
            return m_elements.OfType<T>();
        }

        /// <summary>
        /// 获取相关元素的配置
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetConfig<T>()
            where T : CustomeConfigElement
        {
            return m_elements.OfType<T>().FirstOrDefault();
        }

        string respath;

        /// <summary>
        /// 资源文件路径
        /// </summary>
        [ConfigurationProperty("respath")]
        public string ResPath
        {
            get
            {
                if (respath == null)
                    respath = (string)this["respath"];
                if (string.IsNullOrEmpty(respath))
                    respath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "add-ons");
                return respath;
            }
            set { this["respath"] = value; }
        }

        /// <summary>
        /// 是否调试
        /// </summary>
        [ConfigurationProperty("debug")]
        public bool Debug
        {
            get { return (bool)this["debug"]; }
            set { this["debug"] = value; }
        }
        /// <summary>
        /// 当前使用的版本
        /// </summary>
        [ConfigurationProperty("version")]
        public string Version
        {
            get { return (string)this["version"]; }
            set { this["version"] = value; }
        }

        /// <summary>
        /// 首页路径
        /// </summary>
        [ConfigurationProperty("homepage")]
        public string HomePage
        {
            get { return (string)this["homepage"]; }
            set { this["homepage"] = value; }
        }

        /// <summary>
        /// 是否多租户
        /// </summary>
        [ConfigurationProperty("multimandt")]
        public bool MultiMandt
        {
            get { return (bool)this["multimandt"]; }
            set { this["multimandt"] = value; }
        }

        /// <summary>
        /// 默认租户，为空时表示多租户
        /// </summary>
        [ConfigurationProperty("mandt")]
        public string Mandt
        {
            get { return ((string)this["mandt"]); }
            set { this["mandt"] = value; }
        }
        /// <summary>
        /// 多语言
        /// </summary>
        [ConfigurationProperty("multilanguage")]
        public bool MultiLanguage
        {
            get { return (bool)this["multilanguage"]; }
            set { this["multilanguage"] = value; }
        }
        /// <summary>
        /// 消息日志写入等级
        /// </summary>
        [ConfigurationProperty("msgloglevel")]
        public Feature.MsgLogType? MsgLogLevel
        {
            get { return (Feature.MsgLogType?)this["msgloglevel"]; }
            set { this["msgloglevel"] = value; }
        }

        /// <summary>
        /// 获取映射
        /// </summary>
        /// <param name="category">类别</param>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public string GetMap(string category, string name)
        {
            if (Maps != null)
                return Maps.GetMapTo(category, name);
            return "";
        }

        public IEnumerable<MapRegisterElement> GetMaps()
        {
            return Maps.GetMaps();
        }
    }
}
