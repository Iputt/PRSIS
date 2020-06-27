using System;
using System.Collections.Generic;
using System.Linq;
using Owl.Util;
using Owl.Feature.Assignments;
using Owl.Domain;


namespace Owl.Feature
{
    public class AssignmentEngine : Engine<AssignmentProvider, AssignmentEngine>
    {
        protected override EngineMode Mode
        {
            get
            {
                return EngineMode.Single;
            }
        }
        /// <summary>
        /// 获取所有任务类别
        /// </summary>
        /// <param name="model">对象名称</param>
        /// <returns></returns>
        public static IEnumerable<Category> GetCategories()
        {
            return Execute3<Category>(s => s.GetCategories);
        }
        /// <summary>
        /// 根据Id获取任务类别
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static Category GetCategory(Guid id)
        {
            return Execute2<Guid, Category>(s => s.GetCategory, id);
        }

        /// <summary>
        /// Create the specified category and root.
        /// </summary>
        /// <param name="category">Category.</param>
        /// <param name="root">Root.</param>
        /// <param name="assignto"></param>
        /// <param name="summary"></param>
        protected static void Create(Category category, AggRoot root, string assignto = null, string summary = null)
        {
            if (category == null)
                return;
            Execute(s => s.Create, category, root, assignto, summary);
        }


        protected static List<AggRoot> Filter(AggRoot[] roots, bool autodone)
        {
            return Execute2<AggRoot[], bool, List<AggRoot>>(s => s.Filter, roots, autodone);
        }

        /// <summary>
        /// Handles issue for the roots
        /// </summary>
        /// <param name="roots">Roots.</param>
        public static void HandleIssue(params AggRoot[] roots)
        {
            var noissues = Filter(roots, true);
            if (noissues.Count == 0)
                return;
            var categories = GetCategories().Where(s => s.IsAuto).GroupBy(s => s.Model).ToDictionary(s => s.Key, s => s.ToList());
            if (categories.Count == 0)
                return;
            foreach (var root in noissues)
            {
                if (categories.ContainsKey(root.Metadata.Name))
                {
                    var category = categories[root.Metadata.Name].FirstOrDefault(s => s.CanTrigger(root));
                    if (category != null)
                        Create(category, root);
                }
            }
        }

        /// <summary>
        /// 创建指定的任务
        /// </summary>
        /// <param name="category">任务类别</param>
        /// <param name="assignto">分配给</param>
        /// <param name="summary">任务描述</param>
        /// <param name="roots">对象</param>
        public static void CreateIssue(Category category, string assignto = null, string summary = null, params AggRoot[] roots)
        {
            var valids = Filter(roots, false);
            foreach (var valid in valids)
            {
                Create(category, valid, assignto, summary);
            }
        }


        public static AssignStatistic Statistic()
        {
            return Execute2<string, AssignStatistic>(s => s.Statistic, Member.Current.Login);
        }
    }
}
