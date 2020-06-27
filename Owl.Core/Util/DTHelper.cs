using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Owl.Util
{
    /// <summary>
    /// 时间精度
    /// </summary>
    public enum TimePrecision
    {
        /// <summary>
        /// 年
        /// </summary>
        Year,
        /// <summary>
        /// 月
        /// </summary>
        Month,
        /// <summary>
        /// 日
        /// </summary>
        Day,
        /// <summary>
        /// 小时
        /// </summary>
        Hour,
        /// <summary>
        /// 分钟
        /// </summary>
        Minute,
        /// <summary>
        /// 秒
        /// </summary>
        Second
    }
    /// <summary>
    /// 时间增量
    /// </summary>
    public class TimeIncrement
    {
        public int Year { get; set; }

        public int Month { get; set; }

        public int Day { get; set; }

        public int Hour { get; set; }

        public int Minute { get; set; }

        public int Second { get; set; }

        public DateTime AddFor(DateTime org)
        {
            return org.AddYears(Year).AddMonths(Month).AddDays(Day).AddHours(Hour).AddMinutes(Minute).AddSeconds(Second);
        }

        public DateTime SubtractFor(DateTime org)
        {
            return org.AddYears(-Year).AddMonths(-Month).AddDays(-Day).AddHours(-Hour).AddMinutes(-Minute).AddSeconds(-Second);
        }

        public static implicit operator TimeIncrement(int[] part)
        {
            var ti = new TimeIncrement();
            var tmp = part.Reverse().ToArray();
            if (tmp.Length > 0)
                ti.Second = tmp[0];
            if (tmp.Length > 1)
                ti.Minute = tmp[1];
            if (tmp.Length > 2)
                ti.Hour = tmp[2];
            if (tmp.Length > 3)
                ti.Day = tmp[3];
            if (tmp.Length > 4)
                ti.Month = tmp[4];
            if (tmp.Length > 5)
                ti.Year = tmp[5];
            return ti;
        }
    }

    /// <summary>
    /// 时间日期帮助类
    /// </summary>
    public static class DTHelper
    {
        public static DateTime ParsePlain(string plainstring)
        {
            return new DateTime(
                int.Parse(plainstring.Substring(0, 4)),
                int.Parse(plainstring.Substring(4, 2)),
                int.Parse(plainstring.Substring(6, 2)),
                int.Parse(plainstring.Substring(8, 2)),
                int.Parse(plainstring.Substring(10, 2)),
                int.Parse(plainstring.Substring(12, 2)));
        }
        /// <summary>
        /// 所有本月的第一天
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static DateTime GetFisrtDayOfMonth(this DateTime date)
        {
            return date.AddDays(1 - date.Day).Date;
        }
        /// <summary>
        /// 获取本月的最后一天
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static DateTime GetLastDayOfMonth(this DateTime date)
        {
            return date.AddDays(1 - date.Day).Date.AddMonths(1).AddDays(-1).Date;
        }

        /// <summary>
        /// 获取本周的星期一
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static DateTime GetMonday(this DateTime date)
        {
            int di = (int)date.DayOfWeek;
            if (di == 0)
                di = 7;
            return date.AddDays(1 - di).Date;
        }

        /// <summary>
        /// 获取本周所在月份的第一个星期一
        /// 周的归属：以本周的星期一为标准，星期一所在的月份即为本周的月份
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static DateTime GetMondayOfMonth(this DateTime date)
        {
            var monday = date.GetMonday();
            return monday.AddDays(-(monday.Day / 7) * 7).Date;
        }

        /// <summary>
        /// 获取本周是本周所在本月的第几周
        /// 周的归属：以本周的星期一为标准，星期一所在的月份即为本周的月份
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static int GetWeekOfMonth(this DateTime date)
        {
            var monday = GetMonday(date);
            int weeks = monday.Day / 7;
            int yushu = monday.Day % 7;
            return yushu == 0 ? weeks : weeks + 1;
        }

        /// <summary>
        /// 获取本周所属的月份
        /// 周的归属：以本周的星期一为标准，星期一所在的月份即为本周的月份
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static int GetMonthOfWeek(this DateTime date)
        {
            var monday = GetMonday(date);
            return monday.Month;
        }

        /// <summary>
        /// 获取本周所在月份的总周数
        /// 周的归属：以本周的星期一为标准，星期一所在的月份即为本周的月份
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static int GetMonthWeeks(this DateTime date)
        {
            var monday = date.GetMondayOfMonth();
            if (monday.AddDays(28).Month == monday.Month)
                return 5;
            return 4;
        }
        /// <summary>
        /// 获取本周是本周所在年份的第几周
        /// 周的归属：以本周的星期一为标准，星期一所在的年份即为本周的年份
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static int GetWeekOfYear(this DateTime date)
        {
            var monday = GetMonday(date);
            int weeks = monday.DayOfYear / 7;
            int remain = monday.DayOfYear % 7;
            return remain == 0 ? weeks : weeks + 1;
        }
        /// <summary>
        /// 获取本周所在年份的第一个星期一
        /// 周的归属：以本周的星期一为标准，星期一所在的年份即为本周的年份
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static DateTime GetMondayOfYear(this DateTime date)
        {
            var monday = date.GetMonday();
            return monday.AddDays(-(monday.DayOfYear / 7) * 7).Date;
        }
        /// <summary>
        /// 获取本周所在年份的总周数
        /// 周的归属：以本周的星期一为标准，星期一所在的年份即为本周的年份
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static int GetYearWeeks(this DateTime date)
        {
            var monday = date.GetMondayOfYear();
            if (monday.AddDays(364).Year == monday.Year)
                return 53;
            return 52;
        }
        /// <summary>
        /// 日期精确度
        /// </summary>
        /// <param name="date"></param>
        /// <param name="precision"></param>
        /// <param name="addone">是否在精度的基础上加1</param>
        /// <returns></returns>
        public static DateTime Precision(this DateTime org, TimePrecision precision, bool addone = false)
        {
            var result = org;
            switch (precision)
            {
                case TimePrecision.Year:
                    result = new DateTime(org.Year, 1, 1);
                    if (addone)
                        result = result.AddYears(1);
                    break;
                case TimePrecision.Month:
                    result = new DateTime(org.Year, org.Month, 1);
                    if (addone)
                        result = result.AddMonths(1);
                    break;
                case TimePrecision.Day:
                    result = org.Date;
                    if (addone)
                        result = result.AddDays(1);
                    break;
                case TimePrecision.Hour:
                    result = new DateTime(org.Year, org.Month, org.Day, org.Hour, 0, 0);
                    if (addone)
                        result = result.AddHours(1);
                    break;
                case TimePrecision.Minute:
                    result = new DateTime(org.Year, org.Month, org.Day, org.Hour, org.Minute, 0);
                    if (addone)
                        result = result.AddMinutes(1);
                    break;
                case TimePrecision.Second:
                    result = new DateTime(org.Year, org.Month, org.Day, org.Hour, org.Minute, org.Second);
                    if (addone)
                        result = result.AddSeconds(1);
                    break;
            }
            return result;
        }

        /// <summary>
        /// 增加时间
        /// </summary>
        /// <param name="dt">时间</param>
        /// <param name="dtpart">数组，从右到左以此为 秒 分钟 小时 天 月 年</param>
        /// <returns></returns>
        public static DateTime Add(this DateTime dt, params int[] dtpart)
        {
            TimeIncrement inc = dtpart;
            return inc.AddFor(dt);
        }

        public static DateTime Subtract(this DateTime dt, params int[] dtpart)
        {
            TimeIncrement inc = dtpart;
            return inc.SubtractFor(dt);
        }
        /// <summary>
        /// 获取1970.1.1 到现在的毫秒数
        /// </summary>
        /// <returns>The time stamp.</returns>
        public static long GetTimeStamp(DateTime? time = null)
        {
            if (time == null)
                time = DateTime.UtcNow;
            else
                time = time.Value.ToUniversalTime();
            TimeSpan ts = time.Value - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalMilliseconds);
        }

        public static DateTime ToLocalTime(DateTime dateTime)
        {
            if (dateTime.Kind == DateTimeKind.Utc)
                return dateTime.ToLocalTime();
            return dateTime;
        }
    }
}
