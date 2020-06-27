using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Owl.Domain
{
    /// <summary>
    /// 树状结构
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class TreeNode<T>
        where T : Object2
    {
        /// <summary>
        /// 当前节点在树中的级别
        /// </summary>
        public int Level { get; private set; }

        /// <summary>
        /// 当前节点在同级节点中的序号
        /// </summary>
        public int Index { get; private set; }

        /// <summary>
        /// 编号
        /// </summary>
        public string Number { get; private set; }

        void BuildNumber()
        {
            Number = string.Format("{0}_{1}", Parent.Number, Index);
            foreach (var child in Children)
                child.BuildNumber();
        }

        /// <summary>
        /// 节点包含的对象
        /// </summary>
        public T Object { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="parent"></param>
        public TreeNode(T obj, TreeNode<T> parent, string prefix = "tree")
        {
            Object = obj;
            m_children = new List<TreeNode<T>>();
            if (parent == null)
            {
                Level = 0;
                Index = 0;
                Number = string.Format("{0}_0", prefix);
            }
            else
            {
                m_parent = parent;
                m_parent.AddChild(this);
            }
        }

        public TreeNode(T obj, string prefix = "tree")
            : this(obj, null, prefix)
        {

        }

        TreeNode<T> m_parent;
        /// <summary>
        /// 上级节点
        /// </summary>
        [IgnoreField]
        public TreeNode<T> Parent
        {
            get { return m_parent; }
        }

        List<TreeNode<T>> m_children;
        /// <summary>
        /// 子节点
        /// </summary>
        public IEnumerable<TreeNode<T>> Children
        {
            get
            {
                return m_children;
            }
        }

        public void AddChild(TreeNode<T> node)
        {
            node.m_parent = this;
            node.Level = Level + 1;
            node.Index = m_children.Count;
            node.BuildNumber();
            m_children.Add(node);
        }
        public void RemoveChild(TreeNode<T> node)
        {
            m_children.Remove(node);
        }
        /// <summary>
        /// 添加子节点
        /// </summary>
        /// <param name="obj"></param>
        public TreeNode<T> Append(T obj)
        {
            return new TreeNode<T>(obj, this);
        }

        public TransferObject ToDto()
        {
            var dto = new TransferObject();
            dto.Write(Object);
            dto["id"] = Number;
            if (m_children.Count > 0)
            {
                var children = new List<TransferObject>();
                dto["children"] = children;
                foreach (var child in Children)
                {
                    children.Add(child.ToDto());
                }
            }
            return dto;
        }
    }
}
