using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Owl.Feature.iHR
{
    public class HREngine : Engine<HRProvider, HREngine>
    {
        protected override bool SkipException
        {
            get
            {
                return false;
            }
        }
        /// <summary>
        /// 获取客户端的组织结构信息
        /// </summary>
        /// <param name="mandt">客户端</param>
        public static Organization GetOrganization(string mandt, Guid? partnerid, string code, DateTime date)
        {
            if (mandt == "")
                mandt = null;
            return Execute2<string, Guid?, string, DateTime, Organization>(s => s.GetOrganization, mandt, partnerid, code, date);
        }

        public static IEnumerable<string> GetOrgFromDepartment(Guid? partnerid, string mandt, string bukrs, string department, DateTime date)
        {
            if (mandt == "")
                mandt = null;
            return Execute2<Guid?, string, string, string, DateTime, IEnumerable<string>>(s => s.GetOrgFromDepartment, partnerid, mandt, bukrs, department, date);
        }

        public static IEnumerable<string> GetChildDepartments(Guid? partnerid, string fullcode, DateTime? date)
        {
            return Execute2<Guid?, string, DateTime?, IEnumerable<string>>(s => s.GetChildDepartments, partnerid, fullcode, date);
        }

        public static void FillMember(Member member)
        {
            Execute(s => s.FillMember, member);
        }
    }
}
