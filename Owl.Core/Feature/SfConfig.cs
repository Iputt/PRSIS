using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Feature.Impl.Config;
namespace Owl.Feature
{
    /// <summary>
    /// 自定义配置参数
    /// </summary>
    public class DbSfConfig : Engine<ConfigProvider, DbSfConfig>
    {
        /// <summary>
        /// 获取自定义参数
        /// </summary>
        /// <param name="topcode"></param>
        /// <param name="code"></param>
        /// <param name="param">默认值</param>
        /// <returns></returns>
        public static string GetSetting(string topcode, string code, string param = null)
        {
            return Execute2<string, string, string, string>(s => s.GetSetting, topcode, code, param);
        }
    }
}
