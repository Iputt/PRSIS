using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Owl.Domain.Validation
{
    /// <summary>
    /// 必填验证
    /// </summary>
    public class RequiredValidator : Validator
    {
        private const string _errormessage = "字段 {0} 不能为空";
        private const string _errorresource = "requirederror";
        public RequiredValidator()
            : base("required", _errormessage, _errorresource)
        {

        }

        public override bool IsValid(object value, Object2 obj)
        {
            if (value == null || (value is string && string.IsNullOrEmpty((value as string))))
                return false;
            return true;
        }
    }
}
