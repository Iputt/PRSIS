using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.IO;
using System.Globalization;
using System.Collections.Specialized;
using System.Xml.Serialization;
using System.Xml;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Collections;
using System.Reflection;
using System.Data;
using Owl.Feature;
using System.ComponentModel;
using Owl.Util;
namespace Owl.Util
{
    /// <summary>
    /// 数据转换类 用于补充System.Convert 功能
    /// </summary>
    public static class Convert2
    {
        /// <summary>
        /// 逢一进十
        /// </summary>
        /// <returns></returns>
        public static int Ceil(this decimal number)
        {
            return (int)decimal.Ceiling(number);
        }
        /// <summary>
        /// 四舍五入
        /// </summary>
        /// <param name="d"></param>
        /// <param name="decimals"></param>
        /// <returns></returns>
        public static decimal Rounding(this decimal d, int decimals)
        {
            return decimal.Round(d, decimals, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// 将字符串转为Decimal类型，如果转换失败则返回0.0m
        /// </summary>
        /// <param name="Source">需要转换的字符串</param>
        /// <returns>转换的结果</returns>
        public static decimal ToDecimal(string Source)
        {
            decimal dest = 0;
            decimal.TryParse(Source, out dest);
            return dest;
        }
        /// <summary>
        /// 将字符串转为Int类型，如果失败则返回0
        /// </summary>
        /// <param name="Source">需要转换的字符串</param>
        /// <returns>转换的结果</returns>
        public static int ToInt(string Source)
        {
            int dest = 0;
            int.TryParse(Source, out dest);
            return dest;
        }
        static CultureInfo enUSCulture = CultureInfo.CreateSpecificCulture("en-US");
        /// <summary>
        /// 将字符串转为时间类型，如果失败则返回当前日期
        /// </summary>
        /// <param name="Source">需要转换的字符串</param>
        /// <param name="Source">是否是英文风格的字符串，航信系统所用的方式</param>
        /// <returns>转换的结果</returns>
        public static DateTime ToDateTime(string Source, bool enUS)
        {
            DateTime datetime = DateTime.Now;
            if (!enUS)
                DateTime.TryParse(Source, out datetime);
            else
                DateTime.TryParse(Source, enUSCulture, DateTimeStyles.None, out datetime);
            return datetime;
        }
        /// <summary>
        /// 将中文格式字符串转为时间类型，如果失败则返回当前日期
        /// </summary>
        /// <param name="Source">需要转换的字符串</param>
        /// <returns>转换的结果</returns>
        public static DateTime ToDateTime(string Source)
        {
            return ToDateTime(Source, false);
        }


        static Regex jsondate = new Regex(@"^/(Date\(.*\))/$");
        public static T ChangeType<T>(object obj)
        {
            return (T)ChangeType(obj, typeof(T));
        }
        /// <summary>
        /// 将对象转换为指定类型
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="desttype"></param>
        /// <returns></returns>
        public static object ChangeType(object obj, Type desttype)
        {
            var destundertype = TypeHelper.StripType(desttype);
            bool isnullable = desttype != destundertype || desttype.Name == "String";
            if (obj == null || (desttype.Name != "String" && (obj is string && (string)obj == "")))
                return isnullable ? null : TypeHelper.Default(destundertype);
            var orgtype = obj.GetType();
            if (orgtype == destundertype || (destundertype.IsEnum && TypeHelper.IsDigit(orgtype)))
                return obj;
            if (desttype.Name == "DateTime" && TypeHelper.IsNumeric(orgtype))
            {
                obj = string.Format("({0})", obj.ToString().PadLeft(13, '0'));
                orgtype = typeof(string);
            }
            if (orgtype.Name == "String")
            {
                return TypeHelper.Parse(destundertype, (string)obj);
            }
            //整数转枚举
            if (desttype.IsEnum && TypeHelper.IsDigit(orgtype))
                return Enum.ToObject(desttype, obj);

            //判断是否为数值互转
            if (TypeHelper.IsNumeric(orgtype) && TypeHelper.IsNumeric(destundertype))
                return Convert.ChangeType(obj, destundertype);
            if (desttype.Name == "String")
                return obj.ToString();
            if (desttype.GetInterface("IEnumerable") != null && obj is IEnumerable)
            {
                var eletype = (desttype.GetElementType() ?? desttype.GetGenericArguments().FirstOrDefault()) ?? typeof(object);
                var arraylist = new ArrayList();
                foreach (var v in (IEnumerable)obj)
                {
                    arraylist.Add(ChangeType(v, eletype));
                }
                if (desttype.IsArray || desttype.Name == "IEnumerable`1")
                    return arraylist.ToArray(eletype);
                else
                    return Activator.CreateInstance(desttype, arraylist.ToArray(eletype));
            }

            throw new Exception2("进行转换的值与目标类型不匹配");
        }

        public static object ChangeType(ArrayList arraylist, Type desttype, Type elementtype)
        {
            if (elementtype == null)
                elementtype = TypeHelper.GetElementType(desttype);
            var array = arraylist.ToArray(elementtype);
            if (desttype.IsArray || desttype.Name == "IEnumerable`1")
                return array;
            return Activator.CreateInstance(desttype, array);
        }

        /// <summary>
        /// utf8转为unicode
        /// </summary>
        /// <param name="utf8str"></param>
        /// <returns></returns>
        public static string Utf8ToUnicode(string utf8str)
        {
            Encoding utf8 = Encoding.UTF8;
            Encoding defaultCode = Encoding.Unicode;
            // Convert the string into a byte[].
            byte[] utf8Bytes = utf8.GetBytes(utf8str);
            // Perform the conversion from one encoding to the other.
            byte[] defaultBytes = Encoding.Convert(utf8, defaultCode, utf8Bytes);
            char[] defaultChars = new char[defaultCode.GetCharCount(defaultBytes, 0, defaultBytes.Length)];
            defaultCode.GetChars(defaultBytes, 0, defaultBytes.Length, defaultChars, 0);
            return new string(defaultChars);
        }
    }
}

namespace System
{
    public static class Convertion
    {
        /// <summary>
        /// 去掉开头的0
        /// </summary>
        /// <param name="docno">需要执行的小数</param>
        /// <returns>得到的结果</returns>
        public static string DocConvertToDisplay(string docno)
        {
            if (string.IsNullOrEmpty(docno))
                return string.Empty;
            string arraylist = docno;
            int temp = 0;
            string str1 = "";
            for (int i = 0; i <= arraylist.Length - 1; i++)
            {
                if (arraylist[i].ToString() != "0")
                {
                    temp = i;
                    break;
                }
            }
            for (int i = temp; i <= arraylist.Length - 1; i++)
            {
                str1 += arraylist[i].ToString();
            }
            if (str1.Substring(str1.Length - 1) == ".")
            {
                str1 = str1.Substring(0, str1.Length - 1);
            }
            return str1;
        }

