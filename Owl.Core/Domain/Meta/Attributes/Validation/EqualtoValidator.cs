using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Owl.Domain.Validation
{
    public class EqualtoValidator : Validator
    {
        private const string _error = "两次输入不一致";
        private const string _equalerror = "equalerror";

        private string other;
        public EqualtoValidator(string other)
            : base("equalto", _error, _equalerror)
        {
            this.other = other;
            Parameters["other"] = other;
        }
        public override bool IsValid(object value, Object2 obj)
        {
            return Object.Equals(value, obj[other]);
        }
    }
}
