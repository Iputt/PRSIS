using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace Owl.Util
{
    public static class XmlHelper
    {

        /// <summary>
        /// 对象串行化为XML格式字符串
        /// </summary>
        /// <param name="obj">需要串行化的对象</param>
        /// <returns>XML格式的字符串</returns>
        public static string Serialize(object obj)
        {
            XmlSerializer xs = new XmlSerializer(obj.GetType());
            using (MemoryStream stream = new MemoryStream())
            {
                xs.Serialize(stream, obj);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        public static T DeSerialize<T>(string xml)
        {
            var type = typeof(T);
            XmlSerializer xs = new XmlSerializer(typeof(T));
            using (StringReader sr = new StringReader(xml))
            {
                return (T)xs.Deserialize(sr);
            }
        }

        public static object DeSerialize(Type type, string xml)
        {
            XmlSerializer xs = new XmlSerializer(type);
            using (StringReader sr = new StringReader(xml))
            {
                return xs.Deserialize(sr);
            }
        }


        /// <summary>
        /// 将XML文档实例化
        /// </summary>
        /// <typeparam name="T">实例化的类型</typeparam>
        /// <param name="doc">需要实例化的xml文档</param>
        /// <returns></returns>
        public static T DeSerialize<T>(this XmlDocument doc)
        {
            return DeSerialize<T>(doc.OuterXml);
        }

        /// <summary>
        /// 格式化xml
        /// </summary>
        /// <param name="element"></param>
        /// <param name="padwidth">缩进宽度</param>
        /// <param name="current">当前宽度</param>
        /// <returns></returns>
        public static string FormatXml(XmlElement element, int padwidth = 2, int current = 0)
        {
            StringBuilder sb = new StringBuilder();
            var prepad = "".PadLeft(current);
            sb.AppendFormat("{0}<{1}", prepad, element.LocalName);
            foreach (XmlAttribute attr in element.Attributes)
            {
                sb.AppendFormat(" {0}=\"{1}\"", attr.LocalName, attr.Value);
            }
            if (element.HasChildNodes)
            {
                sb.Append(">\r\n");
                foreach (XmlElement child in element.ChildNodes)
                {
                    sb.Append(FormatXml(child, padwidth, current + padwidth));
                }
                sb.AppendFormat("{0}</{1}>", prepad, element.LocalName);
            }
            else if (!string.IsNullOrEmpty(element.Value))
                sb.AppendFormat(">{0}</{1}>", element.Value, element.LocalName);
            else
                sb.Append(" />");
            if (current > 0)
                sb.Append("\r\n");
            return sb.ToString();
        }

        /// <summary>
        /// 格式化xml
        /// </summary>
        /// <param name="element"></param>
        /// <param name="padwidth">缩进宽度</param>
        /// <param name="current">当前宽度</param>
        /// <returns></returns>
        public static string FormatXml(XElement element, int padwidth = 2, int current = 0)
        {
            StringBuilder sb = new StringBuilder();
            var prepad = "".PadLeft(current);
            sb.AppendFormat("{0}<{1}", prepad, element.Name.LocalName);
            foreach (var attr in element.Attributes())
            {
                sb.AppendFormat(" {0}=\"{1}\"", attr.Name.LocalName, attr.Value);
            }
            if (element.HasElements)
            {
                sb.Append(">\r\n");
                foreach (var child in element.Nodes())
                {
                    switch (child.NodeType)
                    {
                        case XmlNodeType.Text:
                            sb.Append(prepad.PadLeft(current + padwidth) + (child as XText).Value.Trim() + "\r\n");
                            break;
                        case XmlNodeType.Element:
                            sb.Append(FormatXml(child as XElement, padwidth, current + padwidth));
                            break;
                    }
                }
                sb.AppendFormat("{0}</{1}>", prepad, element.Name.LocalName);
            }
            else if (!string.IsNullOrEmpty(element.Value))
                sb.AppendFormat(">{0}</{1}>", element.Value, element.Name.LocalName);
            else
                sb.Append(" />");
            if (current > 0)
                sb.Append("\r\n");
            return sb.ToString();
        }
        /// <summary>
        /// 格式化xml
        /// </summary>
        /// <param name="xml"></param>
        /// <param name="padwidth">缩进宽度</param>
        /// <returns></returns>
        public static string FormatXml(string xml, int padwidth = 2)
        {
            return FormatXml(XElement.Parse(xml), padwidth);
        }
    }
}
