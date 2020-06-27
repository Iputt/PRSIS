using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Owl.Util
{
    public class ExceptionHelper
    {
        /// <summary>
        /// 构建异常
        /// </summary>
        /// <param name="resource">资源</param>
        /// <param name="format">缺省格式</param>
        /// <param name="args">参数</param>
        public static Exception Build(string resource, string format, params object[] args)
        {
            return new AlertException(resource, format, args);
        }
    }
}
