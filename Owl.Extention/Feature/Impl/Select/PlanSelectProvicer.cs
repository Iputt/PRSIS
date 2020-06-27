using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Domain;
using Owl.Const;
namespace Owl.Feature.Impl.Select
{
    public class PlanSelectProvicer : SelectProvicer
    {
        public override int Priority
        {
            get { return 100; }
        }
        Dictionary<string, string> weekdays = new Dictionary<string, string>() {
            {"星期天","0"},{"星期一","1"},{"星期二","2"},{"星期三","3"},{"星期四","4"},{"星期五","5"},{"星期六","6"},
        };
        ListOptionCollection weekdayscollection;
        ListOptionCollection monthcollection;
        ListOptionCollection weekinmonth;
        ListOptionCollection dayinmonth;
        protected override void Init()
        {
            weekdayscollection = new ListOptionCollection(weekdays.Select(s => new ListItem(s.Value, s.Key)));
            monthcollection = new ListOptionCollection();
            for (var i = 1; i <= 12; i++)
            {
                monthcollection.AddOption(new ListItem(i.ToString(), i.ToString()));
            }
            weekinmonth = new ListOptionCollection();
            for (var i = 1; i <= 5; i++)
            {
                weekinmonth.AddOption(new ListItem(i.ToString(), i.ToString()));
            }
            dayinmonth = new ListOptionCollection();
            for (var i = 1; i <= 31; i++)
            {
                dayinmonth.AddOption(new ListItem(i.ToString(), i.ToString()));
            }

            Register(SelectConst.PlanList, top =>
            {
                return new ListOptionCollection(iPlan.PlanEngine.GetDescriptions().Select(s => new ListItem(s.Name, s.Display)));
            });
            Register(SelectConst.WeekDayList, top =>
            {
                return weekdayscollection;
            });
            Register(SelectConst.MonthList, top =>
            {
                return monthcollection;
            });
            Register(SelectConst.WeekInMonthList, top =>
            {
                return weekinmonth;
            });
            Register(SelectConst.DayInMonthList, top =>
            {
                return dayinmonth;
            });
        }
    }
}
