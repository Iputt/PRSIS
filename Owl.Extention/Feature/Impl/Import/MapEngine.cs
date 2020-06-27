using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Owl.Feature.iImport
{
    internal class MapEngine : Engine<ImportMapProvider,MapEngine>
    {
        static readonly Func<ImportMapProvider, string, string, Dictionary<string, string>> getmap = (s, a, a2) => s.GetMap(a, a2);

        public static Dictionary<string, string> GetMap(string modelname, string name)
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            foreach (var provider in Providers)
            {
                try
                {
                    foreach (var pair in provider.GetMap(modelname, name))
                    {
                        if (dict.ContainsKey(pair.Key))
                            continue;
                        dict[pair.Key] = pair.Value;
                    }
                }
                catch
                { }
            }
            return dict;
        }
    }
}
