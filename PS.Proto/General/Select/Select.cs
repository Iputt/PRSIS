using System;
using System.Collections.Generic;
using System.Text;

namespace PS.Proto
{
    /// <summary>
    /// 选项列表
    /// </summary>
    public class SelectOptionList
    {
        /// <summary>
        /// 选项 - 拓展
        /// </summary>
        //public SelectOption Option { get; set; }

        /// <summary>
        /// 选项
        /// </summary>
        public KeyValuePair<string,string> sOption { get; set; }
    }

    /// <summary>
    /// 选项 - 拓展
    /// </summary>
    public class SelectOption
    {
        public string Code { get; set; }

        public string Key { get; set; }

        public string Value { get; set; }
    }
}