        /// <summary>
        /// 去掉末尾多余的0
        /// </summary>
        /// <param name="num">需要执行的小数</param>
        /// <returns>得到的结果</returns>
        public static string MengeConvertToDisplay(float num)
        {
            string arraylist = Convert.ToString(num);
            int temp = 0;
            string str1 = "";
            for (int i = arraylist.Length - 1; i >= 0; i--)
            {
                if (arraylist[i].ToString() != "0")
                {
                    temp = i;
                    break;
                }
            }
            for (int i = 0; i <= temp; i++)
            {
                str1 += arraylist[i].ToString();
            }
            if (str1.Substring(str1.Length - 1) == ".")
            {
                str1 = str1.Substring(0, str1.Length - 1);
            }
            return str1;
        }

        /// <summary>
        /// 去掉字符串末尾多余的0
        /// </summary>
        /// <param name="numstr">需要执行的字符串</param>
        /// <returns></returns>
        public static string MengeConvertToDisplay(string numstr)
        {
            if (string.IsNullOrEmpty(numstr))
                return string.Empty;
            string arraylist = numstr;
            int temp = 0;
            string str1 = "";
            for (int i = arraylist.Length - 1; i >= 0; i--)
            {
                if (arraylist[i].ToString() != "0")
                {
                    temp = i;
                    break;
                }
            }
            for (int i = 0; i <= temp; i++)
            {
                str1 += arraylist[i].ToString();
            }

            if (str1.Substring(str1.Length - 1) == ".")
            {
                str1 = str1.Substring(0, str1.Length - 1);
            }
            return str1;
        }


        public static List<string> GetPropertyList(object obj)
        {
            var propertyList = new List<string>();
            var properties = obj.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
            foreach (var property in properties)
            {
                object o = property.GetValue(obj, null);
                propertyList.Add(o == null ? "" : o.ToString());
            }
            return propertyList;
        }

        #region Datatable转List

