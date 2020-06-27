using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
namespace Owl.Domain
{
    /// <summary>
    /// 字段类型
    /// </summary>
    public enum FieldType
    {
        [IgnoreField]
        none,

        /// <summary>
        /// guid
        /// </summary>
        [DomainLabel("GUID")]
        uniqueidentifier,

        /// <summary>
        /// 字符串
        /// </summary>
        [DomainLabel("字符串")]
        str,

        /// <summary>
        /// 整数
        /// </summary>
        [DomainLabel("整数")]
        digits,
        /// <summary>
        /// 数字
        /// </summary>
        [DomainLabel("数字")]
        number,

        /// <summary>
        /// 可选项
        /// </summary>
        [DomainLabel("可选项")]
        select,

        /// <summary>
        /// 布尔
        /// </summary>
        [DomainLabel("布尔")]
        bollean,

        [DomainLabel("月份")]
        datemonth,
        /// <summary>
        /// 日期
        /// </summary>
        [DomainLabel("日期")]
        date,
        /// <summary>
        /// 日期时间
        /// </summary>
        [DomainLabel("日期时间")]
        datetime,
        /// <summary>
        /// 时间
        /// </summary>
        [DomainLabel("时间")]
        time,
        /// <summary>
        /// 一对多
        /// </summary>
        [DomainLabel("一对多")]
        one2many,
        /// <summary>
        /// 多对一
        /// </summary>
        [DomainLabel("多对一")]
        many2one,
        /// <summary>
        /// 多对多
        /// </summary>
        [DomainLabel("多对多")]
        many2many,
        /// <summary>
        /// 长文本
        /// </summary>
        [DomainLabel("文本域")]
        text,

        /// <summary>
        /// 密码
        /// </summary>
        [DomainLabel("密码")]
        password,

        /// <summary>
        /// 富文本
        /// </summary>
        [DomainLabel("富文本")]
        richtext,
        /// <summary>
        /// 二进制
        /// </summary>
        [DomainLabel("二进制")]
        binary,
        /// <summary>
        /// 文件
        /// </summary>
        [DomainLabel("文件")]
        file,
        /// <summary>
        /// 图片
        /// </summary>
        [DomainLabel("图片")]
        image
    }
    /// <summary>
    /// 变更模式
    /// </summary>
    public enum ChangeMode
    {
        /// <summary>
        /// 添加
        /// </summary>
        Add,
        /// <summary>
        /// 修改
        /// </summary>
        Update,
        /// <summary>
        /// 删除
        /// </summary>
        Remove
    }
    /// <summary>
    /// 类型
    /// </summary>
    public enum MetaMode
    {
        /// <summary>
        /// 基础对象
        /// </summary>
        [DomainLabel("内置")]
        Base,
        /// <summary>
        /// 自定义对象
        /// </summary>
        [DomainLabel("自定义")]
        Custom
    }
    /// <summary>
    /// 关系模式
    /// </summary>
    public enum RelationMode
    {
        /// <summary>
        /// 一般，删除时检测
        /// </summary>
        [DomainLabel("一般(删除时检测)")]
        General,
        /// <summary>
        /// 紧密,删除对象时同时删除关联对象
        /// </summary>
        [DomainLabel("紧密(删除时删除)")]
        Thick,
        /// <summary>
        /// 松散,不检测
        /// </summary>
        [DomainLabel("松散(删除时跳过)")]
        Loose
    }

    /// <summary>
    /// 领域对象类型
    /// </summary>
    public enum DomainType
    {
        /// <summary>
        /// 空类型
        /// </summary>
        None,
        /// <summary>
        /// 聚合根
        /// </summary>
        [DomainLabel("聚合根")]
        AggRoot,
        /// <summary>
        /// 实体
        /// </summary>
        [DomainLabel("实体")]
        Entity,

