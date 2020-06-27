using System;
using Owl.Util;
using Owl.Feature.ExtraSearch;
using System.Collections.Generic;
using Owl.Domain;
using System.Linq;

namespace Owl.Feature
{
    public class ExtraSearchEngine : Engine<ExtraSearchProvider, ExtraSearchEngine>
    {
        public static IEnumerable<SearchResult> Search(string term)
        {
            List<SearchResult> results = new List<SearchResult>();
            var confs = Execute3<SearchConf>(s => s.GetSearchConf);
            foreach (var conf in confs.OrderBy(s => s.Priority))
            {
                if (results.Count >= 100)
                    break;
                foreach (var root in Repository.GetList(conf.Meta, conf.GetExp(term), new SortBy() { { conf.SortBy, conf.SortOrder } }, 0, 100 - results.Count, true, conf.GetSelector()))
                {
                    var result = new SearchResult()
                    {
                        Model = conf.Model,
                        Id = root.GetRealValue<Guid>("Id"),
                        Tag = conf.GetTag(root),
                        Summary = conf.GetSummary(root)
                    };
                    results.Add(result);
                }
            }
            return results;
        }
    }
}