        public static IList<T> ConvertTo<T>(DataTable table)
        {
            if (table == null)
            {
                return null;
            }

            List<DataRow> rows = new List<DataRow>();

            foreach (DataRow row in table.Rows)
            {
                rows.Add(row);
            }

            return ConvertTo<T>(rows);
        }

        public static IList<T> ConvertTo<T>(IList<DataRow> rows)
        {
            IList<T> list = null;

            if (rows != null)
            {
                list = new List<T>();
                foreach (DataRow row in rows)
                {
                    T item = CreateItem<T>(row);
                    list.Add(item);
                }
            }

            return list;
        }

        public static T CreateItem<T>(DataRow row)
        {
            T obj = default(T);
            if (row != null)
            {
                obj = Activator.CreateInstance<T>();

                foreach (DataColumn column in row.Table.Columns)
                {
                    PropertyInfo prop = obj.GetType().GetProperty(column.ColumnName);
                    try
                    {
                        object value = row[column.ColumnName];
                        prop.SetValue(obj, value, null);
                    }
                    catch
                    {
                        // You can log something here  
                        throw;
                    }
                }
            }

            return obj;
        }

        #endregion

        public static Guid ToGuid(this object obj)
        {
            if (obj == null || ((obj as string) == ""))
                return Guid.Empty;
            if (obj.ToString().Trim().Length != 36)
                return Guid.Empty;
            return new Guid(obj.ToString().Trim());
        }
        /// <summary>
        /// 取 数组 的文本值，并去除前面的value值，只取 text 值
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string ToText(this object obj)
        {
            string txt = obj.ToString2(true);
            if (txt.Contains(' '))
            {
                string[] result = txt.Split(' ');
                txt = String.Empty;
                //多空格分割字符串处理
                for (int i = 1; i < result.Length; i++)
                {
                    txt = txt + " " + result[i];
                }
                txt.Trim();
            }
            return txt;
        }

        /// <summary>
        /// 对数据传输对象进行处理，去除某些字符前缀
        /// </summary>
        /// <param name="to">当前对象</param>
        /// <param name="dealKeyList">需要处理的字段队列，默认为空</param>
        /// <returns></returns>
        public static TransferObject MFConvert(this TransferObject to, List<string> dealKeyList = null)
        {
            //如果指定了需要处理的字段队列，只针对指定字段进行处理
            if (dealKeyList != null)
            {
                foreach (var key in dealKeyList)
                {
                    //去除多余空白，防止多空格导致无法查找到数据
                    string k = key.Trim();
                    if (to[k] != null)
                    {
                        (to[k] as object[])[1] = to[k].ToText();
                    }
                }
            }
            //未指定字段，就截取所有带前缀的
            else
            {
                //遍历每一个索引
                foreach (var key in to.Keys)
                {
                    //跳过审批/跳过null值，否者会报错
                    if (!key.Contains("WfInstance") && to[key] != null)
                    {
                        //检查当前值的类型，里面有两个值的时候进行处理
                        if (to[key].GetType().ToString2() == "System.Object[]")
                        {
                            var data = to[key] as object[];
                            string s1 = data[0].ToString2();
                            string s2 = data[1].ToString2();

                            //检查是否为空，长字符串是否包含子字符串
                            if (s1 != s2 && s1 != String.Empty && s2.Contains(s1))
                            {
                                //去除前缀
                                (to[key] as object[])[1] = s2.ToText();
                            }
                        }
                    }
                }
            }
            return to;
        }
        /// <summary>
        /// 当值为NULL时返回string.Empty，传入的如果是数组,则根据 DisplayValue判断返回显示的值，还是返回真实值，默认false返回真实值
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string ToString2(this object obj, bool DisplayValue = false)
        {
            if (obj == null || ((obj as string) == ""))
                return string.Empty;
            if (obj is Array)
            {
                Object[] arr = (Object[])obj;
                if (DisplayValue && arr.Length > 1)
                {
                    if (arr[1] != null)
                        return arr[1].ToString().Trim();
                    else
                        return string.Empty;
                }
                else
                {
                    if (arr[0] != null)
                        return arr[0].ToString().Trim();
                    else
                        return string.Empty;
                }
            }
            return obj.ToString().Trim();
        }
        /// <summary>
        /// 转换为想要的类型，如果传入的值为NULL或空，则返回默认值
        /// </summary>
        /// <typeparam name="T">值的类型</typeparam>
        /// <param name="obj">值</param>
        /// <param name="_default">缺省值</param>
        /// <returns></returns>
        public static T ToString2<T>(this object obj, T _default = default(T))
        {
            if (obj == null || ((obj as string) == ""))
                return _default;
            if (obj is T)
                return (T)obj;
            return Convert2.ChangeType<T>(obj);
        }
        /// <summary>
        /// 参数值必须可转换为数字类型才可使用该方法，若为null则返回0，否则将抛出异常
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        public static string ToString2(this object obj, string format)
        {
            if (obj == null || ((obj as string) == ""))
                obj = 0;
            if (format.StartsWith("D"))
                return int.Parse(obj.ToString().Trim()).ToString(format);
            return decimal.Parse(obj.ToString().Trim()).ToString(format);
        }

