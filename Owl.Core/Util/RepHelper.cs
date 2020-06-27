using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Owl.Util
{
    /// <summary>
    /// 仓储帮助类
    /// </summary>
    public static class RepHelper
    {
        /// <summary>
        /// 获取总页数
        /// </summary>
        /// <param name="total"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public static int GetPage(int total, int size)
        {
            return total / size + (total % size == 0 ? 0 : 1);
        }
    }
}
