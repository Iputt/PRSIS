using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Domain;
using Owl.Domain.Driver;
namespace Owl.Util
{
    public class ModelDump
    {
        /// <summary>
        /// 备份对象
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="modify">修改时间</param>
        /// <returns></returns>
        public static IEnumerable<SmartModel> Dump<T>(DateTime? modify)
            where T : AggRoot
        {
            var roots = Repository<T>.Where(s => true);
            if (modify != null)
                roots = roots.Where(s => s.Modified >= modify.Value);
            return roots.ToList().Select(s => SmartModel.FromDomain(s));
        }

        public static IEnumerable<SmartModel> Dump(string modelname, DateTime? modify)
        {
            var meta = ModelMetadataEngine.GetModel(modelname);
            Specification spec = null;
            if (modify.HasValue)
                spec = Specification.Create("Modified", CmpCode.GTE, modify.Value);
            var roots = Repository.FindAll(meta, spec == null ? null : spec.GetExpression(meta)).ToList();
            return roots.Select(s => SmartModel.FromDomain(s));
        }

        public static void Restore(IEnumerable<SmartModel> models)
        {
            foreach (var model in models)
            {
                var meta = ModelMetadataEngine.GetModel(model.__ModelName__);
                AggRoot root = Repository.FindById(meta, model.Id);
                if (root == null)
                    root = DomainFactory.Create<AggRoot>(meta);
                root.Write(model);
                root.Push();
            }
            DomainContext.Current.Commit();
        }
    }
}
