using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System
{
    /// <summary>
    /// 月份
    /// </summary>
    public class DateMonth
    {
        DateTime m_month;
        private DateMonth() { }

        private DateMonth(string month)
        {
            m_month = DateTime.Parse(month.Insert(4, "-") + "-01");
        }

        /// <summary>
        /// 增加月
        /// </summary>
        /// <param name="months">正数表示增加，负数表示减少</param>
        public DateMonth AddMonth(int months)
        {
            return new DateMonth() { m_month = m_month.AddMonths(months) };
        }

        /// <summary>
        /// 当前月份跟目标月份的间隔
        /// </summary>
        /// <param name="dest"></param>
        /// <returns></returns>
        public int Subtract(DateMonth dest)
        {
            var monthsub = m_month.Month - dest.m_month.Month;
            var myearsub = m_month.Year - dest.m_month.Year;
            return myearsub * 12 + monthsub;
        }
        /// <summary>
        /// 获取第一天
        /// </summary>
        /// <returns></returns>
        public DateTime GetFirstDay()
        {
            return m_month.AddDays(0);
        }

        /// <summary>
        /// 获取最后一天
        /// </summary>
        /// <returns></returns>
        public DateTime GetLastDay()
        {
            return m_month.AddMonths(1).AddDays(-1);
        }

        /// <summary>
        /// 获取本月的总天数
        /// </summary>
        /// <returns></returns>
        public int TotalDays()
        {
            return GetLastDay().Day;
        }

        public override string ToString()
        {
            return m_month.ToString("yyyyMM");
        }

        /// <summary>
        /// 解析月
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static DateMonth Parse(string input)
        {
            return new DateMonth(input);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dateTime"></param>
        /// <param name="maxday">输入日期在本月的天数小于本参数则认为是上个月</param>
        /// <returns></returns>
        public static DateMonth Parse(DateTime dateTime, int maxday = 0)
        {
            dateTime = dateTime.Date;
            if (maxday > 0 && dateTime.Day <= maxday)
            {
                return new DateMonth() { m_month = dateTime.AddDays(1 - dateTime.Day).AddMonths(-1) };
            }
            return new DateMonth() { m_month = dateTime.AddDays(1 - dateTime.Day) };
        }
        public override bool Equals(object obj)
        {
            if (obj is DateMonth)
                return m_month == (obj as DateMonth).m_month;
            return false;
        }
        public override int GetHashCode()
        {
            return m_month.GetHashCode();
        }
        public static bool operator ==(DateMonth dm1, DateMonth dm2)
        {
            return dm1.m_month == dm2.m_month;
        }
        public static bool operator !=(DateMonth dm1, DateMonth dm2)
        {
            return dm1.m_month != dm2.m_month;
        }
        public static int operator -(DateMonth src, DateMonth dest)
        {
            return src.Subtract(dest);
        }
    }
}
