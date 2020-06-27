using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Util;
namespace Owl.Domain
{
    /// <summary>
    /// 权限
    /// </summary>
    public enum Permission
    {
        /// <summary>
        /// 无任何权限
        /// </summary>
        [IgnoreField]
        None = 0,
        /// <summary>
        /// 创建
        /// </summary>
        [DomainLabel("创建")]
        Create = 0x01,
        /// <summary>
        /// 读取
        /// </summary>
        [DomainLabel("读取")]
        Read = 0x02,
        /// <summary>
        /// 更新
        /// </summary>
        [DomainLabel("更新")]
        Update = 0x04,
        /// <summary>
        /// 删除
        /// </summary>
        [DomainLabel("删除")]
        Delete = 0x08,

        /// <summary>
        /// 写入
        /// </summary>
        [IgnoreField]
        Write = 0x0F,
        /// <summary>
        /// 导入
        /// </summary>
        [DomainLabel("导入")]
        Import = 0x10,
        /// <summary>
        /// 导出
        /// </summary>
        [DomainLabel("导出")]
        Export = 0x20,

        /// <summary>
        /// 变更日志
        /// </summary>
        [DomainLabel("变更日志")]
        LogModify = 0x40,

        /// <summary>
        /// 打印
        /// </summary>
        //[DomainLabel("打印")]
        //Print = 0x80,

        /// <summary>
        /// 导出pdf
        /// </summary>
        [DomainLabel("导出pdf")]
        Pdf = 0x100,
        /// <summary>
        /// 所有
        /// </summary>
        [IgnoreField]
        All = 0xFFFF,
    }

    public class MetaEngine : Engine<MetaProvider, MetaEngine>
    {
        static DomainModel system;
        protected static DomainModel System
        {
            get
            {
                if (system == null)
                {
                    system = new DomainModel()
                    {
                        Name = "system",
                        Label = "系统",
                        ObjType = DomainType.None,
                    };
                }
                return system;
            }
        }

        static MetaEngine()
        {
            Execute(s => s.Init);
        }
        /// <summary>
        /// 获取指定名称的模型元数据
        /// </summary>
        /// <param name="modelname"></param>
        /// <returns></returns>
        public static DomainModel GetModel(string modelname)
        {
            if (modelname == "*" || modelname == "system")
                return System;
            return DomainModel.GetModel(modelname);
        }

        public static DomainModel GetModel(Type type)
        {
            return DomainModel.FromType(type);
        }

        public static IEnumerable<DomainModel> GetModels()
        {
            return DomainModel.GetModels();
        }
    }
}
