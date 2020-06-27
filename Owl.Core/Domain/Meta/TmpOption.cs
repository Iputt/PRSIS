using Owl.Domain.Driver.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Owl.Feature;
namespace Owl.Domain.Temp
{
    [DomainModel(NoTable = true)]
    [IgnoreBak]
    [Authorize(Permission.Read)]
    public class TmpSelectOption : AggRoot
    {
        [DomainField(Label = "序号", Default = 1)]
        public int Ordinal { get; set; }

        [DomainField(Label = "代码", AutoSearch = true)]
        public override string Code { get; set; }

        [DomainField(Label = "名称", AutoSearch = true)]
        public virtual string Name { get; set; }

        [DomainField(Label = "标签", Size = 512, AutoSearch = true)]
        public string Tags { get; set; }

        [DomainField(Label = "列表", NonPublic = true)]
        public string Select { get; set; }


        [DomainField(Label = "上联选项", NonPublic = true)]
        public string TopCode { get; set; }

        [DomainField(Label = "上联选项2", NonPublic = true)]
        public string TopCode2 { get; set; }

        [DomainField(Label = "上联选项3", NonPublic = true)]
        public string TopCode3 { get; set; }

        [DomainField(Label = "上联选项4", NonPublic = true)]
        public string TopCode4 { get; set; }

        [DomainField(Label = "上联选项5", NonPublic = true)]
        public string TopCode5 { get; set; }
    }

