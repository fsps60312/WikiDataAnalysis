using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;

namespace WikiDataAnalysis
{
    class SuffixArray
    {
        public List<int> SA { get { return sa; } }
        public List<int> HEIGHT { get { return height; } }
        public List<int> RANK { get { return rank; } }
        public string S { get; private set; }
        List<int> sa = new List<int>(), height = new List<int>(), rank = new List<int>();
        Dictionary<char, int> GetIdx(string s)
        {
            try
            {
                Trace.Indent();
                HashSet<char> t = new HashSet<char>();
                List<char> w = new List<char>();
                Trace.WriteLine("Collecting charactors...");
                foreach (var c in s)
                {
                    if (!t.Contains(c))
                    {
                        t.Add(c);
                        w.Add(c);
                    }
                }
                Trace.WriteLine("Sorting...");
                w.Sort();
                Dictionary<char, int> idx = new Dictionary<char, int>();
                Trace.WriteLine("Almost Finish...");
                for (int i = 0; i < w.Count; i++) idx[w[i]] = i;
                Trace.Write("Done");
                return idx;
            }
            finally { Trace.Unindent(); }
        }
        void BuildSA(string s)
        {
            try
            {
                Trace.Indent();
                Trace.WriteLine("First setup for sa");
                int n = s.Length;
                var idx = GetIdx(s);
                int w = idx.Count;
                Trace.WriteLine("Making arrays");
                int[] c = new int[n];
                Trace.Write(".");
                int[] x = new int[n];
                Trace.Write(".");
                int[] y = new int[n];
                Trace.Write("|");
                sa.Resize(n, 0);
                for (int i = 0; i < w; i++) c[i] = 0;
                Trace.Write(".");
                for (int i = 0; i < n; i++) c[x[i] = idx[s[i]]]++;
                Trace.Write(".");
                for (int i = 1; i < w; i++) c[i] += c[i - 1];
                Trace.Write(".");
                for (int i = n - 1; i >= 0; i--) sa[--c[x[i]]] = i;
                Trace.Write(".");
                for (int move = 1; move <= n; move <<= 1)
                {
                    Trace.WriteLine($"w={w}, move={move} ");
                    //Trace.WriteLine($"move={move}, {string.Join(", ", sa)}");
                    Parallel.Invoke(new Action[]{
                        () => {
                            int p = 0;
                            for (int i = n - move; i < n; i++) y[p++] = i;
                            Trace.Write(".");
                            for (int i = 0; i < n; i++) if (sa[i] >= move) y[p++] = sa[i] - move;
                            Trace.Write(".");
                        },
                        ()=>
                        {
                            for (int i = 0; i < w; i++) c[i] = 0;
                            Trace.Write(".");
                            for (int i = 0; i < n; i++) c[x[i]]++;
                            Trace.Write(".");
                            for (int i = 1; i < w; i++) c[i] += c[i - 1];
                            Trace.Write(".");
                        }
                    });
                    Trace.Write("|");
                    for (int i = n - 1; i >= 0; i--) sa[--c[x[y[i]]]] = y[i];
                    Trace.Write("#");
                    w = 0;
                    Utils.Swap(ref x, ref y);
                    x[sa[0]] = w++;
                    Parallel.For(1, n, (i) =>
                      {
                          x[sa[i]] = (y[sa[i - 1]] != y[sa[i]] || (sa[i - 1] + move < n) != (sa[i] + move < n) || y[sa[i - 1] + move] != y[sa[i] + move]) ? 1 : 0;
                      });
                    Trace.Write("|");
                    for (int i = 1; i < n; i++)
                    {
                        x[sa[i]] = x[sa[i]] == 1 ? w++ : w - 1;
                    }
                    Trace.Write(".");
                    if (w == n) break;
                }
                //Debug.WriteLine(s);
                //Debug.WriteLine(string.Join(", ", sa));
            }
            finally { Trace.WriteLine("Done"); Trace.Unindent(); }
        }
        void BuildHeight(string s)
        {
            int n = s.Length;
            rank.Resize(n,0);height.Resize(n,0);
            for (int i = 0; i < n; i++) rank[sa[i]] = i;
            for(int i=0,ans=0;i<n;i++)
            {
                if (ans > 0) --ans;
                if (rank[i] == 0) height[0] = 0;
                else
                {
                    int j = sa[rank[i] - 1];
                    while (i + ans < n && j + ans < n && s[i + ans] == s[j + ans]) ++ans;
                    height[rank[i]] = ans;
                }
            }
            //Debug.WriteLine(string.Join(", ", height));
            //for (int i = 0; i < n; i++) Debug.WriteLine($"{height[i]}\t{s.Substring(sa[i])}");
        }
        public void Build(string s)
        {
            try
            {
                Trace.Indent();
                S = s;
                Trace.WriteLine("BuildSA...");
                BuildSA(s);
                Trace.WriteLine("BuildHeight...");
                BuildHeight(s);
            }
            finally { Trace.WriteLine("Build Done"); Trace.Unindent(); }
        }
    }
}
