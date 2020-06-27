using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Owl.Domain.Validation
{
    /// <summary>
    /// 正则表达式验证
    /// </summary>
    public class RegexValidator : Validator
    {
        private string pattern;
        /// <summary>
        /// 正则表达式验证器
        /// </summary>
        /// <param name="errormessage">缺省错误</param>
        /// <param name="regexresource">可供翻译的资源名称</param>
        /// <param name="pattern">正则表达式</param>
        public RegexValidator(string errormessage, string regexresource, string pattern)
            : base("regex", errormessage, regexresource)
        {
            this.pattern = pattern;

            Parameters["pattern"] = pattern;
        }
        public override bool IsValid(object value, Object2 obj)
        {
            if (value != null)
                return Regex.IsMatch(value.ToString(), pattern);
            return base.IsValid(value, obj);
        }
    }

    public class PasswordRegexValidator : RegexValidator
    {
        public PasswordRegexValidator()
            : base("密码长度必须大于 6 并且必须包含 数字 大小写字母", "passworderror", @"((?=.*\d)(?=.*\D)(?=.*[a-z])(?=.*[A-Z]))^.{6,}$")
        {

        }
    }
}