    public class OptionRepositoryProvider<TOption> : RepositoryProvider<TOption>
        where TOption : TmpSelectOption
    {
        #region
        protected override void DoPush(TOption root)
        {

        }

        protected override void DoAddColumn(string field)
        {

        }

        protected override void DoChangeColumn(string field, string newfield)
        {

        }

        protected override void DoCreateSchema(bool force)
        {

        }

        protected override void DoDropCoumn(string field)
        {

        }

        protected override void DoDropSchema()
        {

        }

        protected override void DoRemove(TOption root)
        {

        }

        protected override void DoRemoveAll(Expression<Func<TOption, bool>> expression)
        {

        }

        protected override IDictionary<string, object> DoSum(Expression<Func<TOption, bool>> expression, string[] selector)
        {
            throw new NotImplementedException();
        }

        protected override void DoUpdateAll(Expression<Func<TOption, bool>> expression, TransferObject dto)
        {

        }
        protected override object DoInvoke(string name, IDictionary<string, object> param)
        {
            throw new NotImplementedException();
        }
        #endregion




        protected override bool DoExists(Expression<Func<TOption, bool>> expression)
        {
            throw new NotImplementedException();
        }

        protected override IEnumerable<TOption> DoFindAll(Expression<Func<TOption, bool>> expression, SortBy sortby, int start, int count, string[] selector)
        {
            throw new NotImplementedException();
        }

        protected override TOption DoFindById(Guid Id, string[] selector)
        {
            throw new NotImplementedException();
        }

        protected override TOption DoFindFirst(Expression<Func<TOption, bool>> expression, SortBy sortby, string[] selector)
        {
            throw new NotImplementedException();
        }

        string GetParamValue(string paramname, Specification spec, CacheKey ckey)
        {
            var result = "";
            var index = spec.Members.LastIndexOf(paramname);
            if (index > 0)
                result = string.Format("{0}", ckey.Parameters.Get(string.Format("p{0}", index)).Value);
            return result;
        }
        protected IEnumerable<TransferObject> GetItems(Expression<Func<TOption, bool>> expression)
        {
            var spec = new SpecifactionTranslater().Translate(expression);

            var ckey = new CacheKey(expression);
            var selectname = spec.Context.GetRealValue<string>("Select");
            var code = GetParamValue("Code", spec, ckey);
            var term = GetParamValue("Name", spec, ckey);

            var tag = GetParamValue("Tags", spec, ckey);
            List<string> topvalue = new List<string>();
            foreach (var key in spec.Context.Keys.Where(s => s.StartsWith("TopCode")).OrderBy(s => s))
            {
                topvalue.Add(spec.Context.GetRealValue<string>(key));
            }
            var modelname = "";
            if (MsgContext.Current.Message != null)
                modelname = MsgContext.Current.Message.ModelName;

            string cachekey = string.Format("{0}{1}{2}{3}{4}{5}", modelname, selectname, string.Join(",", topvalue), code, term, tag);
            var result = Cache.Thread<IEnumerable<TransferObject>>(cachekey);
            if (result == null)
            {
                var items = Select.GetSelect(null, selectname, topvalue.ToArray(), term, true).GetItems();
                var dtos = new List<TransferObject>();
                var ordinal = 1;
                foreach (var item in items)
                {
                    var dto = new TransferObject();
                    var dtoClone = new TransferObject();
                    dto["Id"] = item.Value;
                    dtoClone["Id"] = item.Value;
                    dto["Select"] = selectname;
                    dtoClone["Select"] = selectname;
                    dto["Code"] = item.Value;
                    dtoClone["Code"] = item.Value == null ? null : item.Value.ToLower();
                    dto["Name"] = item.Text;
                    dtoClone["Name"] = item.Text == null ? null : item.Text.ToLower();
                    dto["Tags"] = item.Description;
                    dtoClone["Tags"] = item.Description == null ? null : item.Description.ToLower();
                    foreach (var extra in item.Extra)
                    {
                        dto[extra.Key] = extra.Value;
                        dtoClone[extra.Key] = extra.Value == null ? null : extra.Value.ToString().ToLower();
                    }
                    for (var i = 1; i <= topvalue.Count; i++)
                    {
                        var topkey = string.Format("TopCode{0}", i == 1 ? "" : i.ToString());
                        dto[topkey] = topvalue[i - 1];
                        dtoClone[topkey] = topvalue[i - 1];
                    }
                    if (spec.IsValid(dtoClone))
                    {
                        dto["Ordinal"] = ordinal;
                        ordinal = ordinal + 1;
                        dtos.Add(dto);
                    }
                }
                result = dtos;
                Cache.Thread(cachekey, result);
            }
            return result;
        }

        protected override int DoCount(Expression<Func<TOption, bool>> expression, string[] groupselector)
        {
            return GetItems(expression).Count();
        }
        protected override IEnumerable<TransferObject> DoGetList(Expression<Func<TOption, bool>> expression, SortBy sortby, int start, int size, bool translate, string[] selector)
        {
            List<TransferObject> result = new List<TransferObject>();
            var items = GetItems(expression);
            if (sortby != null && sortby.Count == 1)
            {
                var sort = sortby.FirstOrDefault();
                if (sort.Value == SortOrder.Descending)
                    items = items.OrderByDescending(s => s.GetDisplay(sort.Key));
            }
            return items.Skip(start).Take(size);
        }

        protected override IEnumerable<TransferObject> DoGroupBy(Expression<Func<TOption, bool>> expression, IEnumerable<string> keySelector, IEnumerable<ResultSelector> resultSelector, SortBy sortby, int start, int count, bool translate)
        {
            throw new NotImplementedException();
        }




        protected override IEnumerable<TransferObject> DoRead(Guid[] id, bool translate, string[] selector)
        {
            throw new NotImplementedException();
        }


        protected override void onInit()
        {

        }
    }
    public class RegisterTmpRepository
    {
        [OnApplicatonPrepare]
        public static void RegisterRepository()
        {
            ObjectContainer.Instance.Register(typeof(RepositoryProvider<>), typeof(OptionRepositoryProvider<>), "tmpselect");
            ObjectContainer.Instance.RegisterGenericResolveMap(typeof(RepositoryProvider<>), typeof(TmpSelectOption), "tmpselect");
        }
    }
}
