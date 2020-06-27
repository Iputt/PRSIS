using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Owl.Domain
{
    /// <summary>
    /// 表单数据，为需要输入的消息提供数据
    /// </summary>
    public abstract class FormObject : BehaviorObject, IForm
    {
        /// <summary>
        /// 获取表单一览
        /// </summary>
        /// <returns></returns>
        public virtual string GetSummary()
        {
            return "";
        }
    }
    /// <summary>
    /// 备注数据，为需要注解的行为提供数据
    /// </summary>
    [DomainModel(Label = "备注")]
    public class FormRemark : FormObject
    {
        [DomainField(FieldType.text, Label = "内容")]
        public string Content { get; private set; }

        public override string GetSummary()
        {
            return Content;
        }
    }
}
