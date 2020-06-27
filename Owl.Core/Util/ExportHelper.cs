using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Domain;

namespace Owl.Util
{
    public class ExportHelper
    {
        /// <summary>
        /// 导出csv
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="roots">导出的数据</param>
        /// <param name="fields">字段</param>
        /// <param name="converter">转换器</param>
        /// <returns>csv字符串</returns>
        public static string Export<T>(IEnumerable<T> roots, Dictionary<string, string> fields, Func<T, string, string> converter = null)
            where T : AggRoot
        {
            var meta = ModelMetadataEngine.GetModel(typeof(T));
            List<List<string>> body = new List<List<string>>();
            List<string> header = new List<string>();
            foreach (var key in fields.Keys)
            {
                var value = fields[key];
                if (string.IsNullOrEmpty(value) && meta.ContainField(key))
                    value = meta.GetField(key).Label;
                header.Add(value);
            }
            body.Add(header);
            foreach (var root in roots)
            {
                List<string> row = new List<string>();
                foreach (var key in fields.Keys)
                {
                    object value = null;
                    if (converter != null)
                        value = converter(root, key);
                    if (value == null)
                    {
                        value = root[key];
                        if (value is AggRoot)
                        {
                            var filed = meta.GetField(key) as Many2OneField;
                            value = ((AggRoot)value)[filed.RelationDisField[0]];
                        }
                    }
                    var str = value == null ? "" : value.ToString();
                    if (str.Contains(","))
                        str = str.Wrapper('"');
                    row.Add(str);
                }
                body.Add(row);
            }
            return string.Join("\r\n", body.Select(s => string.Join(",", s)));
        }
    }
}
