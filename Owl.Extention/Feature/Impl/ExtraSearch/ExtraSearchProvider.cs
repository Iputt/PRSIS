using System;
using Owl.Util;
using Owl.Domain;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Linq;

namespace Owl.Feature.ExtraSearch
{
    /// <summary>
    /// 全局搜素配置
    /// </summary>
    public class SearchConf : SmartObject
    {
        string m_model;
        /// <summary>
        /// 对象名称
        /// </summary>
        public string Model
        {
            get { return m_model; }
            set
            {
                m_model = value;
                m_meta = null;
            }
        }

        string m_condition;
        /// <summary>
        /// 查询条件
        /// </summary>
        public string Condition
        {
            get { return m_condition; }
            set
            {
                m_condition = value;
                m_spec = null;
            }
        }

        string m_tag;
        string[] tagfields;

        /// <summary>
        /// 返回标题
        /// </summary>
        public string Tag
        {
            get { return m_tag; }
            set
            {
                m_tag = value;
                tagfields = new string[0];
                if (!string.IsNullOrEmpty(value))
                    tagfields = value.Split(',');
            }
        }

        string m_fields;
        string[] summaryfields;
        /// <summary>
        /// 显示字段
        /// </summary>
        public string Fields
        {
            get { return m_fields; }
            set
            {
                m_fields = value;
                summaryfields = new string[0];
                if (!string.IsNullOrEmpty(value))
                    summaryfields = value.Split(',');
            }
        }

        /// <summary>
        /// 排序字段
        /// </summary>
        public string SortBy { get; set; }

        /// <summary>
        /// 排序方式
        /// </summary>
        public SortOrder SortOrder { get; set; }

        /// <summary>
        /// 优先级
        /// </summary>
        public int Priority { get; set; }

        ModelMetadata m_meta;
        public ModelMetadata Meta
        {
            get
            {
                if (m_meta == null)
                {
                    m_meta = ModelMetadataEngine.GetModel(Model);
                }
                return m_meta;
            }
        }

        Specification m_spec;

        public LambdaExpression GetExp(string term)
        {
            if (m_spec == null)
            {
                var specs = Condition.Split(',').Select(s => Specification.Compare(s, CmpCode.Con, "@term")).ToArray();
                m_spec = Specification.Or(specs);
                m_spec = Specification.And(m_spec, Member.Current.Filter(Model));
            }
            Variable.CurrentParameters["term"] = term;
            return m_spec.GetExpression(Meta);
        }

        public string[] GetSelector()
        {
            var result = new HashSet<string>(summaryfields);
            result.Add("Id");
            foreach (var field in tagfields)
            {
                if (!result.Contains(field))
                    result.Add(field);
            }
            return result.ToArray();
        }

        public string GetTag(TransferObject root)
        {
            return string.Format("{0} - {1}", string.Join(" ", tagfields.Select(s => root.GetDisplay(s))), Meta.Label);
        }

        public string GetSummary(TransferObject root)
        {
            return string.Join(" ", summaryfields.Select(s => root.GetDisplay(s)));
        }
    }

    public class SearchResult : SmartObject
    {
        public string Model { get; set; }
        public Guid Id { get; set; }
        public string Tag { get; set; }
        public string Summary { get; set; }
    }

    public abstract class ExtraSearchProvider : Provider
    {
        public abstract IEnumerable<SearchConf> GetSearchConf();
    }
}

