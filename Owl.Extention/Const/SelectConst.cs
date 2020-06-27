using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Domain;
using Owl.Const;

[assembly: SelectOption(SelectConst.AccountType, SelectConst.AccountType_Platform, "平台", 1)]
[assembly: SelectOption(SelectConst.AccountType, SelectConst.AccountType_Tenant, "客户端", 2)]
[assembly: SelectOption(SelectConst.PartnerType, SelectConst.AccountType_Supplier, "供应商", 3)]
[assembly: SelectOption(SelectConst.PartnerType, SelectConst.AccountType_Agent, "代理商", 4)]
[assembly: SelectOption(SelectConst.PartnerType, SelectConst.AccountType_Customer, "客户", 5)]
[assembly: SelectContain(SelectConst.AccountType, SelectConst.PartnerType)]
namespace Owl.Const
{
    /// <summary>
    /// select name const
    /// </summary>
    public class SelectConst
    {
        /// <summary>
        /// 任务计划
        /// </summary>
        public const string PlanList = "@owl_feature_plan_list";

        /// <summary>
        /// 星期几的列表
        /// </summary>
        public const string WeekDayList = "@owl_feature_plan_weekday";

        /// <summary>
        /// 每年的月份列表
        /// </summary>
        public const string MonthList = "@owl_feature_plan_month";

        /// <summary>
        /// 每月的星期列表
        /// </summary>
        public const string WeekInMonthList = "@owl_feature_plan_month_week";

        /// <summary>
        /// 每月的日列表
        /// </summary>
        public const string DayInMonthList = "@owl_feature_plan_month_day";

        /// <summary>
        /// 账户类型
        /// </summary>
        public const string AccountType = "@owl_feature_select_accounttype_list";

        /// <summary>
        /// 客户类型
        /// </summary>
        public const string PartnerType = "@owl_feature_select_partnertype_list";

        /// <summary>
        /// 系统角色类型
        /// </summary>
        public const string RoleType = "@owl_feature_select_roletype_list";
        /// <summary>
        /// 系统角色,跟角色类型联动
        /// </summary>
        public const string RoleListForType = "@owl_feature_select_role_list";

        /// <summary>
        /// 平台系统账户类型
        /// </summary>
        public const string AccountType_Platform = "Platform";

        /// <summary>
        /// 租户系统账户类型
        /// </summary>
        public const string AccountType_Tenant = "System";

        /// <summary>
        /// 合作伙伴账户类型
        /// </summary>
        public const string AccountType_Partner = "Partner";

        /// <summary>
        /// 供应商账户类型
        /// </summary>
        public const string AccountType_Supplier = "Supplier";

        /// <summary>
        /// 代理商账户类型
        /// </summary>
        public const string AccountType_Agent = "Agent";

        /// <summary>
        /// 客户账户类型
        /// </summary>
        public const string AccountType_Customer = "Customer";
    }
}
