using Owl.Feature;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Owl.Domain
{
    /// <summary>
    /// 仓储运行时
    /// </summary>
    public class RepositoryRunning
    {
        private static readonly string runningkey = "Owl.Domain.RepositoryRunning";

        /// <summary>
        /// 获取当前运行时
        /// </summary>
        /// <param name="create"></param>
        /// <returns></returns>
        static RepositoryRunning GetCurrent(bool create)
        {
            var current = Cache.Thread<RepositoryRunning>(runningkey);
            if (current == null)
            {
                if (create)
                {
                    current = new RepositoryRunning();
                    Cache.Thread(runningkey, current);
                }
            }
            return current;
        }

        private bool m_nolock;
        private bool querydeleted;
        private bool m_readonly;

        /// <summary>
        /// 是否查询已经逻辑删除的对象
        /// </summary>
        public static bool QueryDeleted
        {
            get
            {
                var current = GetCurrent(false);
                return current == null ? false : current.querydeleted;
            }
            set
            {
                var current = GetCurrent(true);
                if (current != null)
                    current.querydeleted = value;
            }
        }

        /// <summary>
        /// 不加锁查询，可以读取被事务锁定的数据，也称为脏读。
        /// </summary>
        public static bool NoLock
        {
            get
            {
                var current = GetCurrent(false);
                return current == null ? false : current.m_nolock;
            }
            set
            {
                var current = GetCurrent(true);
                if (current != null)
                    current.m_nolock = value;
            }
        }

        /// <summary>
        /// 是否是仅读取，用于读写分离
        /// </summary>
        public static bool Readonly
        {
            get
            {
                var current = GetCurrent(false);
                return current == null ? false : current.m_readonly;
            }
            set {
                var current = GetCurrent(true);
                current.m_readonly = value;
            }
        }
    }
}
