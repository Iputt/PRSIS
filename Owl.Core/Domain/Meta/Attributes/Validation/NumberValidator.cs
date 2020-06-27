using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Util;

namespace Owl.Domain.Validation
{
    /// <summary>
    /// 整型验证器
    /// </summary>
    public class DigitsValidator : Validator
    {
        private const string _error = "字段 {0} 必须是整数";
        public DigitsValidator()
            : base("digits", _error, "digitserror")
        {
        }
        public override bool IsValid(object value, Object2 obj)
        {
            if (IsNull(value) || TypeHelper.IsDigit(value))
                return true;
            long tmp;
            return long.TryParse(value.ToString(), System.Globalization.NumberStyles.Any, null, out tmp);
        }
    }
    /// <summary>
    /// 数值验证器
    /// </summary>
    public class NumberValidator : Validator
    {
        public NumberValidator(int precision)
            : base("number", "字段 {0} 必须是有效的数字", "numbererror")
        {
        }

        public override bool IsValid(object value, Object2 obj)
        {
            if (IsNull(value) || TypeHelper.IsNumeric(value))
                return true;
            double tmp;
            return double.TryParse(value.ToString(), System.Globalization.NumberStyles.Any, null, out tmp);
        }
    }
    /// <summary>
    /// 数值范围
    /// </summary>
    public class RangeValidator : Validator
    {
        private const string _error = "字段 {0} 必须大于 {1} 并且小于 {2} ";
        private const string _errorresource = "rangeerror";

        private const string _minerror = "字段 {0} 不能小于 {1}";
        private const string _minsource = "mintherror";

        private const string _maxerror = "字段 {0} 不能大于 {1}";
        private const string _maxsource = "maxerror";

        private object min;
        private object max;
        private Type objtype;

        static string getError(object min, object max)
        {
            if (min != null && max != null)
                return _error;
            if (min != null)
                return _minerror;
            if (max != null)
                return _maxerror;
            return "";
        }
        static string getResource(object min, object max)
        {
            if (min != null && max != null)
                return _errorresource;
            if (min != null)
                return _minsource;
            if (max != null)
                return _maxsource;
            return "";
        }
        public RangeValidator(object min, object max)
            : base("range", getError(min, max), getResource(min, max))
        {
            if (min == null && max == null)
                throw new Exception2("最小值和最大值不能都为空");
            this.min = min;
            this.max = max;

            if (min != null)
            {
                this.Parameters["min"] = min;
                Argument.Add(min);
                objtype = min.GetType();
            }
            if (max != null)
            {
                this.Parameters["max"] = max;
                Argument.Add(max);
                objtype = max.GetType();
            }
        }
        public override bool IsValid(object value, Object2 obj)
        {
            if (IsNull(value))
                return true;
            var dvalue = Convert2.ChangeType(value, objtype);
            if (min != null && max != null)
                return ObjectExt.Compare(dvalue, min) >= 0 && ObjectExt.Compare(dvalue, max) <= 0;
            if (min != null)
                return ObjectExt.Compare(dvalue, min) >= 0;
            if (max != null)
                return ObjectExt.Compare(dvalue, max) <= 0;
            return true;
        }
    }
    /// <summary>
    /// 最小值
    /// </summary>
    public class MinValidator : RangeValidator
    {
        public MinValidator(decimal min)
            : base(min, null)
        {
        }
    }
    /// <summary>
    /// 最大值
    /// </summary>
    public class MaxValidator : RangeValidator
    {
        public MaxValidator(decimal max)
            : base(null, max)
        {
        }
    }
}
