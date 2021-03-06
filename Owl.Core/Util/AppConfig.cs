﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Xml;
using System.IO;
using Owl.Util.iAppConfig;
using System.Collections.Specialized;

namespace Owl.Util
{
    /// <summary>
    /// 应用程序配置
    /// </summary>
    public static class AppConfig
    {
        /// <summary>
        /// 应用程序配置节
        /// </summary>
        public static OwlConfigSection Section = OwlConfigSection.Current;


        /// <summary>
        /// 根据Key 获取设置 的值
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string GetSetting(string key, string _default = "")
        {
            return ConfigurationManager.AppSettings.Get(key).Coalesce(_default);
        }

        /// <summary>
        /// 获取连接字符串
        /// </summary>
        /// <param name="connectionname"></param>
        /// <param name="rwsplit">读写分离开关</param>
        /// <returns></returns>
        public static string GetConnectionString(string connectionname, bool rwsplit = false)
        {
            ConnectionStringSettings connection = null;
            if (rwsplit && Domain.RepositoryRunning.Readonly)
            {
                connection = ConfigurationManager.ConnectionStrings[string.Format("{0}read", connectionname)];
            }
            if (connection == null)
                connection = ConfigurationManager.ConnectionStrings[connectionname];
            if (connection != null)
                return connection.ConnectionString;
            return "";
        }
    }
}