        #region 一些公用日期的转换
        /// <summary>
        /// 将日期根据季节返回  年份加Q1 Q2 Q3 Q4
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string GetSeason(this DateTime obj)
        {
            switch (obj.Month)
            {
                case 1:
                case 2:
                case 3:
                    return obj.Year.ToString() + "Q1";
                case 4:
                case 5:
                case 6:
                    return obj.Year.ToString() + "Q2";
                case 7:
                case 8:
                case 9:
                    return obj.Year.ToString() + "Q3";
                case 10:
                case 11:
                case 12:
                    return obj.Year.ToString() + "Q4";
            }
            return obj.ToString("yyyyMM");
        }
        /// <summary>
        /// 根据日期获取星期
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string ToWeek(this DateTime obj)
        {
            switch (obj.DayOfWeek.ToString2())
            {
                case "Sunday":
                    return Translation.Get("fieldlabel.common.sunday", "星期日", true);
                case "Monday":
                    return Translation.Get("fieldlabel.common.monday", "星期一", true);
                case "Tuesday":
                    return Translation.Get("fieldlabel.common.tuesday", "星期二", true);
                case "Wednesday":
                    return Translation.Get("fieldlabel.common.wednesday", "星期三", true);
                case "Thursday":
                    return Translation.Get("fieldlabel.common.thursday", "星期四", true);
                case "Friday":
                    return Translation.Get("fieldlabel.common.friday", "星期五", true);
                case "Saturday":
                    return Translation.Get("fieldlabel.common.saturday", "星期六", true);
            }
            return "error";
        }
        /// <summary>
        /// 将符合时间的格式转为yyyyMMdd HH:mm
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string ToyyyyMMdd_HHmm(this object obj)
        {
            return obj.ToDateTime("yyyyMMdd HH:mm");
        }
        /// <summary>
        /// 转换为 yyyyMMdd格式
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string ToyyyyMMdd(this object obj)
        {
            return obj.ToDateTime("yyyyMMdd");
        }
        /// <summary>
        /// 任何符合日期格式的对象均可转换
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        public static string ToDateTime(this object obj, string format)
        {
            DateTime date;
            if (obj is DateTime)
                date = (DateTime)obj;
            else
                date = obj.ToDateTime();
            return date.ToString(format);
        }
        /// <summary>
        /// 转换为DateTime格式，如果转换错误，则抛出异常
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static DateTime ToDateTime(this object obj)
        {
            string str = obj.ToString();
            str = str.Replace("-", "").Replace("/", "").Replace(".", "").Replace(":", "").Replace(" ", "").Replace("T", "").Replace("Z", "").Replace("年", "").Replace("月", "").Replace("日", "");
            str = str.Substring(0, str.Length > 14 ? 14 : str.Length);
            if (str.Length == 8)
            {
                str = str.Insert(6, "-").Insert(4, "-");
            }
            else if (str.Length == 12)
            {
                str = str.Insert(10, ":").Insert(8, " ").Insert(6, "-").Insert(4, "-");
            }
            else if (str.Length == 14)
            {
                str = str.Insert(12, ":").Insert(10, ":").Insert(8, " ").Insert(6, "-").Insert(4, "-");
            }
            return DateTime.Parse(str);
        }
        /// <summary>
        /// 转换为DateTime格式，如果转换错误，则返回DateTime.MinValue
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static DateTime ToTryDateTime(this object obj)
        {
            try
            {
                string str = obj.ToString();
                if (string.IsNullOrEmpty(str))
                    return DateTime.MinValue;
                return obj.ToDateTime();
            }
            catch
            {
                return DateTime.MinValue;
            }
        }
        static Regex reg = new Regex(@"^(0\d{1}|1\d{1}|2[0-3]):[0-5]\d{1}$");
        /// <summary>
        /// 将输入的数字或时间转换为 时间格式如 09：00，无法正确转换的将返回string.Empty()
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string ToTime(this object obj)
        {
            if (obj == null || ((obj as string) == ""))
                return string.Empty;
            string time = string.Empty;
            string str = (string)obj;
            if (!str.Contains(":"))
            {
                if (str.Length == 1)
                {
                    time = "0" + str + ":00";
                }
                else if (str.Length == 2)
                {
                    time = str + ":00";
                }
                else if (str.Length == 3)
                {
                    time = str.Insert(2, "0").Insert(2, ":");
                }
                else if (str.Length == 4)
                {
                    time = str.Insert(2, ":");
                }
            }
            else
            {
                if (str.IndexOf(":") == 0)
                {
                    if (str.Length == 2) //:1
                        time = "00" + str + "0";
                    else if (str.Length == 3) //:12
                        time = "00" + str;
                }
                else if (str.IndexOf(":") == 1)
                {
                    if (str.Length == 3) //1:1
                        time = "0" + str.Insert(2, "0");
                    else if (str.Length == 4) //1:12
                        time = "0" + str;
                }
                else if (str.IndexOf(":") == 2)
                {
                    if (str.Length == 3) //01:
                        time = str + ":00";
                    else if (str.Length == 4) //01:1
                        time = str + "0";
                    else if (str.Length == 5) //01:12
                        time = str;
                }
            }
            if (!reg.IsMatch(time))
                time = string.Empty;
            return time;
        }
        #endregion

