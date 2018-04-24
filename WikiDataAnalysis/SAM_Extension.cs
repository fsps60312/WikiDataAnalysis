using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;

namespace WikiDataAnalysis
{
    partial class SAM
    {
        class NodeData
        {
            public List<Node> children = new List<Node>();
            public int count,frequency;
        }
        partial class Node
        {
            public void BuildTree(List<Node> nodes)
            {
                dfs_counter++;
                if (data != null) return;
                nodes.Add(this);
                data = new NodeData();
                if (edges_large == null)
                {
                    foreach (var p in edges_small) p.Item2.BuildTree(nodes);
                }
                else
                {
                    foreach (var p in edges_large) p.Value.BuildTree(nodes);
                }
            }
        }
        public bool Exist(string s)
        {
            return Traverse(s) != null;
        }
        public int Count(string s)
        {
            return Traverse(s)?.data.count ?? -1;
        }
        static long dfs_counter = 0;
        void BuildTree()
        {
            List<Node> nodes = new List<Node>();
            ROOT.BuildTree(nodes);
            Trace.Assert(nodes[0] == ROOT);
            nodes.RemoveAt(0);
            foreach (var o in nodes)
            {
                o.green.data.children.Add(o);
                //Debug.WriteLine($"{o.id} -> {o.green.id}" +
                //    $", children: {string.Join(",",o.data.children.Select(v=>v.id.ToString()))}" +
                //    $", edge: {string.Join(",",o.edge.Select(v=>v.Item1.ToString()))}");
                if (o.was_last!=-1)
                {
                    o.data.count = 1;
                }
            }
        }
        void Dfs1(Node o)
        {
            dfs_counter++;
            foreach (Node nxt in o.data.children)
            {
                Dfs1(nxt);
                o.data.count += nxt.data.count;
            }
        }
        void Dfs2(Node o,List<Tuple<int,Node>>list)
        {
            dfs_counter++;
            o.data.frequency = o.data.count;
            foreach(Node nxt in o.data.children)
            {
                Dfs2(nxt,list);
                o.data.frequency -= nxt.data.frequency;
            }
            list.Add(new Tuple<int, Node>(o.data.frequency, o));
        }
        void BuildCount()
        {
            Dfs1(ROOT);
        }
        string GetWord(Node o)
        {
            int len = o.max_len;
            while (o.was_last == -1) o = o.data.children[0];
            int idx = o.was_last;
            return S.Substring(idx - len + 1, len);
        }
        void BuildStatistic()
        {
            var list = new List<Tuple<int, Node>>();
            Dfs2(ROOT, list);
            list.Sort(new Comparison<Tuple<int, Node>>((a, b) => { return a.Item1 == b.Item1 ? 0 : (a.Item1 < b.Item1 ? 1 : -1); }));
            for (int i = 0; i < 20 && i < list.Count; i++)
            {
                Trace.WriteLine($"{GetWord(list[i].Item2)}: \t{list[i].Item1}");
            }
        }
        public void Build()
        {
            Thread thread = new Thread(() =>
              {
                  while (true)
                  {
                      Thread.Sleep(5000);
                      UpdateStatus($"{dfs_counter} operations done");
                  }
              });
            new Thread(new ThreadStart(() =>
            {
                thread.Start();
                UpdateStatus("BuildTree...");
                dfs_counter = 0;
                BuildTree();
                UpdateStatus("BuildCount...");
                dfs_counter = 0;
                BuildCount();
                UpdateStatus("BuildStatistic...");
                dfs_counter = 0;
                BuildStatistic();
                thread.Abort();
                UpdateStatus("Done");
            }), 2147483647).Start();
        }
    }
}
