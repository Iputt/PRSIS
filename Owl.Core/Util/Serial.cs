using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using Owl.Feature;
using System.Threading;
namespace Owl.Util
{
    /// <summary>
    /// 序列号工具类
    /// </summary>
    public static class Serial
    {
        static readonly string letter = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
        static readonly string digits = "1234567890";
        /// <summary>
        /// 获取一个指定长度的随机不重复的序号
        /// </summary>
        /// <param name="length">序号的长度</param>
        /// <param name="hasdigit">是否包含数字</param>
        /// <returns>创建的序号</returns>
        public static string GetRandom(int length, bool hasdigit = true)
        {
            int maxSize = length;
            char[] chars;
            if (hasdigit)
                chars = (letter + digits).ToCharArray();
            else
                chars = letter.ToCharArray();
            int size = maxSize;
            byte[] data = new byte[1];
            RNGCryptoServiceProvider crypto = new RNGCryptoServiceProvider();
            crypto.GetNonZeroBytes(data);
            size = maxSize;
            data = new byte[size];
            crypto.GetNonZeroBytes(data);
            StringBuilder result = new StringBuilder(size);
            foreach (byte b in data)
            {
                result.Append(chars[b % (chars.Length - 1)]);
            }
            // Unique identifiers cannot begin with 0-9
            if (result[0] >= '0' && result[0] <= '9')
            {
                return GetRandom(length);
            }
            return result.ToString();
        }

        /// <summary>
        /// 获取递增的数字
        /// </summary>
        /// <param name="key">需要递增的key</param>
        /// <param name="init">初始值公式</param>
        /// <returns></returns>
        public static long Increment(string key, Func<long> init = null)
        {
            return Increment(key, false, 1, init);
        }

        /// <summary>
        /// 获取递增的数字
        /// </summary>
        /// <param name="key">需要递增的key</param>
        /// <param name="init">初始值公式</param>
        /// <returns></returns>
        public static long Increment(string key, long inc, Func<long> init = null)
        {
            return Increment(key, false, inc, init);
        }

        /// <summary>
        /// 获取递增的数字
        /// </summary>
        /// <param name="key">需要递增的key</param>
        /// <param name="fromback">可否从退还的数字中分配</param>
        /// <param name="init">初始值公式</param>
        /// <returns></returns>
        public static long Increment(string key, bool fromback, long inc = 1, Func<long> init = null)
        {
            var cache = Cache.Outer;
            if (fromback)
            {
                var tmp = cache.ListLeftPop(string.Format("{0}_back", key));
                if (tmp != null)
                    return Convert2.ChangeType<long>(tmp);
            }
            if (!cache.KeyExists(key) && init != null)
            {
                using (var dislock = new DisLocker(key + "_locker", TimeSpan.FromMinutes(2)))
                {
                    if (!cache.KeyExists(key))
                    {
                        cache.Increment(key, init());
                    }
                }
            }
            return cache.Increment(key, inc);
        }
        /// <summary>
        /// 将保存失败的编号退回下次分配
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void BackIncrement(string key, long value)
        {
            Cache.Outer.ListRightPush(string.Format("{0}_back", key), value);
        }

        /// <summary>
        /// 根据guid获取唯一数值
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static long GetUnique(Guid? id = null)
        {
            byte[] buffer = (id ?? Guid.NewGuid()).ToByteArray();
            return BitConverter.ToInt64(buffer,0);
        }
    }
}
