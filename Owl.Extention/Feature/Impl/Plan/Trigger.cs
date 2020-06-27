using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Domain;
using Owl.Util;
namespace Owl.Feature.iPlan
{
    /// <summary>
    /// 计划日期循环方式
    /// </summary>
    public enum PlanPeriod
    {
        [DomainLabel("不循环")]
        Once,
        [DomainLabel("按天")]
        Day,
        [DomainLabel("按周")]
        Week,
        [DomainLabel("按月")]
        Month
    }

    /// <summary>
    /// 日期选择模式
    /// </summary>
    public enum DateMode
    {
        [DomainLabel("天")]
        Days,
        [DomainLabel("周")]
        Weeks
    }

    /// <summary>
    /// 计划出发器
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class Trigger : Attribute
    {
        /// <summary>
        /// 计划开始时间 精确到分钟
        /// </summary>
        public DateTime StartTime { get; private set; }

        /// <summary>
        /// 日期循环方式
        /// </summary>
        public PlanPeriod Period { get; private set; }

        /// <summary>
        /// 循环间隔
        /// </summary>
        public int? Interval { get; private set; }

        /// <summary>
        /// 每周的日期 
        /// 周期为每周时有效
        /// </summary>
        public DayOfWeek[] WeekDays { get; private set; }

        /// <summary>
        /// 时间选择模式
        /// 周期为月时有效
        /// </summary>
        public DateMode Mode { get; private set; }


        /// <summary>
        /// 天,31表示本月最后一天
        /// </summary>
        public int[] Days { get; private set; }

        /// <summary>
        /// 每月的第几周集合，5表示本月的最后一周
        /// </summary>
        public int[] Weeks { get; private set; }

        /// <summary>
        /// 每年的第几月
        /// </summary>
        public int[] Months { get; private set; }

        /// <summary>
        /// 作用时段 每天的第几分钟-第几分钟
        /// </summary>
        public Tuple<double, double>[] DayPart { get; private set; }

        /// <summary>
        /// 区段内循环间隔 单位分钟,为0表示不循环
        /// </summary>
        public uint Repeat { get; private set; }

        /// <summary>
        /// 过期时间
        /// </summary>
        public DateTime? Expire { get; private set; }

        /// <summary>
        /// 最大延迟间隔
        /// </summary>
        public int MaxDelay { get; private set; }

        /// <summary>
        /// 是否在调试模式下运行
        /// </summary>
        public bool NoRunInDebug { get; set; }

        double getminutes(string span)
        {
            var tmp = span.Split(':');
            var hour = int.Parse(tmp[0]);
            var minute = int.Parse(tmp[1]);
            if (hour < 0 || hour > 24 || minute < 0 || minute > 60 || (hour == 24 && minute > 0))
                throw new Exception2("时间格式错误");
            return hour * 60 + minute;
        }

        bool InPart(DateTime trigger)
        {
            if (DayPart == null) return true;
            var minute = (trigger - trigger.Date).TotalMinutes;
            foreach (var part in DayPart)
            {
                if (minute >= part.Item1 && minute <= part.Item2)
                    return true;
            }
            return false;
        }
        /// <summary>
        /// 获取触发时间在时段的起始时间
        /// </summary>
        /// <param name="trigger">触发时间</param>
        /// <returns></returns>
        DateTime? GetPartStart(DateTime trigger)
        {
            if (DayPart == null)
                return trigger.Date;
            var minute = (trigger - trigger.Date).TotalMinutes;
            foreach (var part in DayPart)
            {
                if (minute >= part.Item1 && minute <= part.Item2)
                    return trigger.Date.AddMinutes(part.Item1);
            }
            return null;
        }

        private Trigger(string start, string daypart, uint repeat, string expire, int maxdelay)
        {
            if (string.IsNullOrEmpty(start))
                StartTime = DateTime.MinValue.Precision(TimePrecision.Minute);
            else
                StartTime = DateTime.Parse(start).Precision(TimePrecision.Minute);
            Repeat = repeat;
            MaxDelay = maxdelay;
            if (!string.IsNullOrEmpty(expire))
                Expire = DateTime.Parse(expire).Precision(TimePrecision.Minute);
            if (!string.IsNullOrEmpty(daypart))
            {
                var parts = new List<Tuple<double, double>>();
                foreach (var part in daypart.Split(';'))
                {
                    var tmp = part.Split('-');
                    var s = tmp[0];
                    var e = tmp[1];
                    parts.Add(new Tuple<double, double>(getminutes(s), getminutes(e)));
                }
                DayPart = parts.ToArray();
            }

        }

        /// <summary>
        /// 在指定时间的一天内重复执行计划 比如 2016-12-23 16:00:00 开始 每隔30分钟执行一次
        /// </summary>
        /// <param name="start">开始时间 精确到分钟</param>
        /// <param name="daypart">作用时段 精确到分钟，为空表示全时段,多个时段用 ';' 分隔 如 10:00-11:00;14:00-16:00</param>
        /// <param name="repeat">区间内重复间隔 以分钟为单位,为0时只执行一次</param>
        /// <param name="maxdelay">最大延迟次数 0表示无限</param>
        public Trigger(string start, string daypart, uint repeat, int maxdelay = 3)
            : this(start, daypart, repeat, null, maxdelay)
        {
            Period = PlanPeriod.Once;
        }
        /// <summary>
        /// 以指定时间开始每n天每隔n秒重复执行计划， 比如 2016-12-23 16:00:00 开始 每 2 天中 每隔30分钟执行一次
        /// </summary>
        /// <param name="start">开始时间 精确到分钟</param>
        /// <param name="interval">天数间隔 最小为1天</param>
        /// <param name="daypart">作用时段 精确到分钟，多个时段用 ';' 分隔 如 10:00-11:00;14:00-16:00</param>
        /// <param name="repeat">区间内重复间隔 以分钟为单位,为0时只执行一次</param>
        /// <param name="expire">到期时间 精确到分钟</param>
        /// <param name="maxdelay">最大延迟次数 0表示无限</param>
        public Trigger(string start, int interval, string daypart, uint repeat, string expire, int maxdelay = 3)
            : this(start, daypart, repeat, expire, maxdelay)
        {
            Period = PlanPeriod.Day;
            Interval = interval;
        }
        /// <summary>
        /// 以指定时间开始每n周的第{n1,n2,n3}日每n秒重复执行计划，比如 2016-12-23 16:00:00 开始 每 2 周的周一和周三  每隔30分钟执行一次
        /// </summary>
        /// <param name="start">开始时间 精确到分钟</param>
        /// <param name="interval">星期间隔 最小为1周</param>
        /// <param name="weekdays">每周的第几天 用逗号分隔 星期天 用0表示 如 1,2,5,0,为空表示当前周仅执行一次</param>
        /// <param name="daypart">作用时段 精确到分钟，多个时段用 ';' 分隔 如 10:00-11:00;14:00-16:00</param>
        /// <param name="repeat">日内重复间隔 以分钟为单位,为0时只执行一次</param>
        /// <param name="expire">到期时间 精确到分钟</param>
        /// <param name="maxdelay">最大延迟次数 0表示无限</param>
        public Trigger(string start, int interval, string weekdays, string daypart, uint repeat, string expire, int maxdelay = 3)
            : this(start, daypart, repeat, expire, maxdelay)
        {
            Period = PlanPeriod.Week;
            Interval = interval;
            if (!string.IsNullOrEmpty(weekdays))
                WeekDays = weekdays.Split(',').Select(s => (DayOfWeek)int.Parse(s)).ToArray();
        }
        /// <summary>
        /// 以指定时间开始的 {n1,n2,n3} 月份的第 {n1,n2,n3} 日 每n秒执行一次计划 2016-12-23 16:00:00 开始 的 1,3,5,9,10 月的 1,10,20,31 号 每隔30分钟执行一次
        /// </summary>
        /// <param name="start">开始时间 精确到分钟</param>
        /// <param name="months">多个月份中逗号分隔 如 1,3,5,7,9,10,12,空表示所有月份</param>
        /// <param name="days">多天用逗号分隔如 1,3,5,11,23,31 每月的最后一天用31表示，为空则表示当前月仅执行一天</param>
        /// <param name="daypart">作用时段 精确到分钟，多个时段用 ';' 分隔 如 10:00-11:00;14:00-16:00</param>
        /// <param name="repeat">日内重复间隔 以分钟为单位,为0时只执行一次</param>
        /// <param name="expire">到期时间 精确到分钟</param>
        /// <param name="maxdelay">最大延迟次数 0表示无限</param>
        public Trigger(string start, string months, string days, string daypart, uint repeat, string expire, int maxdelay = 3)
            : this(start, daypart, repeat, expire, maxdelay)
        {
            Period = PlanPeriod.Month;
            Mode = DateMode.Days;
            if (!string.IsNullOrEmpty(months))
                Months = months.Split(',').Select(s => int.Parse(s)).ToArray();
            if (!string.IsNullOrEmpty(days))
                Days = days.Split(',').Select(s => int.Parse(s)).ToArray();
        }
        /// <summary>
        /// 以指定时间开始的 {n1,n2,n3} 月份的第 {n1,n2,n3} 周 周{n1,n2,n3} 每n秒执行一次计划 2016-12-23 16:00:00 开始 的 1,3,5,9,10 月的 1，3 周 的 周1和周3 每隔30分钟执行一次
        /// </summary>
        /// <param name="start">开始时间 精确到分钟</param>
        /// <param name="months">多个月份中逗号分隔 如 1,3,5,7,9,10,12,空表示所有月份</param>
        /// <param name="weeks">每月的第几周，5表示最后一周，空表示所有周</param>
        /// <param name="weekdays">每周的第几天 用逗号分隔 星期天 用0表示 如 1,2,5,0，为空则表示当前周仅执行一次</param>
        /// <param name="daypart">作用时段 精确到分钟，多个时段用 ';' 分隔 如 10:00-11:00;14:00-16:00</param>
        /// <param name="repeat">日内重复间隔 以分钟为单位,为0时只执行一次</param>
        /// <param name="expire">到期时间 精确到分钟</param>
        /// <param name="maxdelay">最大延迟次数 0表示无限</param>
        public Trigger(string start, string months, string weeks, string weekdays, string daypart, uint repeat, string expire, int maxdelay = 3)
            : this(start, daypart, repeat, expire, maxdelay)
        {
            Period = PlanPeriod.Month;
            Mode = DateMode.Weeks;
            if (!string.IsNullOrEmpty(months))
                Months = months.Split(',').Select(s => int.Parse(s)).ToArray();
            Weeks = weeks.Split(',').Select(s => int.Parse(s)).ToArray();
            if (!string.IsNullOrEmpty(weekdays))
                Days = weekdays.Split(',').Select(s => int.Parse(s)).ToArray();
        }
        /// <summary>
        /// 判断是否可以触发
        /// </summary>
        /// <param name="triggertime">触发时间</param>
        /// <param name="lasttime">最后一次执行时间</param>
        /// <returns></returns>
        public bool CanTrigger(DateTime triggertime, DateTime? lasttime)
        {
            if (NoRunInDebug && AppConfig.Section.Debug)
                return false;
            if (StartTime > triggertime || (Expire.HasValue && Expire.Value < triggertime)) //如果开始时间大于触发时间或已过期则不可触发
                return false;
            //if (triggertime.TimeOfDay < StartTime.TimeOfDay)
            //    return false;
            if (!InPart(triggertime))
                return false;
            switch (Period)
            {
                case PlanPeriod.Once:
                    if (StartTime.Date != triggertime.Date)
                        return false;
                    break;
                case PlanPeriod.Day:
                    if (Interval > 1 && ((triggertime - StartTime).Days % Interval.Value) != 0)
                        return false;
                    break;
                case PlanPeriod.Week:
                    if (Interval > 1 && (DTHelper.GetMonday(triggertime) - DTHelper.GetMonday(StartTime)).Days % (7 * Interval.Value) != 0)
                        return false;
                    if (WeekDays == null)
                    {
                        if ((lasttime != null && lasttime.Value.Year == triggertime.Year && lasttime.Value.GetWeekOfYear() == triggertime.GetWeekOfYear() && lasttime.Value.Day != lasttime.Value.Day))
                            return false;
                        //return lasttime == null || lasttime.Value.Year != triggertime.Year || lasttime.Value.GetWeekOfYear() != triggertime.GetWeekOfYear();
                    }
                    else
                    {
                        if (!triggertime.DayOfWeek.In(WeekDays))
                            return false;
                    }
                    break;
                case PlanPeriod.Month:
                    if (Mode == DateMode.Days)
                    {
                        if (Months != null && !triggertime.Month.In(Months))
                            return false;
                        if (Days == null)
                        {
                            if (lasttime != null && lasttime.Value.Year == triggertime.Year && lasttime.Value.Month == triggertime.Month && lasttime.Value.Day != lasttime.Value.Day)
                                return false;
                            //return lasttime == null || lasttime.Value.Year != triggertime.Year || lasttime.Value.Month != triggertime.Month;
                        }
                        else
                        {
                            if (!Days.Contains(triggertime.Day) && (!Days.Contains(31) || triggertime.GetLastDayOfMonth() != triggertime.Date))
                                return false;
                        }
                    }
                    else
                    {
                        var monday = triggertime.GetMonday();
                        if (Months != null && Months.Length > 0 && !monday.Month.In(Months))
                            return false;
                        var week = monday.GetWeekOfMonth();
                        if (Weeks != null && !Weeks.Contains(week) && Weeks.Contains(5) && week != monday.GetMonthWeeks())
                            return false;
                        if (WeekDays == null)
                        {
                            if ((lasttime != null && lasttime.Value.Year == triggertime.Year && lasttime.Value.GetWeekOfYear() == triggertime.GetWeekOfYear() && lasttime.Value.Day != lasttime.Value.Day))
                                return false;
                            //return lasttime == null || lasttime.Value.Year != triggertime.Year || lasttime.Value.GetWeekOfYear() != triggertime.GetWeekOfYear();
                        }
                        else
                        {
                            if (!triggertime.DayOfWeek.In(WeekDays))
                                return false;
                        }
                    }
                    break;
            }

            var partstart = GetPartStart(triggertime).Value;
            if (Repeat == 0)
            {
                if (lasttime != null && lasttime.Value >= partstart)//判断本区段是否已经执行过
                    return false;
                //if (MaxDelay > 0 && ((int)(triggertime - partstart).TotalMinutes > MaxDelay))
                //    return false;
                return true;
            }
            else
            {
                var ltime = lasttime ?? StartTime;
                if (ltime < partstart)
                    return true;
                var intervalminutes = (int)(triggertime - ltime).TotalMinutes;
                var times = intervalminutes / Repeat + (intervalminutes % Repeat == 0 ? 0 : 1);
                return ltime.AddMinutes(Repeat * times) == triggertime;
            }
        }
    }
}
