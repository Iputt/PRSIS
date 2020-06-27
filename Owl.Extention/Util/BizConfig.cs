using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Domain;
using Owl.Feature;
using Owl.Util.iAppConfig;
using System.Configuration;
namespace Owl.Util
{
    /// <summary>
    /// 基础业务模块的配置
    /// </summary>
    public class BizConfig : CustomeConfigElement
    {
        /// <summary>
        /// 多组织结构
        /// </summary>
        [ConfigurationProperty("multiorg")]
        public bool MultiOrganization
        {
            get { return (bool)this["multiorg"]; }
            set { this["multiorg"] = value; }
        }

        /// <summary>
        /// 组织结构对象名
        /// </summary>
        [ConfigurationProperty("orgmodel")]
        public string OrgModel
        {
            get { return (string)this["orgmodel"]; }
            set { this["orgmodel"] = value; }
        }

        /// <summary>
        /// 多公司
        /// </summary>
        [ConfigurationProperty("multibukrs")]
        public bool MultiBukrs
        {
            get { return (bool)this["multibukrs"]; }
            set { this["multibukrs"] = value; }
        }
        /// <summary>
        /// 公司对应的对象
        /// </summary>
        [ConfigurationProperty("bukrsmodel")]
        public string BukrsModel
        {
            get { return (string)this["bukrsmodel"]; }
            set { this["bukrsmodel"] = value; }
        }
        /// <summary>
        /// 默认操作范围
        /// </summary>
        [ConfigurationProperty("opscope")]
        public OpScope OpScope
        {
            get { return (OpScope)this["opscope"]; }
            set { this["opscope"] = value; }
        }

        static BizConfig current;
        /// <summary>
        /// 当前配置
        /// </summary>
        public static BizConfig Current
        {
            get
            {
                if (current == null)
                {
                    current = AppConfig.Section.GetConfig<BizConfig>();
                    if (current == null)
                    {
                        current = new BizConfig()
                        {
                            OpScope = OpScope.MySelf,
                            MultiBukrs = true,
                            MultiOrganization = true
                        };
                    }
                }
                return current;
            }
        }
    }
}