        #region Parse 的转换的处理

        /// <summary>
        /// 格式化为decimal格式，如果错误则抛出异常
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static decimal ToDecimal(this object obj)
        {
            decimal dec;
            if (obj is decimal)
                dec = (decimal)obj;
            else
            {
                string str = obj.ToString();
                str = string.IsNullOrEmpty(str) ? "0" : str;
                dec = decimal.Parse(str);
            }
            return dec;
        }

        /// <summary>
        /// 格式化为decimal格式，如果错误则返回0
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static decimal ToTryDecimal(this object obj)
        {
            try
            {
                return obj.ToDecimal();
            }
            catch
            {
                return 0m;
            }
        }
        /// <summary>
        /// 格式化为decimal格式，如果错误则抛出异常
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        public static decimal ToDecimal(this object obj, string format)
        {
            decimal dec;
            if (obj is decimal)
                dec = (decimal)obj;
            else
            {
                string str = obj.ToString();
                dec = decimal.Parse(str);
            }
            return decimal.Parse(dec.ToString(format));
        }
        /// <summary>
        /// 格式化为decimal格式，如果错误则返回0
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        public static decimal ToTryDecimal(this object obj, string format)
        {
            try
            {
                return obj.ToDecimal(format);
            }
            catch
            {
                return 0m;
            }
        }
        /// <summary>
        /// 格式化为int格式，如果错误则抛出异常
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static int ToInt(this object obj)
        {
            int dec;
            if (obj is int)
                dec = (int)obj;
            else
            {
                string str = obj.ToString();
                dec = int.Parse(str);
            }
            return dec;
        }
        /// <summary>
        /// 格式化为int格式，如果错误则返回0
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static int ToTryInt(this object obj)
        {
            try
            {
                return obj.ToInt();
            }
            catch
            {
                return 0;
            }
        }

