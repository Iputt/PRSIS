using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic;
using System.Text;

namespace Owl
{
    /// <summary>
    /// 返回类型
    /// </summary>
    public enum ResType
    {
        /// <summary>
        /// 成功
        /// </summary>
        success,
        /// <summary>
        /// 未验证通过
        /// </summary>
        unauthenticate,
        /// <summary>
        /// 未授权通过
        /// </summary>
        unauthorize,

        /// <summary>
        /// 发生异常
        /// </summary>
        error,

        /// <summary>
        /// 提示信息
        /// </summary>
        info,

        /// <summary>
        /// 警告
        /// </summary>
        warning
    }
    public class Response
    {
        /// <summary>
        /// 返回类型
        /// </summary>
        public string type { get; private set; }

        /// <summary>
        /// 返回消息
        /// </summary>
        public string message { get; private set; }
        /// <summary>
        /// 是否刷新页面
        /// </summary>
        public bool refresh { get; private set; }

        /// <summary>
        /// 结果类型 字符串或值类型为 simple 其他为类型名称小写
        /// </summary>
        public string datatype { get; private set; }
        /// <summary>
        /// 返回结果
        /// </summary>
        public object data { get; private set; }

        public Response(ResType type, string message, object result)
        {
            this.type = type.ToString();
            this.message = message;
            data = result;
            if (result != null)
            {
                var dtype = result.GetType();
                if (dtype.Name == "String" || dtype.IsValueType)
                    datatype = "simple";
                else
                    datatype = dtype.Name.ToLower();
            }
        }
        public Response(ResType type, bool _refresh, string message, object result)
            : this(type, message, result)
        {
            refresh = _refresh;
        }
        public void setDatatype(string datatype)
        {
            this.datatype = datatype;
        }
    }
}
