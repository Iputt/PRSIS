using Owl.Feature;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Owl.Const
{
    public class CoreConst
    {
        /// <summary>
        /// 最小时间
        /// </summary>
        public static readonly DateTime MinTime = DateTime.Parse("1753-01-01");


        /// <summary>
        /// 对象列表名称
        /// </summary>
        public const string ModelList = "@owl_domain_model";

        /// <summary>
        /// 根据字段类型变化的对象列表
        /// </summary>
        public const string ModelForFieldTypeList = "@owl_domain_model_field_type";
        /// <summary>
        /// 自定义对象模板
        /// </summary>
        public const string CustomObjectTemplateList = "@owl_domain_custom_template";
        /// <summary>
        /// 字段列表名称
        /// </summary>
        public const string FieldList = "@owl_domain_model_field";

        /// <summary>
        /// 字段列表包含下属的Entity的字段
        /// </summary>
        public const string FieldListWithEntity = "@owl_domain_model_field_with_entity";

        /// <summary>
        /// 字段值
        /// </summary>
        public const string FieldValue = "@owl_domain_model_fieldvalue";
        /// <summary>
        /// 根对象列表
        /// </summary>
        public const string RootList = "@owl_domain_model_root";
        /// <summary>
        /// 实体对象列表
        /// </summary>
        public const string EngityList = "@owl_domain_model_entity";
        /// <summary>
        /// 事件模型列表
        /// </summary>
        public const string EventList = "@owl_domain_model_event";

        /// <summary>
        /// 表单对象列表
        /// </summary>
        public const string FormList = "@owl_domain_model_form";

        /// <summary>
        /// 模板引擎列表
        /// </summary>
        public const string TemplateList = "@owl_feature_temlate_list";
        /// <summary>
        /// 状态
        /// </summary>
        public const string Status = "@Status";

        /// <summary>
        /// select display 分割符
        /// </summary>
        public static string SelectDisplaySeparator = Util.AppConfig.GetSetting("OwlSelectDisplaySeparator", " ");

        public static bool SelectDisplayHideCode = bool.Parse(DbSfConfig.GetSetting("sys_config", "OwlSelectDisplayHideCode", "false"));
        /// <summary>
        /// 构建select option的展示文本
        /// </summary>
        /// <param name="texts"></param>
        /// <returns></returns>
        public static string BuildOptionDisplay(string code, string text)
        {
            if (Owl.Feature.SelectContext.Current.HideCode ?? SelectDisplayHideCode)
                return text;
            return string.Format("{0}{1}{2}", code, SelectDisplaySeparator, text);
        }

        /// <summary>
        /// 通用翻译资源
        /// </summary>
        public const string Transition_Common = "owl.feature.transition.common";
    }
    /// <summary>
    /// 翻译资源名称
    /// </summary>
    public class ResName
    {
        /// <summary>
        /// 验证器
        /// </summary>
        public const string Validator = "owl.validation.errorresource";
    }

    public static class RoleUsed
    {
        /// <summary>
        /// 系统
        /// </summary>
        public const string System = "System";
        /// <summary>
        /// 代理商
        /// </summary>
        public const string Agent = "Agent";
        /// <summary>
        /// 供应商
        /// </summary>
        public const string Supplier = "Supplier";

        /// <summary>
        /// 普通客户
        /// </summary>
        public const string Customer = "Customer";
    }
}
