using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Owl.Util;
using Owl.Domain.Driver;
using Owl.Domain;


namespace Owl.Feature.Assignments
{
    public class AssignEntry
    {
        public string Key { get; set; }

        public string Title { get; set; }

        public string Rank { get; set; }

        public int Percent { get; set; }

    }

    public class AssignStatistic
    {
        public string Model { get; set; }

        public int Count { get; set; }

        List<AssignEntry> entries;
        public List<AssignEntry> Entries
        {
            get
            {
                if (entries == null)
                    entries = new List<AssignEntry>();
                return entries;
            }
            set { entries = value; }
        }
    }
    /// <summary>
    /// 任务分配器
    /// </summary>
    public abstract class AssignmentProvider : Provider
    {
        /// <summary>
        /// 获取所有任务别
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public abstract IEnumerable<Category> GetCategories();

        /// <summary>
        /// 根据Id获取任务类别
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public abstract Category GetCategory(Guid id);


        public abstract List<AggRoot> Filter(AggRoot[] roots, bool autodone);

        /// <summary>
        /// 创建指定对象的任务
        /// </summary>
        /// <param name="category">任务类别</param>
        /// <param name="root">对象</param>
        public abstract void Create(Category category, AggRoot root, string assignto = null, string summary = null);

        /// <summary>
        /// Statistic this instance.
        /// </summary>
        public abstract AssignStatistic Statistic(string login);
    }
}