        #region 将中文数字转换阿拉伯数字，支持中文金额转换
        /// <summary>
        /// 转换数字
        /// </summary>
        static long CharToNumber(char c)
        {
            switch (c)
            {
                case '一':
                case '壹': return 1;
                case '二':
                case '贰': return 2;
                case '三':
                case '叁': return 3;
                case '四':
                case '肆': return 4;
                case '五':
                case '伍': return 5;
                case '六':
                case '陆': return 6;
                case '七':
                case '柒': return 7;
                case '八':
                case '捌': return 8;
                case '九':
                case '玖': return 9;
                case '零': return 0;
                default: return -1;
            }
        }
        /// <summary>
        /// 转换单位
        /// </summary>
        static double CharToUnit(char c)
        {
            switch (c)
            {
                case '十':
                case '拾': return 10;
                case '百':
                case '佰': return 100;
                case '千':
                case '仟': return 1000;
                case '万': return 10000;
                case '亿': return 100000000;
                default: return 1;
            }
        }
        /// <summary>
        /// 将中文数字转换阿拉伯数字，支持中文金额转换
        /// </summary>
        /// <param name="cnum">汉字数字</param>
        /// <returns>长整型阿拉伯数字</returns>
        public static double ToDouble(this object obj)
        {
            string cnum = obj.ToString();
            if (cnum.EndsWith("整"))
                cnum = cnum.Substring(0, cnum.Length - 1);
            if (cnum.EndsWith("元"))
                cnum = cnum.Substring(0, cnum.Length - 1);
            string decimalPart = "";
            if (cnum.Contains("元"))
            {
                decimalPart = cnum.Split('元')[1];
                cnum = cnum.Split('元')[0];
            }
            cnum = Regex.Replace(cnum, "\\s+", "");
            double firstUnit = 1;//一级单位                
            double secondUnit = 1;//二级单位
            double result = 0;//结果
            for (var i = cnum.Length - 1; i > -1; --i)//从低到高位依次处理
            {
                var tmpUnit = CharToUnit(cnum[i]);//临时单位变量
                if (tmpUnit > firstUnit)//判断此位是数字还是单位
                {
                    firstUnit = tmpUnit;//是的话就赋值,以备下次循环使用
                    secondUnit = 1;
                    if (i == 0)//处理如果是"十","十一"这样的开头的
                    {
                        result += firstUnit * secondUnit;
                    }
                    continue;//结束本次循环
                }
                if (tmpUnit > secondUnit)
                {
                    secondUnit = tmpUnit;
                    continue;
                }
                result += firstUnit * secondUnit * CharToNumber(cnum[i]);//如果是数字,则和单位想乘然后存到结果里
            }
            if (!string.IsNullOrEmpty(decimalPart))
            {
                if (decimalPart.EndsWith("分"))
                {
                    var dp = decimalPart.Split('角');
                    result += (CharToNumber(dp[0][0]) * 0.1);
                    decimalPart = dp[1].Substring(0, dp[1].Length - 1);
                    result += (CharToNumber(decimalPart[0]) * 0.01);
                }
                else
                {
                    decimalPart = decimalPart.Substring(0, decimalPart.Length - 1);
                    result += (CharToNumber(decimalPart[0]) * 0.1);
                }
            }
            return result;
        }
        #endregion
        #endregion

        #region  将数字转换为 中文

        /// <summary>
        /// 转换人民币大小金额
        /// </summary>
        /// <param name="num">金额</param>
        /// <returns>返回大写形式</returns>
        public static string ToCmycurD(this decimal num)
        {
            bool flag = num.ToString2().StartsWith("-");
            string str1 = "零壹贰叁肆伍陆柒捌玖";            //0-9所对应的汉字
            string str2 = "万仟佰拾亿仟佰拾万仟佰拾元角分"; //数字位所对应的汉字
            string str3 = "";    //从原num值中取出的值
            string str4 = "";    //数字的字符串形式
            string str5 = "";  //人民币大写金额形式
            int i;    //循环变量
            int j;    //num的值乘以100的字符串长度
            string ch1 = "";    //数字的汉语读法
            string ch2 = "";    //数字位的汉字读法
            int nzero = 0;  //用来计算连续的零值是几个
            int temp;            //从原num值中取出的值

            num = Math.Round(Math.Abs(num), 2);    //将num取绝对值并四舍五入取2位小数
            str4 = ((long)(num * 100)).ToString();        //将num乘100并转换成字符串形式
            j = str4.Length;      //找出最高位
            if (j > 15) { return "溢出"; }
            str2 = str2.Substring(15 - j);   //取出对应位数的str2的值。如：200.55,j为5所以str2=佰拾元角分

            //循环取出每一位需要转换的值
            for (i = 0; i < j; i++)
            {
                str3 = str4.Substring(i, 1);          //取出需转换的某一位的值
                temp = Convert.ToInt32(str3);      //转换为数字
                if (i != (j - 3) && i != (j - 7) && i != (j - 11) && i != (j - 15))
                {
                    //当所取位数不为元、万、亿、万亿上的数字时
                    if (str3 == "0")
                    {
                        ch1 = "";
                        ch2 = "";
                        nzero = nzero + 1;
                    }
                    else
                    {
                        if (str3 != "0" && nzero != 0)
                        {
                            ch1 = "零" + str1.Substring(temp * 1, 1);
                            ch2 = str2.Substring(i, 1);
                            nzero = 0;
                        }
                        else
                        {
                            ch1 = str1.Substring(temp * 1, 1);
                            ch2 = str2.Substring(i, 1);
                            nzero = 0;
                        }
                    }
                }
                else
                {
                    //该位是万亿，亿，万，元位等关键位
                    if (str3 != "0" && nzero != 0)
                    {
                        ch1 = "零" + str1.Substring(temp * 1, 1);
                        ch2 = str2.Substring(i, 1);
                        nzero = 0;
                    }
                    else
                    {
                        if (str3 != "0" && nzero == 0)
                        {
                            ch1 = str1.Substring(temp * 1, 1);
                            ch2 = str2.Substring(i, 1);
                            nzero = 0;
                        }
                        else
                        {
                            if (str3 == "0" && nzero >= 3)
                            {
                                ch1 = "";
                                ch2 = "";
                                nzero = nzero + 1;
                            }
                            else
                            {
                                if (j >= 11)
                                {
                                    ch1 = "";
                                    nzero = nzero + 1;
                                }
                                else
                                {
                                    ch1 = "";
                                    ch2 = str2.Substring(i, 1);
                                    nzero = nzero + 1;
                                }
                            }
                        }
                    }
                }
                if (i == (j - 11) || i == (j - 3))
                {
                    //如果该位是亿位或元位，则必须写上
                    ch2 = str2.Substring(i, 1);
                }
                str5 = str5 + ch1 + ch2;

                if (i == j - 1 && str3 == "0")
                {
                    //最后一位（分）为0时，加上“整”
                    str5 = str5 + '整';
                }
            }
            if (num == 0)
            {
                str5 = "零元整";
            }
            else if (flag)
            {
                str5 = "负" + str5;
            }
            return str5;
        }

