using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Domain;
using Owl.Feature.Impl.Config;
namespace Owl.Feature
{
    
    public class SysSection : Section
    {
        /// <summary>
        /// 超级账号
        /// </summary>
        public string SuperUser { get; set; }
    }

    /// <summary>
    /// 商标显示模式
    /// </summary>
    public enum BrandStyle
    {
        /// <summary>
        /// 仅logo
        /// </summary>
        Logo,
        /// <summary>
        /// 仅文本
        /// </summary>
        Text,
        /// <summary>
        /// Logo和文本
        /// </summary>
        Both
    }

    /// <summary>
    /// 公司信息配置节
    /// </summary>
    public class CompanySection : Section
    {

        static CompanySection()
        {
            RegisterDefault(new CompanySection()
            {
                Name = "Owl Rapid Development Framework",
                BrandName = "ORDF",
                Title = "Owl Rapid Development Framework",
            });
        }
        /// <summary>
        /// 公司名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 年份
        /// </summary>
        public string Year { get; set; }
        /// <summary>
        /// 公司地址
        /// </summary>
        public string Address { get; set; }
        /// <summary>
        /// 联系电话
        /// </summary>
        public string Phone { get; set; }

        /// <summary>
        /// 标题
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// 商标Logo
        /// </summary>
        public string BrandLogo { get; set; }

        /// <summary>
        /// 商标名称
        /// </summary>
        public string BrandName { get; set; }

        /// <summary>
        /// 商标显示
        /// </summary>
        public BrandStyle BrandStyle { get; set; }
    }

    /// <summary>
    /// 配置节管理
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal static class Section<T>
        where T : Section, new()
    {
        static string cachekey = string.Format("owl.feature.config.{0}", typeof(T).FullName.ToString().ToLower());
        /// <summary>
        /// 当前配置
        /// </summary>
        public static T Current
        {
            get
            {
                var section = Cache.Session<T>(cachekey, () => SectionEngine.GetConfig<T>());
                if (section == null)
                {
                    section = Section.Default<T>();
                }
                return section;
            }
        }
    }
    /// <summary>
    /// 配置管理
    /// </summary>
    public static class Config
    {
        /// <summary>
        /// 获取配置节
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T Section<T>()
            where T : Section, new()
        {
            return Feature.Section<T>.Current;
        }
    }
}
