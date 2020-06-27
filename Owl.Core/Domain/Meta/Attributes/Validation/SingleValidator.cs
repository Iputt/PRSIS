using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
namespace Owl.Domain.Validation
{
    /// <summary>
    /// 电子邮件
    /// </summary>
    public class EmailValidator : Validator
    {
        private static readonly Regex pattern = new Regex(@"^\w+([-+.]\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*$");
        private const string _error = "字段 {0} 不是有效的电子邮件地址";
        private const string _emailerror = "emailerror";

        public EmailValidator()
            : base("email", _error, _emailerror)
        {
        }
        public override bool IsValid(object value, Object2 obj)
        {
            if (IsNull(value))
                return true;
            return pattern.IsMatch((string)value);
        }
    }

    /// <summary>
    /// 网址
    /// </summary>
    public class UrlValidator : Validator
    {
        private static readonly Regex pattern = new Regex(@"^[\w-]+(\.[\w-]+)*@[\w-]+(\.[\w-]+)+$");
        private const string _error = "字段 {0} 不是有效的网址";
        private const string _urlerror = "urlerror";

        public UrlValidator()
            : base("url", _error, _urlerror)
        {
        }
        public override bool IsValid(object value, Object2 obj)
        {
            if (IsNull(value))
                return true;
            return pattern.IsMatch((string)value);
        }
    }
    /// <summary>
    /// 信用卡
    /// </summary>
    public class CreditcardValidator : Validator
    {
        private static readonly Regex pattern = new Regex(@"^(4\\d{12}(?:\\d{3})?)$");
        private const string _error = "字段 {0} 不是有效的信用卡号码";
        private const string _creditcarderror = "creditcarderror";
        public CreditcardValidator()
            : base("creditcard", _error, _creditcarderror)
        {
        }
        public override bool IsValid(object value, Object2 obj)
        {
            if (IsNull(value))
                return true;
            return pattern.IsMatch((string)value);
        }
    }
    /// <summary>
    /// 日期
    /// </summary>
    public class DateValidator : Validator
    {
        private static readonly Regex pattern = new Regex(@"^\d{4}[\/\-](0?[1-9]|1[012])[\/\-](0?[1-9]|[12][0-9]|3[01])$");
        private const string _error = "字段 {0} 不是有效的日期";
        private const string _creditcarderror = "creditcarderror";
        public DateValidator()
            : base("dateISO", _error, _creditcarderror)
        {
        }
        public override bool IsValid(object value, Object2 obj)
        {
            if (IsNull(value) || value is DateTime)
                return true;
            DateTime date;
            return DateTime.TryParse((string)value, out date);
        }
    }
}
