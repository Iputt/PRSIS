using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Configuration;
using Owl.Util;
using Owl.Util.iAppConfig;
namespace Owl.Domain.Driver.Repository
{


    public class RestRepositoryProvider<TAggregateRoot> : RepositoryProvider<TAggregateRoot>
        where TAggregateRoot : AggRoot
    {
        IRestfulApi Api = RestProxy.Instance;
        //RestConfigElement Config = AppConfig.Section.GetConfig<RestConfigElement>();

        protected RestRepositoryContext Context
        {
            get
            {
                return (RestRepositoryContext)UnitOfWork.Current.GetContext(typeof(RestRepositoryContext));
            }
        }


        protected override void onInit()
        {
            //Api = RestProxy.Create(Config.Url);
            //Api.Token(Config.Login, Config.Password);
        }

        protected override void DoCreateSchema(bool force)
        {
            throw new NotImplementedException();
        }

        protected override void DoDropSchema()
        {
            throw new NotImplementedException();
        }

        protected override void DoAddColumn(string field)
        {
            throw new NotImplementedException();
        }

        protected override void DoDropCoumn(string field)
        {
            throw new NotImplementedException();
        }

        protected override void DoChangeColumn(string field, string newfield)
        {
            throw new NotImplementedException();
        }
        protected override void DoPush(TAggregateRoot entity)
        {
            Context.Push(entity);
        }
        protected override void DoRemove(TAggregateRoot entity)
        {
            Context.Remove(entity);
        }

        protected override void DoUpdateAll(Expression<Func<TAggregateRoot, bool>> expression, TransferObject dto)
        {

        }

        protected override void DoRemoveAll(Expression<Func<TAggregateRoot, bool>> expression)
        {

        }

        protected override IEnumerable<TransferObject> DoRead(Guid[] id, bool translate, string[] selector)
        {
            return Api.Read(MetaData.Name, id, selector);
        }
        string Translate(Expression<Func<TAggregateRoot, bool>> expression)
        {
            if (expression == null)
                return "";
            var spec = new SpecifactionTranslater().Translate(expression);
            return spec == null ? "" : spec.ToString();
        }
        protected override IEnumerable<TransferObject> DoGetList(Expression<Func<TAggregateRoot, bool>> expression, SortBy sortby, int start, int size, bool translate, string[] selector)
        {
            return Api.GetList(MetaData.Name, Translate(expression), sortby, start, size, selector);
        }

        protected override TAggregateRoot DoFindById(Guid Id, string[] selector)
        {
            return Api.FindById(MetaData.Name, Id) as TAggregateRoot;
        }

        protected override TAggregateRoot DoFindFirst(Expression<Func<TAggregateRoot, bool>> expression, SortBy sortby, string[] selector)
        {
            return Api.FindFirst(MetaData.Name, Translate(expression)) as TAggregateRoot;
        }

        protected override IEnumerable<TAggregateRoot> DoFindAll(Expression<Func<TAggregateRoot, bool>> expression, SortBy sortby, int start, int count, string[] selector)
        {
            return Api.FindAll(MetaData.Name, Translate(expression), sortby, start, count, selector).Cast<TAggregateRoot>();
        }

        protected override bool DoExists(Expression<Func<TAggregateRoot, bool>> expression)
        {
            return Api.Exists(MetaData.Name, Translate(expression));
        }

        protected override int DoCount(Expression<Func<TAggregateRoot, bool>> expression, string[] groupselector)
        {
            return Api.Count(MetaData.Name, Translate(expression));
        }

        protected override IDictionary<string, object> DoSum(Expression<Func<TAggregateRoot, bool>> expression, string[] selector)
        {
            return Api.Sum(MetaData.Name, Translate(expression), selector);
        }

        protected override IEnumerable<TransferObject> DoGroupBy(Expression<Func<TAggregateRoot, bool>> expression, IEnumerable<string> keySelector, IEnumerable<ResultSelector> resultSelector, SortBy sortby, int start, int count, bool translate)
        {
            throw new NotImplementedException();
        }
        protected override object DoInvoke(string name, IDictionary<string, object> param)
        {
            throw new NotImplementedException();
        }
    }
}
