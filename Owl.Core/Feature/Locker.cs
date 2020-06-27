using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Owl.Feature
{
    internal class ReadLocker : IDisposable
    {
        ReaderWriterLockSlim m_Locker;
        bool locked = false;
        public ReadLocker(ReaderWriterLockSlim locker)
        {
            if (locker != null)
            {
                m_Locker = locker;
                locked = m_Locker.TryEnterUpgradeableReadLock(-1);
            }
        }
        public void Dispose()
        {
            if (locked)
                m_Locker.ExitUpgradeableReadLock();
        }
    }

    internal class WriteLocker : IDisposable
    {
        ReaderWriterLockSlim m_Locker;
        bool locked = false;
        public WriteLocker(ReaderWriterLockSlim locker)
        {
            if (locker != null)
            {
                m_Locker = locker;
                locked = m_Locker.TryEnterWriteLock(-1);
            }
        }
        public void Dispose()
        {
            if (locked)
                m_Locker.ExitWriteLock();
        }
    }
    /// <summary>
    /// 分布式锁
    /// </summary>
    public class DisLocker : IDisposable
    {
        string m_Key;
        /// <summary>
        /// 分布式锁
        /// </summary>
        /// <param name="key">锁名称</param>
        /// <param name="expire">过期时间</param>
        public DisLocker(string key, TimeSpan? expire = null)
        {
            m_Key = key;
            if (expire == null)
                expire = TimeSpan.FromSeconds(60);
            while (!Cache.Outer.SetNE(key, "locker", expire))
            {
                Thread.Sleep(50);
            }
        }
        public void Dispose()
        {
            Cache.Outer.KeyRemove(m_Key);
        }
    }

    /// <summary>
    /// 系统锁
    /// </summary>
    public static class Locker
    {
        static Dictionary<int, ReaderWriterLockSlim> lockers = new Dictionary<int, ReaderWriterLockSlim>();

        static ReaderWriterLockSlim GetLocker(object key)
        {
            var tmp = key.GetHashCode();
            if (!lockers.ContainsKey(tmp))
                lockers[tmp] = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
            return lockers[tmp];
        }

        public static void Remove(object key)
        {
            lockers.Remove(key.GetHashCode());
        }

        /// <summary>
        /// 创建新锁
        /// </summary>
        /// <returns></returns>
        public static ReaderWriterLockSlim Create()
        {
            return new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        }

        /// <summary>
        /// 锁定资源的读取并返回并返回解锁对象
        /// </summary>
        /// <param name="key">资源的Key值</param>
        /// <returns></returns>
        public static IDisposable LockRead(object key)
        {
            return new ReadLocker(GetLocker(key));
        }
        /// <summary>
        /// 锁定资源的读取并返回解锁对象
        /// </summary>
        /// <param name="locker">锁</param>
        /// <returns></returns>
        public static IDisposable LockRead(ReaderWriterLockSlim locker)
        {
            return new ReadLocker(locker);
        }

        /// <summary>
        /// 锁定资源的写入并返回解锁对象
        /// </summary>
        /// <param name="key">资源的key值</param>
        /// <returns></returns>
        public static IDisposable LockWrite(object key)
        {
            return new WriteLocker(GetLocker(key));
        }

        /// <summary>
        /// 锁定资源的写入并返回解锁对象
        /// </summary>
        /// <param name="locker">锁</param>
        /// <returns></returns>
        public static IDisposable LockWrite(ReaderWriterLockSlim locker)
        {
            return new WriteLocker(locker);
        }
        /// <summary>
        /// 锁定执行
        /// </summary>
        /// <param name="key">锁的key</param>
        /// <param name="condition">执行条件</param>
        /// <param name="action">执行体</param>
        public static void SafeExecute(object key, Func<bool> condition, Action action)
        {
            if (condition())
            {
                lock (key)
                {
                    if (condition())
                    {
                        action();
                    }
                }
            }
        }
    }
}
