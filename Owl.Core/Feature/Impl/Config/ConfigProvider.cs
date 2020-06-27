using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Domain;
namespace Owl.Feature.Impl.Config
{
    public abstract class ConfigProvider : Provider
    {
        public abstract string GetSetting(string topcode, string code, string param = null);
    }
}
