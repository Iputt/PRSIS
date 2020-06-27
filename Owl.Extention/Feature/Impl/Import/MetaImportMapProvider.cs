using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Domain;

namespace Owl.Feature.iImport
{
    public  class MetaImportMapProvider : ImportMapProvider
    {
        public override Dictionary<string, string> GetMap(string modelname, string name)
        {
            if (!string.IsNullOrEmpty(name))
                return null;
            var meta = MetaEngine.GetModel(modelname);
            Dictionary<string, string> map = new Dictionary<string, string>();
            foreach (var field in meta.GetFields())
            {
                map[field.GetLabel()] = field.Name;
            }
            return map;
        }

        public override int Priority
        {
            get { return 1; }
        }
    }
}
