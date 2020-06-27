using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
namespace Owl.Util
{
    public static class Crypto
    {
        public static string ToHex(byte[] hash, bool ignorezero = true)
        {
            StringBuilder result = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                var d = hash[i].ToString("x");
                if (!ignorezero)
                    d = hash[i].ToString("x").PadLeft(2, '0');
                result.Append(d);
            }
            return result.ToString();
        }

        /// <summary>
        /// md5加密
        /// </summary>
        /// <param name="md5str">待加密的字符串</param>
        /// <returns></returns>
        public static string Md5(string md5str, bool ignorezero = true, Encoding encoding = null)
        {
            var provider = new MD5CryptoServiceProvider();
            if (encoding == null)
                encoding = Encoding.Unicode;
            var hash = provider.ComputeHash(encoding.GetBytes(md5str));
            return ToHex(hash, ignorezero);
        }
        /// <summary>
        /// SHAs the 256 hex.
        /// </summary>
        /// <returns>The 256 hex.</returns>
        /// <param name="str">String.</param>
        /// <param name="ignorezero">If set to <c>true</c> ignorezero.</param>
        public static string SHA256Hex(string str, bool ignorezero = true)
        {
            SHA256 s256 = new SHA256Managed();
            byte[] byte1;
            byte1 = s256.ComputeHash(Encoding.UTF8.GetBytes(str));
            s256.Clear();
            return ToHex(byte1, ignorezero);
        }
    }
}
