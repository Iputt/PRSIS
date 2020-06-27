using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Owl.Domain.Validation
{

    /// <summary>
    /// 长度验证器
    /// </summary>
    public class LengthValidator : Validator
    {
        private const string _error = "字段 {0} 的长度必须大于 {1} 并且小于 {2}";
        private const string _errorresource = "lengtherror";

        private const string _minerror = "字段 {0} 的长度不能小于 {1} ";
        private const string _minsource = "minlengtherror";

        private const string _maxerror = "字段 {0} 的长度不能大于 {1} ";
        private const string _maxsource = "maxlengtherror";

        private int minLength;
        private int maxLength;

        static string getError(int minlength, int maxlength)
        {
            if (minlength > 0 && maxlength > 0)
                return _error;
            if (minlength > 0)
                return _minerror;
            if (maxlength > 0)
                return _maxerror;
            return "";
        }
        static string getResource(int minlength, int maxlength)
        {
            if (minlength > 0 && maxlength > 0)
                return _errorresource;
            if (minlength > 0)
                return _minsource;
            if (maxlength > 0)
                return _maxsource;
            return "";
        }

        public override bool IsValid(object value, Object2 obj)
        {
            if (value == null)
                return true;
            string v = (string)value;
            if (minLength > 0 && maxLength > 0)
                return v.Length >= minLength && v.Length <= maxLength;
            if (minLength > 0)
                return v.Length >= minLength;
            if (maxLength > 0)
                return v.Length <= maxLength;
            return true;
        }

        public LengthValidator(int minlength, int maxlength)
            : base("length", getError(minlength, maxlength), getResource(minlength, maxlength))
        {
            if (minlength > maxlength)
                throw new Exception2("最小长度不能大于最大长度");

            if (maxlength == 0 && maxlength == 0)
                throw new Exception2("最小长度和最大长度不能都为0");

            minLength = minlength;
            maxLength = maxlength;

            if (minlength > 0)
            {
                Parameters["min"] = minlength;
                Argument.Add(minlength);
            }
            if (maxlength > 0)
            {
                Parameters["max"] = maxlength;
                Argument.Add(maxlength);
            }
        }
    }
    /// <summary>
    /// 最小长度
    /// </summary>
    public class MinLengthValidator : LengthValidator
    {
        public MinLengthValidator(int minlength)
            : base(minlength, 0)
        {
        }
    }
    /// <summary>
    /// 最大长度
    /// </summary>
    public class MaxLengthValidator : LengthValidator
    {
        public MaxLengthValidator(int maxlength)
            : base(0, maxlength)
        {
        }
    }
}
