using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Util;
namespace Owl.Domain
{
    /// <summary>
    /// 模型元数据提供者
    /// </summary>
    public abstract class MetaProvider : Provider
    {
        public abstract void Init();

        protected void RegisterMeta(DomainModel model)
        {
            DomainModel.RegisterMeta(model);
        }

        protected void RemoveMeta(string name)
        {
            DomainModel.RemoveMeta(name);
        }
    }
}
