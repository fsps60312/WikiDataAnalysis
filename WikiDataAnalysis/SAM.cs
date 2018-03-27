using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;

namespace WikiDataAnalysis
{
    //class SAM
    //{
    //    class Node
    //    {
    //        public Node green;
    //        public List<Node> edge = new List<Node>();
    //        public int max_len;
    //        public Node(int _max_len)
    //        {
    //            green = null;
    //            max_len = _max_len;
    //            for (int i = 0; i < 26; i++) edge.Add(null);
    //        }
    //    }
    //    Node ROOT, LAST;
    //    public void Extend(int c)
    //    {
    //        Node cursor = LAST; LAST = new Node((LAST.max_len) + 1);
    //        for (; cursor != null && cursor.edge[c] == null; cursor = cursor.green) cursor.edge[c] = LAST;//添加LAST所有的黑色字串 
    //        if (cursor == null) LAST.green = ROOT;//其實圖上沒有畫綠色邊的點，代表著它的綠色邊是直接指向「代表空串的根結點」
    //        else
    //        {
    //            Node potential_green = cursor.edge[c];//找出最長的綠色字串(為了要讓LAST代表所有後綴組成的字串集合，要決定綠色邊連到哪)，在圖上會走到哪個節點 
    //            if ((potential_green.max_len) == (cursor.max_len + 1)) LAST.green = potential_green;//剛剛好potential_green代表的字串全部都是LAST的後綴，可以直接利用綠色邊連到potential_green，添加LAST所有的綠色字串 
    //            else
    //            {
    //                Debug.Assert((potential_green.max_len) > (cursor.max_len + 1));//potential_green代表的字串集合中有些不是LAST的後綴
    //                Node wish = new Node((cursor.max_len) + 1);//從potential_green分離出想要的節點，恰好代表LAST所有的綠色字串 
    //                for (; cursor != null && cursor.edge[c] == potential_green; cursor = cursor.green) cursor.edge[c] = wish;//添加wish所有的黑色字串，同時可能搶走部分potential_green代表的字串集合 
    //                for (int i = 0; i < 26; i++) wish.edge[i] = potential_green.edge[i];//讓wish接管原本potential_green黑色邊的功能(防止potential_green下游的節點代表的字串集合中的一些黑色字串，因為potential_green丟掉一些黑色字串而遺失)
    //                wish.green = potential_green.green;//利用綠色邊添加wish所有的綠色字串 
    //                potential_green.green = wish;//利用綠色邊修復potential_green代表的字串集合 
    //                LAST.green = wish;//利用綠色邊添加LAST所有的綠色字串 
    //            }
    //        }
    //    }
    //    public void Initialize()
    //    {
    //        ROOT = LAST = new Node(0);//SAM的根結點代表空串，max_len當然是0 
    //    }
    //    public void Extend(string s)
    //    {
    //        for (int i = 0; i < s.Length; i++) Extend(s[i]-'a');
    //    }
    //    public bool Exist(string s)
    //    {
    //        Node cursor = ROOT;
    //        for (int i = 0; i < s.Length; i++)
    //        {
    //            cursor = cursor.edge[s[i] - 'a'];
    //            if (cursor == null) return false;//圖上沒有路可以走了，A不是S的子字串 
    //        }
    //        return true;
    //    }
    //}
    partial class SAM
    {
        class NodeData
        {
            public List<Node> children = new List<Node>();
            public int count;
        }
        public bool Exist(string s)
        {
            return Traverse(s) != null;
        }
        public int Count(string s)
        {
            return Traverse(s)?.data.count??-1;
        }
        void BuildTree(Node o,List<Node>nodes)
        {
            if (o.data != null) return;
            if(o!=ROOT)nodes.Add(o);
            o.data = new NodeData();
            foreach (var p in o.edge)
            {
                BuildTree(p.Item2,nodes);
            }
        }
        void BuildTree()
        {
            List<Node> nodes=new List<Node>();
            BuildTree(ROOT,nodes);
            foreach (var o in nodes)
            {
                o.green.data.children.Add(o);
                //Debug.WriteLine($"{o.id} -> {o.green.id}" +
                //    $", children: {string.Join(",",o.data.children.Select(v=>v.id.ToString()))}" +
                //    $", edge: {string.Join(",",o.edge.Select(v=>v.Item1.ToString()))}");
                if(o.was_last)o.data.count = 1;
            }
        }
        void Dfs(Node o)
        {
            foreach (Node nxt in o.data.children)
            {
                Dfs(nxt);
                o.data.count += nxt.data.count;
            }
        }
        void BuildCount()
        {
            Dfs(ROOT);
        }
        public void Build()
        {
            Thread thread = new Thread(new ThreadStart(() =>
              {
                  BuildTree();
                  BuildCount();
                  UpdateStatus("Done");
              }), 2147483647);
            thread.Start();
        }
    }
    partial class SAM
    {
        static int id_counter = 0;
        class Node
        {
            public NodeData data;
            public Node green;
            public List<Tuple<char, Node>> edge = new List<Tuple<char, Node>>();
            public int max_len,id=++id_counter;
            public bool was_last = false;
            public Node(int _max_len)
            {
                green = null;
                max_len = _max_len;
            }
        }
        private bool ContainsKey(List<Tuple<char, Node>>s,char c)
        {
            foreach (var v in s) if (v.Item1 == c) return true;
            return false;
        }
        private void Set(List<Tuple<char, Node>> s, char c,Node o)
        {
            for(int i=0;i<s.Count;i++)
            {
                if(s[i].Item1==c)
                {
                    s[i] = new Tuple<char, Node>(c, o);
                    return;
                }
            }
            s.Add(new Tuple<char, Node>(c, o));
        }
        private Node Get(List<Tuple<char, Node>> s, char c)
        {
            foreach (var v in s) if (v.Item1 == c) return v.Item2;
            return null;
        }
        Node ROOT, LAST;
        long SIZE = 0;
        public void Extend(char c)
        {
            Node cursor = LAST; LAST = new Node((LAST.max_len) + 1) { was_last = true }; SIZE++;
            for (; cursor != null && !ContainsKey(cursor.edge, c); cursor = cursor.green) Set(cursor.edge,c, LAST);//添加LAST所有的黑色字串 
            if (cursor == null) LAST.green = ROOT;//其實圖上沒有畫綠色邊的點，代表著它的綠色邊是直接指向「代表空串的根結點」
            else
            {
                Node potential_green = Get(cursor.edge, c);//找出最長的綠色字串(為了要讓LAST代表所有後綴組成的字串集合，要決定綠色邊連到哪)，在圖上會走到哪個節點 
                if ((potential_green.max_len) == (cursor.max_len + 1)) LAST.green = potential_green;//剛剛好potential_green代表的字串全部都是LAST的後綴，可以直接利用綠色邊連到potential_green，添加LAST所有的綠色字串 
                else
                {
                    Debug.Assert((potential_green.max_len) > (cursor.max_len + 1));//potential_green代表的字串集合中有些不是LAST的後綴
                    Node wish = new Node((cursor.max_len) + 1); SIZE++;//從potential_green分離出想要的節點，恰好代表LAST所有的綠色字串 
                    for (; cursor != null && Get(cursor.edge, c) == potential_green; cursor = cursor.green)Set(cursor.edge,c, wish);//添加wish所有的黑色字串，同時可能搶走部分potential_green代表的字串集合 
                    foreach (var i in potential_green.edge) Set(wish.edge, i.Item1, i.Item2);//讓wish接管原本potential_green黑色邊的功能(防止potential_green下游的節點代表的字串集合中的一些黑色字串，因為potential_green丟掉一些黑色字串而遺失)
                    wish.green = potential_green.green;//利用綠色邊添加wish所有的綠色字串 
                    potential_green.green = wish;//利用綠色邊修復potential_green代表的字串集合 
                    LAST.green = wish;//利用綠色邊添加LAST所有的綠色字串 
                }
            }
        }
        public void Initialize()
        {
            id_counter = 0;
            ROOT = LAST = new Node(0);SIZE=1;//SAM的根結點代表空串，max_len當然是0 
        }
        public delegate void StringEventHandler(string s);
        public event StringEventHandler StatusChanged;
        public void Extend(string s)
        {
            DateTime start = DateTime.Now;
            for (int i = 0; i < s.Length; i++)
            {
                Extend(s[i]);
                if((DateTime.Now-start).TotalSeconds>=1)
                {
                    UpdateStatus($"Extending... {i + 1}/{s.Length}");
                    start = DateTime.Now;
                }
            }
            UpdateStatus($"SAM size: {SIZE}");
        }
        void UpdateStatus(string s)
        {
            Debug.WriteLine(s);
            StatusChanged?.Invoke(s);
            System.Windows.Forms.Application.DoEvents();
        }
        private Node Traverse(string s)
        {
            Node cursor = ROOT;
            for (int i = 0; i < s.Length; i++)
            {
                if ((cursor=Get(cursor.edge,s[i]))==null) return null;//圖上沒有路可以走了，A不是S的子字串 
            }
            Debug.WriteLine($"{s} = {cursor.id}");
            return cursor;
        }
    }
}