        /// <summary>
        /// 消息处理器
        /// </summary>
        [DomainLabel("消息处理器")]
        Handler,
        /// <summary>
        /// 表单对象
        /// </summary>
        [DomainLabel("表单对象")]
        Form
    }
    /// <summary>
    /// 授权目标
    /// </summary>
    public enum AuthTarget
    {
        /// <summary>
        /// 对象
        /// </summary>
        [DomainLabel("对象")]
        Model,
        /// <summary>
        /// 字段
        /// </summary>
        [DomainLabel("字段")]
        Field,
        /// <summary>
        /// 消息
        /// </summary>
        [DomainLabel("消息")]
        Message,

    }
    /// <summary>
    /// 作用于
    /// </summary>
    public enum Used : int
    {
        [DomainLabel("系统")]
        System = 0x01,
        [DomainLabel("代理商")]
        Agent = 0x02,
        [DomainLabel("供应商")]
        Supplier = 0x04,
        [DomainLabel("客户")]
        Customer = 0x08
    }

    /// <summary>
    /// 领域帮助类
    /// </summary>
    public static class DomainHelper
    {
        /// <summary>
        /// 聚合根接口类型
        /// </summary>
        static readonly Type IRoot = typeof(IRoot);
        /// <summary>
        /// 实体接口类型
        /// </summary>
        static readonly Type IEndity = typeof(IEntity);

        static readonly Type IEvent = typeof(IHandler);

        static readonly Type IWf = typeof(IForm);
        /// <summary>
        /// 根据对象类型获取领域类型
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static DomainType GetDomainType(Type type)
        {
            var intfs = type.GetInterfaces();
            if (intfs.Contains(IRoot))
                return DomainType.AggRoot;
            if (intfs.Contains(IEndity))
                return DomainType.Entity;
            if (intfs.Contains(IEvent))
                return DomainType.Handler;
            if (intfs.Contains(IWf))
                return DomainType.Form;
            return DomainType.Entity;
        }
        /// <summary>
        /// 从类型中解析 字段类型
        /// </summary>
        /// <param name="type"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public static FieldType GetFieldType(Type type)
        {
            FieldType fieldtype = FieldType.none;
            if (type.IsEnum)
            {
                fieldtype = FieldType.select;
            }
            else
                switch (Type.GetTypeCode(type))
                {
                    case TypeCode.String: fieldtype = FieldType.str; break;
                    case TypeCode.SByte:
                    case TypeCode.Byte:
                    case TypeCode.Int16:
                    case TypeCode.UInt16:
                    case TypeCode.Int32:
                    case TypeCode.UInt32:
                    case TypeCode.Int64:
                    case TypeCode.UInt64: fieldtype = FieldType.digits; break;
                    case TypeCode.Single:
                    case TypeCode.Double:
                    case TypeCode.Decimal: fieldtype = FieldType.number; break;
                    case TypeCode.Boolean: fieldtype = FieldType.bollean; break;
                    case TypeCode.DateTime: fieldtype = FieldType.date; break;
                    case TypeCode.Object:
                        if (type.Name == "Guid")
                            fieldtype = FieldType.uniqueidentifier;
                        else if (type.Name == "Byte[]")
                            fieldtype = FieldType.binary;
                        else if (type.IsGenericType)
                            fieldtype = FieldType.one2many;
                        else if (GetDomainType(type) == DomainType.AggRoot)
                            fieldtype = FieldType.many2one;
                        break;
                }
            return fieldtype;
        }

        /// <summary>
        /// 从字段类型中解析大小
        /// </summary>
        /// <param name="fieldtype"></param>
        /// <param name="ptype"></param>
        /// <returns></returns>
        public static int GetSize(FieldType fieldtype, Type ptype)
        {
            int m_size = 0;
            switch (fieldtype)
            {
                case FieldType.password:
                case FieldType.str:
                case FieldType.select: m_size = 128; break;
                case FieldType.file:
                case FieldType.image: m_size = 256; break;
                case FieldType.time: m_size = 8; break;
                case FieldType.datemonth: m_size = 6; break;
                case FieldType.richtext: m_size = 2048; break;
                case FieldType.text: m_size = 512; break;
                case FieldType.digits:
                case FieldType.number:
                    switch (Type.GetTypeCode(Nullable.GetUnderlyingType(ptype) ?? ptype))
                    {
                        case TypeCode.SByte:
                        case TypeCode.Byte: m_size = 8; break;
                        case TypeCode.Int16:
                        case TypeCode.UInt16: m_size = 16; break;
                        case TypeCode.Int32:
                        case TypeCode.UInt32: m_size = 32; break;
                        case TypeCode.Int64:
                        case TypeCode.UInt64: m_size = 64; break;
                        case TypeCode.Single: m_size = 32; break;
                        case TypeCode.Double: m_size = 64; break;
                        case TypeCode.Decimal: m_size = 128; break;
                    }
                    break;
                default: break;
            }
            return m_size;
        }

