using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Feature.iHR;

namespace Owl.Feature
{
    /// <summary>
    /// 组织结构
    /// </summary>
    public class HR
    {
        /// <summary>
        /// 获取指定代码指定日期的组织结构
        /// </summary>
        /// <param name="mandt">客户端</param>
        /// <param name="bukrs">公司</param>
        /// <param name="code">组织结构代码</param>
        /// <param name="date">日期</param>
        /// <returns></returns>
        public static Organization GetOrganization(string mandt, Guid? partnerid, string code, DateTime date)
        {
            return HREngine.GetOrganization(mandt, partnerid, code, date);
        }



        /// <summary>
        /// 根据部门获取组织结构代码
        /// </summary>
        /// <param name="mandt">客户端</param>
        /// <param name="bukrs">公司</param>
        /// <param name="code">部门</param>
        /// <param name="date">日期</param>
        /// <returns></returns>
        public static IEnumerable<string> GetOrgFromDepartment(Guid? partnerid, string mandt, string bukrs, string code, DateTime date)
        {
            return HREngine.GetOrgFromDepartment(partnerid, mandt, bukrs, code, date);
        }
        /// <summary>
        /// 获取子部门
        /// </summary>
        /// <param name="fullcode"></param>
        /// <returns></returns>
        public static IEnumerable<string> GetChildDepartments(Guid? partnerid, string fullcode, DateTime? date = null)
        {
            return HREngine.GetChildDepartments(partnerid, fullcode, date);
        }

        /// <summary>
        /// 获取员工
        /// </summary>
        /// <param name="mandt">客户端</param>
        /// <param name="login">登录名</param>
        public static void FillMember(Member member)
        {
            HREngine.FillMember(member);
        }
    }
}
