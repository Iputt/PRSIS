using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace PS.Auxiliary.Verification
{
    /// <summary>
    /// 对称加密
    /// </summary>
    public class AESService
    {
        /// <summary>
        /// AES加密
        /// </summary>
        /// <param name="str"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string AesEncrypt(string str, string key)
        {
            string result;
            try
            {
                if (string.IsNullOrEmpty(str))
                {
                    result = null;
                }
                else
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(str);
                    RijndaelManaged rijndaelManaged = new RijndaelManaged
                    {
                        Key = Encoding.UTF8.GetBytes(key),
                        Mode = CipherMode.ECB,
                        Padding = PaddingMode.PKCS7
                    };
                    ICryptoTransform cryptoTransform = rijndaelManaged.CreateEncryptor();
                    byte[] array = cryptoTransform.TransformFinalBlock(bytes, 0, bytes.Length);
                    result = Convert.ToBase64String(array, 0, array.Length);

                    return result;
                }
            }
            catch (Exception ex)
            {
                result = null;
                System.Console.WriteLine(ex);
            }
            return result;
        }

        /// <summary>
        /// AES解密
        /// </summary>
        /// <param name="str"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string AesDecrypt(string str, string key)
        {
            if (string.IsNullOrEmpty(str))
            {
                return null;
            }
            string result;
            try
            {
                byte[] array = Convert.FromBase64String(str);

                RijndaelManaged rijndaelManaged = new RijndaelManaged
                {
                    Key = Encoding.UTF8.GetBytes(key),
                    Mode = CipherMode.ECB,
                    Padding = PaddingMode.PKCS7
                };
                ICryptoTransform cryptoTransform = rijndaelManaged.CreateDecryptor();
                byte[] bytes = cryptoTransform.TransformFinalBlock(array, 0, array.Length);
                result = Encoding.UTF8.GetString(bytes);
            }
            catch (Exception ex)
            {
                result = null;
                Console.WriteLine(ex);
            }
            return result;
        }
    }
}