        /// <summary>
        /// 获取字段精度
        /// </summary>
        /// <param name="fieldtype"></param>
        /// <param name="ptype"></param>
        /// <returns></returns>
        public static int GetPrecision(FieldType fieldtype, Type ptype)
        {
            int precision = 0;
            if (fieldtype == FieldType.number)
            {
                precision = 2;
            }
            return precision;
        }
        /// <summary>
        /// 判断字段是否值类型
        /// </summary>
        /// <param name="fieldtype"></param>
        /// <returns></returns>
        public static bool isValueType(this FieldType fieldtype)
        {
            return fieldtype == FieldType.number
                    || fieldtype == FieldType.digits
                    || fieldtype == FieldType.uniqueidentifier
                    || fieldtype == FieldType.date
                    || fieldtype == FieldType.datetime
                    || fieldtype == FieldType.bollean;
        }
        /// <summary>
        /// 获取字段的类型
        /// </summary>
        /// <param name="fieldtype"></param>
        /// <param name="size"></param>
        /// <param name="relationMeta"></param>
        /// <param name="asentity">将聚合根作为实体使用</param>
        /// <returns></returns>
        public static Type GetType(FieldType fieldtype, int size, DomainModel relationMeta, bool asentity = false)
        {
            switch (fieldtype)
            {
                case FieldType.str:
                case FieldType.text:
                case FieldType.richtext:
                case FieldType.password:
                case FieldType.time:
                case FieldType.datemonth:
                case FieldType.select:
                case FieldType.file:
                case FieldType.image:
                    return typeof(string);
                case FieldType.digits:
                    if (size == 8)
                        return typeof(byte);
                    if (size == 16)
                        return typeof(short);
                    if (size == 32)
                        return typeof(int);
                    if (size == 64)
                        return typeof(long);
                    return typeof(int);
                case FieldType.number:
                    if (size == 32)
                        return typeof(float);
                    if (size == 64)
                        return typeof(double);
                    if (size == 128)
                        return typeof(decimal);
                    return typeof(float);
                case FieldType.datetime:
                case FieldType.date:
                    return typeof(DateTime);
                case FieldType.bollean:
                    return typeof(Boolean);
                case FieldType.binary:
                    return typeof(byte[]);
                case FieldType.uniqueidentifier:
                    return typeof(Guid);
                case FieldType.one2many:
                    if (relationMeta != null)
                    {
                        if (relationMeta.ObjType == DomainType.Entity || (relationMeta.ObjType == DomainType.AggRoot && asentity))
                            return typeof(EntityCollection<>).MakeGenericType(relationMeta.ModelType);
                        return typeof(AggregateCollection<>).MakeGenericType(relationMeta.ModelType);
                    }
                    return null;
                case FieldType.many2many:
                    if (relationMeta != null)
                        return typeof(AggregateCollection<>).MakeGenericType(relationMeta.ModelType);
                    return null;
                case FieldType.many2one:
                    return relationMeta == null ? null : relationMeta.ModelType;
            }
            return null;
        }
        /// <summary>
        /// 获取字段的类型
        /// </summary>
        /// <param name="fieldtype"></param>
        /// <param name="size"></param>
        /// <param name="relationmodel"></param>
        /// <returns></returns>
        public static Type GetType(FieldType fieldtype, int size = 0, string relationmodel = "")
        {
            DomainModel relationMeta = null;
            if (!string.IsNullOrEmpty(relationmodel))
                relationMeta = MetaEngine.GetModel(relationmodel);
            return GetType(fieldtype, size, relationMeta);
        }
    }
}
