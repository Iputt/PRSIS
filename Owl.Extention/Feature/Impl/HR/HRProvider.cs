using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl;
namespace Owl.Feature.iHR
{
    public abstract class HRProvider : Provider
    {
        public abstract Organization GetOrganization(string mandt,Guid? partnerid, string code, DateTime date);

        public abstract IEnumerable<string> GetOrgFromDepartment(Guid? partnerid, string mandt, string bukrs, string code, DateTime date);

        public abstract IEnumerable<string> GetChildDepartments(Guid? partnerid, string fullcode, DateTime? date);

        public abstract void FillMember(Member member);
    }
}
