using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PS.API.Extension
{
    /// <summary>
    /// appsettings.json配置文件帮助类
    /// </summary>
    public static class ConfigHelper
    {
        private static IConfiguration config { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        static ConfigHelper()
        {
            var builder = new ConfigurationBuilder();//创建config的builder
            builder.SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json");//设置配置文件所在的路径加载配置文件信息
            config = builder.Build();
        }

        /// <summary>
        /// 根据key获取对应的配置值
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string GetSection(string key)
        {
            return config[key];
        }

        /// <summary>
        /// 获取ConnectionStrings下默认的配置连接字符串
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string GetConnectionString(string key)
        {
            return config.GetConnectionString(key);
        }

        /// <summary>
        /// appsettings.json 子节点转实体
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">节点名称</param>
        /// <returns></returns>
        public static T GetSectionModel<T>(string key) where T : new()
        {
            var model = new T();
            config.GetSection(key).Bind(model);
            return model;
        }

        /// <summary>
        /// 从appsettings.json获取key的值
        /// 取RabbitMQ下的HostName的值，则参数key为 RabbitMQ:HostName
        /// </summary>
        /// <param name="key">key</param>
        /// <returns></returns>
        public static string GetSubValue(string key)
        {
            var rr = config.GetSection(key).GetChildren();

            return config[key];
        }
    }
}
