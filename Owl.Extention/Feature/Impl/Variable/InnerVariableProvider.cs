using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Domain;
using Owl.Feature;
namespace Owl.Feature.Impl.Variable
{
    public class VarDate : SmartObject
    {
        public DateTime Now { get { return DateTime.Now; } }

        /// <summary>
        /// 今天
        /// </summary>
        public string Today { get { return DateTime.Today.ToString("yyyy-MM-dd"); } }

        /// <summary>
        /// 
        /// </summary>
        public string Yesterday { get { return DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd"); } }


        public static VarDate Instance = new VarDate();
    }

    internal class InnerVariableProvider : VariableProvider
    {
        public override bool Contain(string key)
        {
            switch (key)
            {
                case "date":
                case "self":
                case "selflogin":
                case "selfname":
                case "selfaccount":
                case "selfactname":
                case "isSystem":
                case "today": return true;
                default: return false;
            }
        }
        public override object GetValue(string parameter)
        {
            switch (parameter)
            {
                case "self": return Member.Current;//登录名
                case "selflogin": return Member.Current.Login;
                case "selfname": return Member.Current.Name;//姓名
                case "selfaccount": return Member.Current.Account.Id;//公司id
                case "selfactname": return Member.Current.Account.Name;//公司名
                case "isSystem": return Member.Current.Account.Id == null;//是否系统帐户
                case "today": return DateTime.Today.ToString("yyyy-MM-dd");//今天
                case "date": return VarDate.Instance;
                case "year": return DateTime.Today.ToString("yyyy");
                case "lastyear": return (DateTime.Today.Year - 1).ToString();
                case "month": return DateTime.Today.ToString("yyyyMM");
            }
            return null;
        }

        public override int Priority
        {
            get { return 500; }
        }
    }
}
