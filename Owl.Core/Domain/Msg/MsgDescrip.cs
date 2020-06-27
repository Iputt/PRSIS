using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Util;

namespace Owl.Domain
{
    /// <summary>
    /// 消息描述
    /// </summary>
    public class MsgDescrip : SmartObject
    {
        /// <summary>
        /// 消息名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 其他名称，用于进行权限控制等
        /// </summary>
        public string Alias { get; set; }
        /// <summary>
        /// 资源名称
        /// </summary>
        public string Resource { get; set; }

        public string GetResKey()
        {
            return Resource.Coalesce(string.Format("msglabel.{0}.{1}", Model, Name));
        }
        /// <summary>
        /// 展示名称
        /// </summary>
        public string Label2 { get; set; }

        /// <summary>
        /// 翻译后的展示名称
        /// </summary>
        public string Label
        {
            get
            {
                return Feature.Translation.Get(GetResKey(), Label2, true);
            }
        }
        /// <summary>
        /// 关联对象
        /// </summary>
        public string Model { get; set; }

        /// <summary>
        /// 对象所拥有的实体对象
        /// </summary>
        public string EntityModel { get; set; }
        /// <summary>
        /// 消息来源
        /// </summary>
        public MetaMode Source { get; set; }

        /// <summary>
        /// 数据限定
        /// </summary>
        public RootRestrict Restrict { get; set; }

        /// <summary>
        /// 加载指定数据时同时加载相关关系数据
        /// </summary>
        public string[] Relations { get; set; }

        /// <summary>
        /// 执行条件
        /// </summary>
        public Specification Condition { get; set; }

        /// <summary>
        /// 可进行权限配置
        /// </summary>
        public bool PermissionConfig { get; set; }
        

        /// <summary>
        /// 单例模式
        /// </summary>
        public bool Singleton { get; set; }

        /// <summary>
        /// 单例模式超时时间(秒)
        /// </summary>
        public int? SingleTimeout { get; set; }

        /// <summary>
        /// 单例模式执行中提示
        /// </summary>
        public string SingleNotify { get; set; }

        /// <summary>
        /// 单例模式执行中提示资源名称
        /// </summary>
        public string SingleNotifyResource { get; set; }
        /// <summary>
        /// 消息成功执行后
        /// </summary>
        public IEnumerable<IMessageBehavior> Behaviors { get; set; }

        /// <summary>
        /// 消息类型
        /// </summary>
        public MsgType Type { get; set; }

        /// <summary>
        /// 是否自动显示在工具栏上
        /// </summary>
        public bool AutoShow { get; set; }

        /// <summary>
        /// 显示序号
        /// </summary>
        public int Ordinal { get; set; }


        /// <summary>
        /// 访问控制列表
        /// </summary>
        public AuthObject AuthObj { get; set; }

        /// <summary>
        /// 确认信息
        /// </summary>
        public string Confirm { get; set; }


        /// <summary>
        /// 参数对象
        /// </summary>
        public string ParamModel { get; set; }

        ModelMetadata m_parammeta;
        /// <summary>
        /// 参数对象元数据
        /// </summary>
        public ModelMetadata ParamMetadata
        {
            get
            {
                if (m_parammeta == null && ParamModel != null && ParamModel != "")
                    m_parammeta = ModelMetadataEngine.GetModel(ParamModel);
                return m_parammeta;
            }
            set
            {
                m_parammeta = value;
                if (value != null)
                    ParamModel = value.Name;
            }
        }
        /// <summary>
        /// 消息参数包含字段
        /// </summary>
        public bool ParamHasfield
        {
            get { return ParamMetadata != null && ParamMetadata.FieldCount > 0; }
        }
    }
}
