using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Domain;
namespace Owl.Feature.Impl.Select
{
    public class SelectEngine : Engine<SelectProvicer, SelectEngine>
    {
        /// <summary>
        /// 获取选择项
        /// </summary>
        /// <param name="name"></param>
        /// <param name="topvalue"></param>
        /// <returns></returns>
        public static ListOptionCollection GetSelect(string name, string term, string[] topvalue, bool all = false)
        {
            if (name.StartsWith("@"))
            {
                name = name.Substring(1);
                ListOptionCollection collect = new ListOptionCollection();
                foreach (var provider in Providers)
                {
                    var tmp = provider.GetSelect(name, term, topvalue);
                    if (tmp != null)
                    {
                        collect.Merge(tmp);
                    }
                }
                return collect;
            }
            return Execute2<string, string, string[], bool, ListOptionCollection>(s => s.GetSelect, name, term, topvalue, all);
        }


    }
}
