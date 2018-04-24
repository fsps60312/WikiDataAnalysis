using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;

namespace WikiDataAnalysis
{
    partial class SAM
    {
        static int id_counter = 0;
        partial class Node
        {
            public const int max_edges_small_size = 10;
            public NodeData data;
            public Node green;
            Tuple<char, Node>[] edges_small = new Tuple<char, Node>[0];
            Dictionary<char, Node> edges_large = null;
            public void AddEdges(Node o)
            {
                if (o.edges_large == null)
                {
                    foreach (var p in o.edges_small) SetEdge(p.Item1, p.Item2);
                }
                else
                {
                    foreach (var p in o.edges_large) SetEdge(p.Key, p.Value);
                }
            }
            public Node GetEdge(char c)
            {
                if (edges_large == null)
                {
                    foreach (var v in edges_small) if (v.Item1 == c) return v.Item2;
                    return null;
                }
                else
                {
                    if (edges_large.TryGetValue(c, out Node result)) return result;
                    return null;
                }
            }
            public void SetEdge(char c, Node o)
            {
                if (edges_large == null)
                {
                    for (int i = 0; i < edges_small.Length; i++)
                    {
                        if (edges_small[i].Item1 == c)
                        {
                            edges_small[i] = new Tuple<char, Node>(c, o);
                            return;
                        }
                    }
                    Array.Resize(ref edges_small, edges_small.Length + 1);
                    edges_small[edges_small.Length - 1] = new Tuple<char, Node>(c, o);
                    if (edges_small.Length > max_edges_small_size)
                    {
                        edges_large = new Dictionary<char, Node>();
                        foreach (var v in edges_small) edges_large[v.Item1] = v.Item2;
                        edges_small = null;
                    }
                }
                else
                {
                    edges_large[c] = o;
                }
            }
            public int max_len, id = ++id_counter;
            public int was_last = -1;
            public Node(int _max_len)
            {
                green = null;
                max_len = _max_len;
            }
        }
        private bool ContainsKey(List<Tuple<char, Node>> s, char c)
        {
            foreach (var v in s) if (v.Item1 == c) return true;
            return false;
        }
        Node ROOT, LAST;
        long SIZE = 0;
        int INDEX=0;
        string S;
        public void Extend(char c)
        {
            Node cursor = LAST; LAST = new Node((LAST.max_len) + 1) { was_last = INDEX++ }; SIZE++;
            for (; cursor != null && cursor.GetEdge(c) == null; cursor = cursor.green) cursor.SetEdge(c, LAST);//添加LAST所有的黑色字串 
            if (cursor == null) LAST.green = ROOT;//其實圖上沒有畫綠色邊的點，代表著它的綠色邊是直接指向「代表空串的根結點」
            else
            {
                Node potential_green = cursor.GetEdge(c);//找出最長的綠色字串(為了要讓LAST代表所有後綴組成的字串集合，要決定綠色邊連到哪)，在圖上會走到哪個節點 
                if ((potential_green.max_len) == (cursor.max_len + 1)) LAST.green = potential_green;//剛剛好potential_green代表的字串全部都是LAST的後綴，可以直接利用綠色邊連到potential_green，添加LAST所有的綠色字串 
                else
                {
                    Trace.Assert((potential_green.max_len) > (cursor.max_len + 1));//potential_green代表的字串集合中有些不是LAST的後綴
                    Node wish = new Node((cursor.max_len) + 1); SIZE++;//從potential_green分離出想要的節點，恰好代表LAST所有的綠色字串 
                    for (; cursor != null && cursor.GetEdge(c) == potential_green; cursor = cursor.green) cursor.SetEdge(c, wish);//添加wish所有的黑色字串，同時可能搶走部分potential_green代表的字串集合 
                    wish.AddEdges(potential_green);//讓wish接管原本potential_green黑色邊的功能(防止potential_green下游的節點代表的字串集合中的一些黑色字串，因為potential_green丟掉一些黑色字串而遺失)
                    wish.green = potential_green.green;//利用綠色邊添加wish所有的綠色字串 
                    potential_green.green = wish;//利用綠色邊修復potential_green代表的字串集合 
                    LAST.green = wish;//利用綠色邊添加LAST所有的綠色字串 
                }
            }
        }
        public void Initialize()
        {
            id_counter = 0;
            ROOT = LAST = new Node(0); SIZE = 1;//SAM的根結點代表空串，max_len當然是0 
            S = "";
        }
        public delegate void StringEventHandler(string s);
        public event StringEventHandler StatusChanged;
        public void Extend(string s)
        {
            DateTime start = DateTime.Now;
            for (int i = 0; i < s.Length; i++)
            {
                Extend(s[i]);
                if ((DateTime.Now - start).TotalSeconds >= 1)
                {
                    UpdateStatus($"Extending... {i + 1}/{s.Length}");
                    start = DateTime.Now;
                }
            }
            S += s;
            UpdateStatus($"SAM size: {SIZE}");
        }
        void UpdateStatus(string s)
        {
            Trace.WriteLine(s);
            StatusChanged?.Invoke(s);
            System.Windows.Forms.Application.DoEvents();
        }
        private Node Traverse(string s)
        {
            Node cursor = ROOT;
            for (int i = 0; i < s.Length; i++)
            {
                if ((cursor = cursor.GetEdge(s[i])) == null) return null;//圖上沒有路可以走了，A不是S的子字串 
            }
            Trace.WriteLine($"{s} = {cursor.id}");
            return cursor;
        }
    }
}
