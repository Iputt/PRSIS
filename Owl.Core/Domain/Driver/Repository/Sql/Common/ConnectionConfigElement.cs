using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Util.iAppConfig;
using System.Xml;
using System.Configuration;
namespace Owl.Domain.Driver.Repository.Sql
{
    public class ConnectionRegisterElement : ConfigurationElement
    {
        /// <summary>
        /// 对象名称 * 表示所有
        /// </summary>
        [ConfigurationProperty("name")]
        public string Name
        {
            get { return (string)this["name"]; }
            set { this["name"] = value; }
        }
        /// <summary>
        /// 连接名称
        /// </summary>
        [ConfigurationProperty("connection")]
        public string Connection
        {
            get { return (string)this["connection"]; }
            set { this["connection"] = value; }
        }

        public ConnectionRegisterElement() { }

        public ConnectionRegisterElement(XmlReader reader)
        {
            DeserializeElement(reader, false);
        }
    }

    public class ConnectionConfigElement : CustomeConfigElement
    {

        List<ConnectionRegisterElement> elements = new List<ConnectionRegisterElement>();
        protected override bool OnDeserializeUnrecognizedElement(string elementName, XmlReader reader)
        {
            if (elementName == "register")
            {
                elements.Add(new ConnectionRegisterElement(reader));
                return true;
            }
            return base.OnDeserializeUnrecognizedElement(elementName, reader);
        }

        Dictionary<string, string> m_connections;
        protected Dictionary<string, string> Connections
        {
            get
            {
                if (m_connections == null)
                {
                    m_connections = new Dictionary<string, string>(elements.Count);
                    elements.ForEach(s => m_connections[s.Name] = s.Connection);
                }
                return m_connections;
            }
        }

        public string GetConnectionName(string model, string defaultname = "query")
        {
            if (Connections.ContainsKey(model))
                return Connections[model];
            if (Connections.ContainsKey("*"))
                return Connections["*"];
            return defaultname;
        }
        /// <summary>
        /// 获取链接字符串的名称
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        public string GetConnectionStringName(string prefix, string model, string defaultname = "query")
        {
            var name = GetConnectionName(model, defaultname);
            return string.Format("{0}{1}", prefix, name);
        }
    }
}