        /// <summary>
        /// 转换人民币大小金额
        /// </summary>
        /// <param name="num">金额</param>
        /// <returns>返回大写形式</returns>
        public static string ToCmycurD(this string numstr)
        {
            try
            {
                decimal num = Convert.ToDecimal(numstr);
                return ToCmycurD(num);
            }
            catch
            {
                return "非数字形式！";
            }
        }
        /// <summary>
        /// 转换人民币大小金额
        /// </summary>
        /// <param name="num">金额</param>
        /// <returns>返回大写形式</returns>
        public static string ToCmycurD(this double numstr)
        {
            try
            {
                decimal num = Convert.ToDecimal(numstr);
                return ToCmycurD(num);
            }
            catch
            {
                return "非数字形式！";
            }
        }
        #endregion

        /// <summary>  
        /// 获取枚举项描述信息 例如GetEnumDesc(Days.Sunday)  
        /// </summary>  
        /// <param name="en">枚举项 如Days.Sunday</param>  
        /// <returns></returns>  
        public static string GetEnumDesc(Enum en)
        {
            Type type = en.GetType();
            MemberInfo[] memInfo = type.GetMember(en.ToString());

            if (memInfo != null && memInfo.Length > 0)
            {
                object[] attrs = memInfo[0].GetCustomAttributes(false);
                if (attrs != null && attrs.Length > 0)
                    return ((Owl.Domain.DomainLabel)attrs[0]).Label;
            }
            return en.ToString();
        }

        ///<summary>  
        /// 获取枚举项+描述  
        ///</summary>  
        ///<param name="enumType">Type,该参数的格式为typeof(需要读的枚举类型)</param>  
        ///<returns>键值对</returns>  
        public static Dictionary<string, string> GetEnumItemDesc(Type enumType)
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();
            FieldInfo[] fieldinfos = enumType.GetFields();
            foreach (FieldInfo field in fieldinfos)
            {
                if (field.FieldType.IsEnum)
                {
                    Object[] objs = field.GetCustomAttributes(false);
                    dic.Add(field.Name, ((Owl.Domain.DomainLabel)objs[0]).Label);
                }
            }
            return dic;
        }

        ///<summary>  
        /// 获取枚举值+描述  
        ///</summary>  
        ///<param name="enumType">Type,该参数的格式为typeof(需要读的枚举类型)</param>  
        ///<returns>键值对</returns>  
        public static Dictionary<string, string> GetEnumItemValueDesc(Type enumType)
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();
            Type typeDescription = typeof(DescriptionAttribute);
            FieldInfo[] fields = enumType.GetFields();
            string strText = string.Empty;
            string strValue = string.Empty;
            foreach (FieldInfo field in fields)
            {
                if (field.FieldType.IsEnum)
                {
                    strValue = ((int)enumType.InvokeMember(field.Name, BindingFlags.GetField, null, null, null)).ToString();
                    object[] arr = field.GetCustomAttributes(typeDescription, true);
                    if (arr.Length > 0)
                    {
                        DescriptionAttribute aa = (DescriptionAttribute)arr[0];
                        strText = aa.Description;
                    }
                    else
                    {
                        strText = field.Name;
                    }
                    dic.Add(strValue, strText);
                }
            }
            return dic;
        }


