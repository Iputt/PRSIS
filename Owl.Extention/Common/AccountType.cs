using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Owl.Domain;
namespace Owl.Common
{
    /// <summary>
    /// 账户等级
    /// </summary>
    public enum AccountLevel
    {
        /// <summary>
        /// 平台账户
        /// </summary>
        Platform,
        /// <summary>
        /// 租户账户
        /// </summary>
        Tenant,
        /// <summary>
        /// 合作伙伴账户
        /// </summary>
        Partner
    }
    /// <summary>
    /// 账户类型
    /// </summary>
    public class AccountType : SmartObject
    {
        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 等级
        /// </summary>
        public int Level { get; set; }
    }
}
