using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Feature;
using Owl.Util;

namespace Owl.Domain
{

    /// <summary>
    /// 智能表单
    /// </summary>
    [CustomObjectTemplate(Label = "表单对象",  Ordinal = 800)]
    public sealed class SmartForm : FormObject
    {
        public SmartForm()
        {
        }
        public SmartForm(ModelMetadata meta)
        {
            Metadata = meta;
        }

        FormExtension m_extension;
        FormExtension extension
        {
            get
            {
                if (m_extension == null)
                {
                    m_extension = Metadata.GetExtension<FormExtension>();
                    if (m_extension == null)
                        m_extension = new FormExtension();
                }
                return m_extension;
            }
        }
        public override string GetSummary()
        {
            return extension.GetSummary(this);
        }
        

        protected override void BuildDefault()
        {
            extension.BuildDefault(this);
        }
    }
}
