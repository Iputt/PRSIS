using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Feature;
namespace Owl.Util
{
    public class CheckHelper
    {
        /// <summary>
        /// 对资源进行签入操作
        /// </summary>
        /// <param name="key">资源的key</param>
        /// <param name="timeout">指定秒后过期</param>
        /// <returns></returns>
        public static bool CheckIn(string key, int timeout)
        {
            return Cache.Outer.SetNE(key, "checked", TimeSpan.FromSeconds(timeout));
        }

        /// <summary>
        /// 对资源进行签出操作
        /// </summary>
        /// <param name="key"></param>
        public static void CheckOut(string key)
        {
            Cache.Outer.KeyRemove(key);
        }
    }
}
