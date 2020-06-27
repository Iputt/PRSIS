using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Text.RegularExpressions;
using Owl.Domain;

namespace Owl.Util
{
    public static class StringHelper
    {
        /// <summary>
        /// 读取csv
        /// </summary>
        /// <param name="csv">csv文件</param>
        /// <param name="delemiter">分隔符</param>
        /// <returns></returns>
        public static List<string[]> SplitCSV(this string csv, char delemiter = ',')
        {
            if (string.IsNullOrEmpty(csv))
                return null;
            csv = csv.Trim();
            List<string[]> result = new List<string[]>();
            List<string> row = new List<string>();
            List<char> column = new List<char>();
            bool statement = false;
            for (int i = 0; i < csv.Length; i++)
            {
                char ch = csv[i];
                if (ch == delemiter)
                {
                    if (statement && i > 0 && csv[i - 1] == '"')
                    {
                        statement = false;
                        column.RemoveAt(column.Count - 1);
                    }

                    if (!statement)
                    {
                        row.Add(new string(column.ToArray()).Trim());
                        column = new List<char>();
                    }
                    else
                        column.Add(ch);

                    if (!statement && i < csv.Length - 1 && csv[i + 1] == '"')
                    {
                        statement = true;
                        i = i + 1;
                    }
                }
                else if (ch == '\r' || ch == '\n')
                {
                    if (statement && i > 0 && csv[i - 1] == '"')
                    {
                        statement = false;
                        column.RemoveAt(column.Count - 1);
                    }
                    if (!statement)
                    {
                        row.Add(new string(column.ToArray()).Trim());
                        result.Add(row.ToArray());
                        row = new List<string>();
                        column = new List<char>();
                    }

                    if (i < csv.Length - 1 && csv[i + 1] == '\n')
                        i = i + 1;

                    if (!statement && i < csv.Length - 1 && csv[i + 1] == '"')
                    {
                        statement = true;
                        i = i + 1;
                    }
                }
                else
                    column.Add(csv[i]);
            }

            row.Add(new string(column.ToArray()).Trim());
            result.Add(row.ToArray());
            return result;
        }

        /// <summary>
        /// 包装字符串
        /// </summary>
        /// <param name="str">待包装的字符串</param>
        /// <param name="start">包装头</param>
        /// <param name="end">包装尾</param>
        /// <returns></returns>
        public static string Wrapper(this string str, string start, string end = null)
        {
            return string.Format("{0}{1}{2}", start, str, end ?? start);
        }

        /// <summary>
        /// 包装字符串
        /// </summary>
        /// <param name="str">待包装的字符串</param>
        /// <param name="start">包装头</param>
        /// <param name="end">包装尾</param>
        /// <returns></returns>
        public static string Wrapper(this string str, char start, char end)
        {
            return string.Format("{0}{1}{2}", start, str, end);
        }
        /// <summary>
        /// 包装字符串
        /// </summary>
        /// <param name="str">待包装的字符串</param>
        /// <param name="wrapper">包装字符</param>
        /// <returns></returns>
        public static string Wrapper(this string str, char wrapper)
        {
            return Wrapper(str, wrapper, wrapper);
        }

        static Regex hanzi = new Regex("^.*[\u2E80-\u9FFF]+.*$");
        /// <summary>
        /// 将全角括号转为半角
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string ConvertHalf(this string str)
        {
            return str.Replace("（", "(").Replace("）", ")");
        }

        /// <summary>
        /// 合并运算，返回第一个不为空的值
        /// </summary>
        /// <param name="str1"></param>
        /// <param name="str2"></param>
        /// <returns></returns>
        public static string Coalesce(this string str1, string str2)
        {
            if (string.IsNullOrEmpty(str1))
                return str2;
            return str1;
        }

        /// <summary>
        /// 分离字符串并去除头尾的空白
        /// </summary>
        /// <param name="str"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        public static string[] TrySplit(this string str, char separator)
        {
            return string.IsNullOrEmpty(str) ? new string[0] : str.Split(separator).Select(s => s.Trim()).Where(s => s != "").ToArray();
        }
        static Regex Regex = new Regex(@"^(\w+)(\[(\d{1,3})\])");

