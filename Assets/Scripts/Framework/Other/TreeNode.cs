using System;
using System.Collections.Generic;

namespace SkierFramework
{
    public class TreeNode
    {
        public Dictionary<ulong, TreeNode> childs;
        public ulong id;
        public object data;

        public TreeNode Get(ulong id)
        {
            if (childs == null)
            {
                return null;
            }
            childs.TryGetValue(id, out var node);
            return node;
        }

        public TreeNode GetOrAdd(ulong id)
        {
            if (childs == null)
            {
                childs = new Dictionary<ulong, TreeNode>();
            }
            if (!childs.TryGetValue(id, out var node))
            {
                node = ObjectPool<TreeNode>.Get();
                node.id = id;
                childs.Add(id, node);
            }
            return node;
        }

        public void CleanUp()
        {
            if (childs != null)
            {
                foreach (var item in childs.Values)
                {
                    item.CleanUp();
                    ObjectPool<TreeNode>.Release(item);
                }
                childs.Clear();
            }
            id = 0;
            data = default;
        }
    }
}
