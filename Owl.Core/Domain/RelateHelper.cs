using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
namespace Owl.Domain
{
    public static class RelateHelper
    {

        static IEnumerable<TRoot> LoadNav<TRoot>(this IEnumerable<TRoot> roots, NavigatField navfield)
            where TRoot : AggRoot
        {
            if (roots == null || roots.Count() == 0 || navfield == null)
                return roots;
            var meta = navfield.Metadata;
            var member = navfield.Name;
            if (navfield.Field_Type == FieldType.many2one)
            {
                var m2ofield = navfield as Many2OneField;
                var keys = roots.Select(s => (Guid)s[m2ofield.GetFieldname()]).Distinct();
                var relas = Repository.FindAll(m2ofield.RelationModelMeta, Specification.Create("Id", CmpCode.IN, keys)).ToDictionary(s => s.Id);
                foreach (var root in roots)
                {
                    var rkey = (Guid)root[m2ofield.GetFieldname()];
                    root[member] = relas.ContainsKey(rkey) ? relas[rkey] : null;
                }
            }
            else if (navfield.Field_Type == FieldType.one2many)
            {
                var o2mfile = navfield as One2ManyField;
                if (o2mfile.PrimaryField == meta.PrimaryField.Name)
                {
                    var keys = roots.Select(s => s.Id);
                    var relas = keys.Slice(0, 200)
                        .SelectMany(s => Repository.FindAll(o2mfile.RelationModelMeta, Specification.Create(o2mfile.RelationField, CmpCode.IN, s)))
                        .GroupBy(s => (Guid)s[o2mfile.RelationField]).ToDictionary(s => s.Key);
                    foreach (var root in roots)
                    {
                        var end = root[member] as RelatedEnd;
                        if (relas.ContainsKey(root.Id))
                            end.Initialize(relas[root.Id]);
                        else
                            end.Initialize();
                    }
                }
            }
            return roots;
        }

        /// <summary>
        /// 批量加载关系数据
        /// </summary>
        /// <typeparam name="TRoot">根类型</typeparam>
        /// <typeparam name="TProperty">关系类型</typeparam>
        /// <param name="roots"></param>
        /// <param name="proerty"></param>
        public static IEnumerable<TRoot> LoadNav<TRoot, TProperty>(this IEnumerable<TRoot> roots,
            Expression<Func<TRoot, TProperty>> proerty)
            where TRoot : AggRoot
        {
            if (roots == null || roots.Count() == 0)
                return roots;
            var meta = roots.FirstOrDefault().Metadata;
            var member = (proerty.Body as MemberExpression).Member.Name;
            var navfield = meta.GetField(member) as NavigatField;
            if (navfield == null)
                return roots;
            return LoadNav(roots, navfield);
        }
        /// <summary>
        /// 批量加载关系数据
        /// </summary>
        /// <typeparam name="TRoot">根类型</typeparam>
        /// <param name="roots"></param>
        /// <param name="fields"></param>
        public static void LoadNav<TRoot>(this IEnumerable<TRoot> roots, params string[] fields)
            where TRoot : AggRoot
        {
            if (roots == null || roots.Count() == 0 || fields == null || fields.Length == 0)
                return;
            var meta = roots.FirstOrDefault().Metadata;
            foreach (var field in fields)
            {
                var navfield = meta.GetField(field) as NavigatField;
                if (navfield != null)
                    LoadNav(roots, navfield);
            }
        }
    }
}
