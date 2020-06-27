using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Owl.Domain.Driver;
using Owl.Util;
namespace Owl.Domain.Driver.Repository
{
    /// <summary>
    /// 仓储工厂 单件模式
    /// </summary>
    public static class RepositoryProviderFactory
    {
        static Dictionary<string, RepositoryProvider> providers = new Dictionary<string, RepositoryProvider>(150);
        static Dictionary<string, RepositoryProvider> cproviders = new Dictionary<string, RepositoryProvider>(30);

        static HashSet<string> registers = new HashSet<string>();
        static void RegistChange(ModelMetadata metadata)
        {
            if (!registers.Contains(metadata.Name))
                metadata.onModelChange += new EventHandler<MetaChangeArgs>(metadata_onModelChange);
        }

        static void metadata_onModelChange(object sender, MetaChangeArgs e)
        {
            var metadata = sender as ModelMetadata;
            if (providers.ContainsKey(metadata.Name))
                providers.Remove(metadata.Name);
            lock (orglocker)
            {
                cproviders.Remove(metadata.Name);
            }
        }
        /// <summary>
        /// 创建强类型仓储提供者,有多对多关系禁止缓存
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static RepositoryProvider<T> CreateProvider<T>(ModelMetadata metadata = null) where T : AggRoot
        {
            if (metadata == null)
                metadata = ModelMetadataEngine.GetModel(typeof(T));
            RepositoryProvider<T> provider = null;
            if (metadata.IsCacheEnable)
            {
                if (cproviders.ContainsKey(metadata.Name))
                    provider = (RepositoryProvider<T>)cproviders[metadata.Name];
                else
                {
                    provider = new CachedRepositoryProvider<T>();
                    provider.Init(metadata);
                    cproviders[metadata.Name] = provider;
                    RegistChange(metadata);
                }
            }
            else
                provider = CreateOrgProvider<T>(metadata);
            return provider;
        }
        static object orglocker = new object();
        /// <summary>
        /// 创建原生仓储
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="metadata"></param>
        /// <returns></returns>
        public static RepositoryProvider<T> CreateOrgProvider<T>(ModelMetadata metadata = null)
            where T : AggRoot
        {
            if (metadata == null)
                metadata = ModelMetadataEngine.GetModel(typeof(T));
            RepositoryProvider<T> provider = null;
            if (!providers.ContainsKey(metadata.Name))
            {
                lock (orglocker)
                {
                    if (!providers.ContainsKey(metadata.Name))
                    {
                        provider = ObjectContainer.Instance.Resolve<RepositoryProvider<T>>();
                        provider.Init(metadata);
                        providers[metadata.Name] = provider;
                        RegistChange(metadata);
                    }
                }
            }
            if (provider == null)
                provider = (RepositoryProvider<T>)providers[metadata.Name];
            return provider;
        }

        public static RepositoryProvider CreateProvider(string modelname)
        {
            return CreateProvider(ModelMetadataEngine.GetModel(modelname));
        }

        public static RepositoryProvider CreateOrgProvider(ModelMetadata metadata)
        {
            //var resolvename = "";
            //if (metadata != null && (metadata.ModelType == typeof(Temp.TmpSelectOption) || metadata.ModelType.IsSubclassOf(typeof(Temp.TmpSelectOption))))
            //    resolvename = "tmpselect";
            RepositoryProvider provider = null;
            if (!providers.ContainsKey(metadata.Name))
            {
                lock (orglocker)
                {
                    if (!providers.ContainsKey(metadata.Name))
                    {
                        provider = ObjectContainer.Instance.Resolve(typeof(RepositoryProvider<>).MakeGenericType(metadata.ModelType), "") as RepositoryProvider; provider.Init(metadata);
                        providers[metadata.Name] = provider;
                        RegistChange(metadata);
                    }
                }
            }

            if (provider == null)
                provider = providers[metadata.Name];
            return provider;
        }

        public static RepositoryProvider CreateProvider(ModelMetadata metadata)
        {
            if (metadata == null)
                throw new ArgumentNullException("metadata");
            dynamic provider = null;
            if (metadata.IsCacheEnable)
            {
                if (cproviders.ContainsKey(metadata.Name))
                    provider = cproviders[metadata.Name];
                else
                {
                    provider = Activator.CreateInstance(typeof(CachedRepositoryProvider<>).MakeGenericType(metadata.ModelType));
                    provider.Init(metadata);
                    cproviders[metadata.Name] = provider;
                    RegistChange(metadata);
                }
            }
            else
            {
                provider = CreateOrgProvider(metadata);
            }
            return provider;
        }
    }
}
