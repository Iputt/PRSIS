using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Owl.Domain.Validation
{
    /// <summary>
    /// 远程验证
    /// </summary>
    public class RemoteValidator : Validator
    {
        private const string _error = "验证失败";
        private const string _remoteerror = "remoteerror";

        private string url;
        private string afields;
        public RemoteValidator(string url, string additionalfields)
            : base("remote", _error, _remoteerror)
        {
            this.url = url;
            this.afields = additionalfields;

            Parameters["url"] = url;
            Parameters["type"] = "POST";
            Parameters["additionalfields"] = additionalfields;
        }

        public override bool IsValid(object value, Object2 obj)
        {
            return true;
        }
    }

    public class UniqueValidator : RemoteValidator
    {
        public UniqueValidator(string _event, string additionalfields)
            : base("", "")
        {

        }
    }
}