        /// <summary>
        /// 将数字转换为 中文
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        public static String ConvertToChinese(this Decimal number)
        {
            var s = number.ToString("#L#E#D#C#K#E#D#C#J#E#D#C#I#E#D#C#H#E#D#C#G#E#D#C#F#E#D#C#.0B0A");
            var d = Regex.Replace(s, @"((?<=-|^)[^1-9]*)|((?'z'0)[0A-E]*((?=[1-9])|(?'-z'(?=[F-L\.]|$))))|((?'b'[F-L])(?'z'0)[0A-L]*((?=[1-9])|(?'-z'(?=[\.]|$))))", "${b}${z}");
            var r = Regex.Replace(d, ".", m => "负元空零壹贰叁肆伍陆柒捌玖空空空空空空空分角拾佰仟万亿兆京垓秭穰"[m.Value[0] - '-'].ToString());
            return r;
        }
        /// <summary>
        /// 将数字转换为 中文
        /// </summary>
        /// <param name="Num"></param>
        /// <returns></returns>
        public static string NumGetStr(this double Num)
        {
            string[] DX_SZ = { "零", "壹", "贰", "叁", "肆", "伍", "陆", "柒", "捌", "玖", "拾" };//大写数字  
            string[] DX_DW = { "元", "拾", "佰", "仟", "万", "拾", "佰", "仟", "亿", "拾", "佰", "仟", "万" };
            string[] DX_XSDS = { "角", "分" };//大些小数单位  
            if (Num == 0) return DX_SZ[0];

            Boolean IsXS_bool = false;//是否小数  

            string NumStr;//整个数字字符串  
            string NumStr_Zs;//整数部分  
            string NumSr_Xs = "";//小数部分  
            string NumStr_R = "";//返回的字符串  


            NumStr = Num.ToString();
            NumStr_Zs = NumStr;
            if (NumStr_Zs.Contains("."))
            {
                NumStr = Math.Round(Num, 2).ToString();
                NumStr_Zs = NumStr.Substring(0, NumStr.IndexOf("."));
                NumSr_Xs = NumStr.Substring((NumStr.IndexOf(".") + 1), (NumStr.Length - NumStr.IndexOf(".") - 1));
                IsXS_bool = true;
            }

            int k = 0;
            Boolean IsZeor = false;//整数中间连续0的情况  
            for (int i = 0; i < NumStr_Zs.Length; i++) //整数  
            {
                int j = int.Parse(NumStr_Zs.Substring(i, 1));
                if (j != 0)
                {
                    NumStr_R += DX_SZ[j] + DX_DW[NumStr_Zs.Length - i - 1];
                    IsZeor = false; //没有连续0  
                }
                else if (j == 0)
                {
                    k++;
                    if (!IsZeor && !(NumStr_Zs.Length == i + 1)) //等于0不是最后一位，连续0取一次  
                    {
                        //有问题  
                        if (NumStr_Zs.Length - i - 1 >= 4 && NumStr_Zs.Length - i - 1 <= 6)
                            NumStr_R += DX_DW[4] + "零";
                        else
                            if (NumStr_Zs.Length - i - 1 > 7)
                            NumStr_R += DX_DW[8] + "零";
                        else
                            NumStr_R += "零";

                        IsZeor = true;
                    }

                    if (NumStr_Zs.Length == i + 1)//  等于0且是最后一位 变成 XX元整  
                        NumStr_R += DX_DW[NumStr_Zs.Length - i - 1];
                }

            }
            if (NumStr_Zs.Length > 2 && k == NumStr_Zs.Length - 1)
                NumStr_R = NumStr_R.Remove(NumStr_R.IndexOf('零'), 1); //比如1000，10000元整的情况下 去0  

            if (!IsXS_bool) return NumStr_R + "整"; //如果没有小数就返回  
            else
            {
                for (int i = 0; i < NumSr_Xs.Length; i++)
                {
                    int j = int.Parse(NumSr_Xs.Substring(i, 1));
                    NumStr_R += DX_SZ[j] + DX_XSDS[NumSr_Xs.Length - i - 1];
                }
            }

            return NumStr_R;
        }

    }
}
