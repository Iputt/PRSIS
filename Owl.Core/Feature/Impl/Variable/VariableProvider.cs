using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
namespace Owl.Feature.Impl.Variable
{
    public abstract class VariableProvider : Provider
    {
        public abstract bool Contain(string key);
        public abstract object GetValue(string parameter);
    }
}