        public static string Template(string template, TransferObject obj, string patternstart = "{{", string patternend = "}}")
        {
            return Template(template, (Object2)obj, patternstart, patternend);
        }
        public static string Template(string template, IDictionary<string, object> obj, string patternstart = "{{", string patternend = "}}")
        {
            return Template(template, new TransferObject(obj), patternstart, patternend);
        }
        public static string Template(string template, Object2 obj, string patternstart = "{{", string patternend = "}}")
        {
            var pattern = string.Format(@"{0}([\w|\[|\]]+){1}", patternstart, patternend);
            return Regex.Replace(template, pattern, s =>
            {
                var key = s.Groups[1].Value;
                if (obj != null)
                {
                    if (obj.ContainsKey(key))
                    {
                        if (obj[key] != null)
                            return obj[key].ToString();
                    }
                    else if (key.Contains("["))
                    {
                        var match = Regex.Match(key);
                        if (match.Success)
                        {
                            key = match.Groups[1].Value;
                            var index = int.Parse(match.Groups[3].Value);
                            if (obj.ContainsKey(key) && obj[key] is Array)
                            {
                                var tmp = obj[key] as Array;
                                if (tmp.Length > index)
                                    return tmp.GetValue(index).ToString();
                            }
                        }
                    }
                }
                return "";
            });
        }

        public static string ConvertPattern(string template, string destpatternstart, string destpatternend, DomainModel metadata = null, string orgpatternstart = "{{", string orgpatternend = "}}")
        {
            if (template.Contains(destpatternstart))
                return template;
            var pattern = string.Format(@"{0}(\w+){1}", orgpatternstart, orgpatternend);
            return Regex.Replace(template, pattern, s =>
            {
                var key = s.Groups[1].Value;
                if (metadata != null)
                {

                    if (metadata.ContainField(key))
                    {
                        var field = metadata.GetField(key);
                        if (field.Field_Type == FieldType.many2one || field.Field_Type == FieldType.select)
                            key = string.Format("{0}[1]", key);
                    }
                    else if (key.EndsWith("_value"))
                    {
                        key = string.Format("{0}[0]", key.Substring(0, key.Length - 6));
                    }
                }
                return string.Format("{0}{1}{2}", destpatternstart, key, destpatternend);
            });
        }

        static readonly Regex intpattern = new Regex(@"^\d*$");
        /// <summary>
        /// 判断字符串是否可转换为整数
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static bool IsDigits(string input)
        {
            return intpattern.IsMatch(input);
        }
        static readonly Regex floatpattern = new Regex(@"^\d+.?\d*$");
        /// <summary>
        /// 判断字符串是否可转换为数字
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static bool IsNumber(string input)
        {
            return floatpattern.IsMatch(input);
        }


        static Regex mathregex = new Regex(@"[+\-\?*/%]");
        /// <summary>
        /// 判断字符串是否为数学表达式
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static bool IsMath(string input)
        {
            return mathregex.IsMatch(input);
        }

        /// <summary>
        /// 判断字符串是否为Json
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static bool IsJson(this string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;
            if ((input.StartsWith("[") && input.EndsWith("]")) || (input.StartsWith("{") && input.EndsWith("}")))
                return true;
            return false;
        }

        public static string ToHex(string str)
        {
            byte[] b = Encoding.Default.GetBytes(str);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < b.Length; i++)
            {
                sb.Append(b[i].ToString("X2").ToUpper());
            }

            return sb.ToString();
        }

        public static string DeHex(string str)
        {
            try
            {
                byte[] bytes = new byte[str.Length / 2];
                for (int i = 0; i < str.Length / 2; i++)
                {
                    bytes[i] = Convert.ToByte(str.Substring(i * 2, 2), 16);
                }
                return Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                return "error";
            }
        }
    }
}
