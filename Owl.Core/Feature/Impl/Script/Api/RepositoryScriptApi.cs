using Owl.Domain;
using Owl.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Owl.Feature.iScript.Api
{
    /// <summary>
    /// 数据操作api
    /// </summary>
    internal class RepositoryApi : ScriptRuntimeApi
    {
        public override string Name
        {
            get
            {
                return "repo";
            }
        }
        protected override int Priority
        {
            get { return 1; }
        }
        protected SortBy GetSortby(IDictionary<string, string> sortby)
        {
            var result = new SortBy();
            if (sortby != null)
            {
                foreach (var pair in sortby)
                {
                    switch (pair.Value.ToLower())
                    {
                        case "asc":
                        case "ascending":
                            result[pair.Key] = SortOrder.Ascending;
                            break;
                        case "desc":
                        case "descending":
                            result[pair.Key] = SortOrder.Descending;
                            break;
                        default:
                            result[pair.Key] = SortOrder.Unspecified;
                            break;
                    }
                }
            }
            return result;
        }

        protected LambdaExpression GetExp(ModelMetadata meta, string specification)
        {
            return Repository.GetExpression(meta, specification);
        }

        /// <summary>
        /// 获取指定id的数据
        /// </summary>
        /// <param name="modelname"></param>
        /// <param name="id"></param>
        /// <param name="selector"></param>
        /// <returns></returns>
        public AggRoot Find(string modelname, Guid id, string[] selector)
        {
            return Repository.FindById(modelname, id, selector);
        }

        public AggRoot Find(string modelname, Guid id)
        {
            return Repository.FindById(modelname, id);
        }

        /// <summary>
        /// 获取符合条件的第一条数据
        /// </summary>
        /// <param name="modelname"></param>
        /// <param name="specification">条件或Id</param>
        /// <param name="selector"></param>
        /// <returns></returns>
        public AggRoot Find(string modelname, string specification, string[] selector)
        {
            Guid Id;
            if (Guid.TryParse(specification, out Id))
                return Repository.FindById(modelname, Id, selector);
            var meta = ModelMetadata.GetModel(modelname);
            if (meta == null)
                throw new AlertException("对象{0}不存在", modelname);
            return Repository.FindFirst(meta, GetExp(meta, specification), selector);
        }

        public AggRoot Find(string modelname, string specification)
        {
            return Find(modelname, specification, new string[0]);
        }
        /// <summary>
        /// 获取符合条件的第一条数据
        /// </summary>
        /// <param name="modelname"></param>
        /// <param name="specification"></param>
        /// <param name="sortby"></param>
        /// <param name="selector"></param>
        /// <returns></returns>
        public AggRoot Find(string modelname, string specification, IDictionary<string, string> sortby, string[] selector)
        {
            return Repository.FindFirst(modelname, specification, GetSortby(sortby), selector);
        }

        public AggRoot Find(string modelname, string specification, IDictionary<string, string> sortby)
        {
            return Repository.FindFirst(modelname, specification, GetSortby(sortby));
        }

        /// <summary>
        /// 获取符合条件的所有数据
        /// </summary>
        /// <param name="modelname"></param>
        /// <param name="specification"></param>
        /// <param name="selector"></param>
        /// <returns></returns>
        public IEnumerable<AggRoot> FindAll(string modelname, string specification, string[] selector)
        {
            return Repository.FindAll(modelname, specification, null, 0, 0, selector);
        }
        public IEnumerable<AggRoot> FindAll(string modelname, string specification)
        {
            var roots = Repository.FindAll(modelname, specification, null, 0, 0);
            return roots;
        }
        /// <summary>
        /// 获取符合条件的所有数据
        /// </summary>
        /// <param name="modelname"></param>
        /// <param name="specification"></param>
        /// <param name="selector"></param>
        /// <returns></returns>
        public IEnumerable<AggRoot> FindAll(string modelname, string specification, IDictionary<string, string> sortby, int start, int end, string[] selector)
        {
            return Repository.FindAll(modelname, specification, GetSortby(sortby), start, end, selector);
        }

        public IEnumerable<AggRoot> FindAll(string modelname, string specification, IDictionary<string, string> sortby, int start, int end)
        {
            return Repository.FindAll(modelname, specification, GetSortby(sortby), start, end);
        }

        /// <summary>
        /// 查询数量
        /// </summary>
        /// <param name="modelname"></param>
        /// <param name="specification"></param>
        /// <returns></returns>
        public int Count(string modelname, string specification)
        {
            var meta = ModelMetadata.GetModel(modelname);
            return Repository.Count(meta, GetExp(meta, specification));
        }
        /// <summary>
        /// 获取汇总
        /// </summary>
        /// <param name="modelname"></param>
        /// <param name="specification"></param>
        /// <param name="selector"></param>
        /// <returns></returns>
        public TransferObject Sum(string modelname, string specification, string[] selector)
        {
            var meta = ModelMetadata.GetModel(modelname);
            return new TransferObject(Repository.Sum(meta, GetExp(meta, specification), selector));
        }

        /// <summary>
        /// 判断条件是否有数据存在
        /// </summary>
        /// <param name="modelname"></param>
        /// <param name="specification"></param>
        /// <returns></returns>
        public bool Exists(string modelname, string specification)
        {
            return Repository.Exists(modelname, specification);
        }
        /// <summary>
        /// 查询分组
        /// </summary>
        /// <param name="modelname"></param>
        /// <param name="specification"></param>
        /// <param name="keySelector"></param>
        /// <param name="resultSelector"></param>
        /// <param name="sortby"></param>
        /// <param name="start"></param>
        /// <param name="count"></param>
        /// <param name="translate"></param>
        /// <returns></returns>
        public IEnumerable<TransferObject> GroupBy(string modelname, string specification, IEnumerable<string> keySelector, IEnumerable<IDictionary<string, string>> resultSelector, IDictionary<string, string> sortby, int start, int count, bool translate)
        {
            var meta = ModelMetadata.GetModel(modelname);
            List<ResultSelector> rselectors = new List<ResultSelector>();
            foreach (var rselector in resultSelector)
            {
                string name = "", selector = "", mode = "";
                foreach (var pair in rselector)
                {
                    switch (pair.Key.ToLower())
                    {
                        case "name": name = pair.Value; break;
                        case "selector": selector = pair.Value; break;
                        case "mode": mode = pair.Value; break;
                    }

                    rselectors.Add(new ResultSelector(name, selector.Coalesce(name), EnumHelper.Parse<ResultMode>(mode)));
                }
            }
            return Repository.GroupBy(meta, GetExp(meta, specification), keySelector, rselectors, GetSortby(sortby), start, count, translate);
        }
        /// <summary>
        /// 执行存储过程
        /// </summary>
        /// <param name="modelname"></param>
        /// <param name="name"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public object Invoke(string modelname, string name, IDictionary<string, object> param)
        {
            return Repository.Invoke(ModelMetadata.GetModel(modelname), name, param);
        }

        public void UpdateAll(string modelname, string specification, IDictionary<string, object> fieldvalues)
        {
            var meta = ModelMetadata.GetModel(modelname);
            DomainContext.Current.Host(() =>
            {
                Repository.UpdateAll(meta, GetExp(meta, specification), new TransferObject(fieldvalues));
            });
        }
    }
}
