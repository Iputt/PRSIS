using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Owl.Feature.iImport
{
    public abstract class ImportMapProvider : Provider
    {
        public abstract Dictionary<string, string> GetMap(string modelname, string name);
    }
}
