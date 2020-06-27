using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Owl.Feature.Impl.Cache
{
    public interface ICache
    {
        /// <summary>
        /// 缓存是否存在
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        bool KeyExists(string key);

        /// <summary>
        /// 删除缓存
        /// </summary>
        /// <param name="key"></param>
        void KeyRemove(string key);

        /// <summary>
        /// 设置缓存的过期时间
        /// </summary>
        /// <param name="key"></param>
        /// <param name="expire">过期时间，为空则永不过期</param>
        void KeyExpire(string key, TimeSpan? expire = null);

        /// <summary>
        /// 设置缓存并更新缓存项的过期时间
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="expire">过期时间，为空则永不过期</param>
        void Set(string key, object value, TimeSpan? expire = null);

        /// <summary>
        /// 当缓存项不存在时设置缓存，同时更新过期时间
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="expire"></param>
        /// <returns></returns>
        bool SetNE(string key, object value, TimeSpan? expire = null);
        /// <summary>
        /// 获取缓存
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        object Get(string key);

        /// <summary>
        /// 设置指定的值并返回旧值，如果旧值不存在则返回空，可用于判断资源是否经过初始化
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        object GetSet(string key, object value);

        /// <summary>
        /// 设置hash类型的缓存
        /// </summary>
        /// <param name="key"></param>
        /// <param name="field"></param>
        /// <param name="value"></param>
        void HashSet(string key, string field, object value);

        /// <summary>
        /// 获取hash表类型的缓存中的一项
        /// </summary>
        /// <param name="key"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        object HashGet(string key, string field);

        /// <summary>
        /// 删除hash表中中的项
        /// </summary>
        /// <param name="key"></param>
        /// <param name="async"></param>
        void HashDelete(string key,string field, bool async);
        /// <summary>
        /// 获取hash表所有项
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        Hashtable HashGetAll(string key);
        /// <summary>
        /// 数据增量
        /// </summary>
        /// <param name="key">key</param>
        /// <param name="value">增长</param>
        /// <returns></returns>
        long Increment(string key, long value = 1);

        /// <summary>
        /// 向队列开头添加项
        /// </summary>
        /// <param name="key">队列名</param>
        /// <param name="value"></param>
        void ListLeftPush(string key,params object[] value);
        /// <summary>
        /// 删除队列开头的数据并返回
        /// </summary>
        /// <param name="key">队列名称</param>
        /// <returns></returns>
        object ListLeftPop(string key);

        /// <summary>
        /// 向队列结尾添加项
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        void ListRightPush(string key, params object[] value);

        /// <summary>
        /// 删除队列结尾的数据并返回
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        object ListRightPop(string key);

        /// <summary>
        /// 获取队列的子集
        /// </summary>
        /// <param name="key"></param>
        /// <param name="start"></param>
        /// <param name="end">-1表示队列结尾</param>
        /// <returns></returns>
        IEnumerable<object> ListRange(string key, int start = 0, int end = -1);
    }
}
