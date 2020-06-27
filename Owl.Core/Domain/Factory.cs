using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Util;
namespace Owl.Domain
{
    /// <summary>
    /// 领域对象工厂
    /// </summary>
    public abstract class DomainFactory
    {
        static Dictionary<string, DomainFactory> instances = new Dictionary<string, DomainFactory>(100);

        protected static DomainFactory GetInstance(string model)
        {
            if (!instances.ContainsKey(model))
            {
                var metadata = ModelMetadataEngine.GetModel(model);
                var type = metadata.ModelType;
                if (!type.IsSubclassOf(typeof(DomainObject)))
                    throw new Exception2("the factory can not create this type object");
                var factory = Activator.CreateInstance(typeof(DomainFactory<>).MakeGenericType(type)) as DomainFactory;
                factory.Metadata = metadata;
                factory.Invoker = FastInvoker.GetInstance(metadata.ModelType);
                instances[model] = factory;
                return factory;
            }
            return instances[model];
        }

        protected ModelMetadata Metadata { get; set; }
         internal FastInvoker Invoker { get; set; }

        protected abstract DomainObject Create();

        /// <summary>
        /// 根据名称创建对象
        /// </summary>
        /// <param name="modelname">对象名称</param>
        /// <returns></returns>
        public static DomainObject Create(string modelname)
        {
            return Create(modelname, null, false);
        }

        /// <summary>
        /// 创建对象并赋值
        /// </summary>
        /// <param name="modelname">对象名称</param>
        /// <param name="dto">值</param>
        /// <returns></returns>
        public static DomainObject Create(string modelname, TransferObject dto)
        {
            return Create(modelname, dto, false);
        }

        /// <summary>
        /// 创建对象并赋缺省值
        /// </summary>
        /// <param name="modelname">对象名称</param>
        /// <param name="_default">是否赋缺省值</param>
        /// <returns></returns>
        public static DomainObject Create(string modelname, bool _default)
        {
            return Create(modelname, null, _default);
        }

        /// <summary>
        /// 创建对象
        /// </summary>
        /// <param name="modelname">对象名称</param>
        /// <param name="dto">初始值</param>
        /// <param name="_default">是否用缺省值</param>
        /// <returns></returns>
        public static DomainObject Create(string modelname, TransferObject dto, bool _default)
        {
            var obj = GetInstance(modelname).Create();
            if (_default)
            {
                if (dto == null)
                    dto = new TransferObject();
                foreach (var field in obj.Metadata.GetFields())
                {
                    if (!dto.ContainsKey(field.Name))
                    {
                        var tmp = field.Default;
                        if (tmp != null)
                            dto[field.Name] = tmp;
                    }
                }
            }
            if (dto != null)
                obj.Write(dto);
            return obj;
        }

        public static DomainObject Create(ModelMetadata metadata)
        {
            return Create(metadata, null, false);
        }

        public static DomainObject Create(ModelMetadata metadata, TransferObject dto)
        {
            return Create(metadata, dto, false);
        }

        public static DomainObject Create(ModelMetadata metadata, bool _default)
        {
            return Create(metadata, null, _default);
        }

        /// <summary>
        /// 创建对象
        /// </summary>
        /// <param name="metadata">对象元数据</param>
        /// <param name="dto">初始化数据</param>
        /// <param name="_default">是否用缺省值</param>
        /// <returns></returns>
        public static DomainObject Create(ModelMetadata metadata, TransferObject dto, bool _default)
        {
            if (metadata == null)
                return null;
            return Create(metadata.Name, dto, _default);
        }

        public static T Create<T>()
            where T : DomainObject
        {
            return Create<T>((TransferObject)null, false);
        }

        public static T Create<T>(TransferObject dto)
            where T : DomainObject
        {
            return Create<T>(dto, false);
        }
        public static T Create<T>(bool _default)
            where T : DomainObject
        {
            return Create<T>((TransferObject)null, _default);
        }
        /// <summary>
        /// 根据类型创建领域对象
        /// </summary>
        /// <typeparam name="T">返回类型</typeparam>
        /// <param name="dto">初始化值</param>
        /// <param name="_default">是否用缺省值</param>
        /// <returns></returns>
        public static T Create<T>(TransferObject dto, bool _default)
            where T : DomainObject
        {
            return (T)Create(typeof(T).MetaName(), dto, _default);
        }

        public static T Create<T>(ModelMetadata metadata)
            where T : DomainObject
        {
            return Create<T>(metadata, null, false);
        }

        public static T Create<T>(ModelMetadata metadata, TransferObject dto)
            where T : DomainObject
        {
            return Create<T>(metadata, dto, false);
        }

        public static T Create<T>(ModelMetadata metadata, bool _default)
            where T : DomainObject
        {
            return Create<T>(metadata, null, _default);
        }
        /// <summary>
        /// 根据类型创建领域对象
        /// </summary>
        /// <typeparam name="T">返回类型</typeparam>
        /// <param name="metadata">元数据</param>
        /// <param name="dto">初始化数据</param>
        /// <param name="_default">是否启用缺省数据</param>
        /// <returns></returns>
        public static T Create<T>(ModelMetadata metadata, TransferObject dto, bool _default)
            where T : DomainObject
        {
            return (T)Create(metadata == null ? typeof(T).MetaName() : metadata.Name, dto, _default);
        }

        public static T Create<T>(string modelname)
            where T : DomainObject
        {
            return Create<T>(modelname, null, false);
        }

        public static T Create<T>(string modelname, TransferObject dto)
            where T : DomainObject
        {
            return Create<T>(modelname, dto, false);
        }

        public static T Create<T>(string modelname, bool _default)
            where T : DomainObject
        {
            return Create<T>(modelname, null, _default);
        }

        public static T Create<T>(string modelname, TransferObject dto, bool _default)
            where T : DomainObject
        {
            return (T)Create(string.IsNullOrEmpty(modelname) ? typeof(T).MetaName() : modelname, dto, _default);
        }
    }
    /// <summary>
    /// 领域对象工厂
    /// </summary>
    /// <typeparam name="TObj"></typeparam>
    internal sealed class DomainFactory<TObj> : DomainFactory
        where TObj : DomainObject, new()
    {
        protected override DomainObject Create()
        {
            var obj = new TObj();
            obj.Metadata = Metadata;
            obj.SetInvoker(Invoker);
            return obj;
        }
    }

}
