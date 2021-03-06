﻿using System;
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
        public bool IsBuilt { get; private set; } = false;
        List<int> sa = new List<int>(), height = new List<int>(), rank = new List<int>();
        public int UpperBound(string s)
        {
            int l = 0, r = S.Length;
            while(l<r)
            {
                int mid = (l + r) / 2;
                if (string.Compare(S, SA[mid], s, 0, s.Length,StringComparison.Ordinal) <= 0) l = mid + 1;
                else r = mid;
            }
            return r;
        }
        public int LowerBound(string s)
        {
            int l = 0, r = S.Length;
            while (l < r)
            {
                int mid = (l + r) / 2;
                if (string.Compare(S, SA[mid], s, 0, s.Length, StringComparison.Ordinal) < 0) l = mid + 1;
                else r = mid;
            }
            return r;
        }
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
                    //{
                    //    int a = --c[x[y[n - 1]]], b = 0;
                    //    for (int i = n - 2; i >= 0; i--)
                    //    {
                    //        Parallel.Invoke(new Action[]
                    //        {
                    //            ()=>sa[a]=y[i+1],
                    //            ()=>b=--c[x[y[i]]]
                    //        });
                    //        Utils.Swap(ref a, ref b);
                    //    }
                    //    sa[a] = y[0];
                    //}
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
        public async Task BuildAsync(string s)
        {
            await Task.Run(() => Build(s));
        }
        void Build(string s)
        {
            try
            {
                Trace.Indent();
                S = s;
                Trace.WriteLine("BuildSA...");
                BuildSA(s);
                Trace.WriteLine("BuildHeight...");
                BuildHeight(s);
                IsBuilt = true;
            }
            finally { Trace.WriteLine("Build Done"); Trace.Unindent(); }
        }
        public async Task ListFrequentWords(int threshold, Func<string, Task> action)
        {
            Trace.WriteLine("ListFrequentWords(int threshold, Func<string, Task> action)...");
            try
            {
                Trace.Indent();
                Trace.Assert(threshold >= 2, $"threshold, which is {threshold}, must >= 2");
                int n = S.Length;
                Trace.WriteLine("Copying height data...");
                List<Tuple<int, int>> h = new List<Tuple<int, int>>();
                for (int i = 1; i < n; i++) h.Add(new Tuple<int, int>(HEIGHT[i], i));
                Trace.WriteLine("Sorting...");
                //DistributedSort(h, (a, b) => a.Item1.CompareTo(b.Item1));
                h.Sort((a, b) => a.Item1.CompareTo(b.Item1));
                var changes = new List<Tuple<int, int, int>>();
                {
                    Trace.WriteLine("Creating linked list...");
                    int[] linkl = new int[n + 1], linkr = new int[n + 1];
                    for (int i = 0; i < n; i++)
                    {
                        linkl[i + 1] = i;
                        linkr[i] = i + 1;
                    }
                    Trace.WriteLine("Simulate changes...");
                    int j = n - 2;
                    for (int gram = n; gram >= 1; gram--)
                    {
                        changes.Add(new Tuple<int, int, int>(-1, -1, -1));
                        while (j >= 0 && h[j].Item1 >= gram)
                        {
                            int k = h[j].Item2;
                            int l = linkl[k], r = linkr[k];
                            changes.Add(new Tuple<int, int, int>(l, k, r));
                            linkl[r] = l;
                            linkr[l] = r;
                            --j;//j+1 is the num of splittings
                            if (r - l >= threshold)
                            {
                                for (int len = gram; len > Math.Max(HEIGHT[l], r >= n ? 0 : HEIGHT[r]); len--)
                                {
                                    await action(S.Substring(SA[l], len));
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception error) { System.Windows.Forms.MessageBox.Show(error.ToString()); throw; }
            finally { Trace.Unindent(); }
        }
        public async Task LoadAsync(System.IO.Stream fileStream)
        {
            try
            {
                Trace.Indent();
                Trace.WriteLine("Loading...");
                await Task.Run(() =>
                {
                    using (System.IO.BinaryReader reader = new System.IO.BinaryReader(fileStream))
                    {
                        S = null;
                        sa = null;
                        rank = null;
                        height = null;
                        FPL = null;
                        Trace.WriteLine("Reading S data...");
                        S = reader.ReadString();
                        Trace.WriteLine("Reading SA data...");
                        sa = new List<int>();
                        for (int i = 0; i < S.Length; i++) sa.Add((int)reader.ReadInt64());
                        Trace.WriteLine("Reading HEIGHT data...");
                        height = new List<int>();
                        for (int i = 0; i < S.Length; i++) height.Add((int)reader.ReadInt64());
                        reader.Close();
                    }
                });
                Trace.WriteLine("Rebuilding RANK data...");
                rank = new List<int>();
                rank.Resize(S.Length, 0);
                for (int i = 0; i < S.Length; i++) rank[sa[i]] = i;
                IsBuilt = true;
                Trace.WriteLine("Done.");
            }
            catch (Exception error) { Trace.WriteLine(error); }
            finally { Trace.WriteLine("Loaded."); Trace.Unindent(); }
        }
        public async Task SaveAsync(System.IO.Stream fileStream)
        {
            try
            {
                Trace.Indent();
                Trace.WriteLine("Saving...");
                await Task.Run(() =>
                {
                    using (System.IO.BinaryWriter writer = new System.IO.BinaryWriter(fileStream))
                    {
                        Trace.WriteLine("Writing S data...");
                        writer.Write(S);
                        Trace.WriteLine("Writing SA data...");
                        for (int i = 0; i < S.Length; i++) writer.Write((long)SA[i]);
                        Trace.WriteLine("Writing HEIGHT data...");
                        for (int i = 0; i < S.Length; i++) writer.Write((long)HEIGHT[i]);
                        writer.Close();
                    }
                });
                Trace.WriteLine("Done.");
            }
            catch (Exception error) { Trace.WriteLine(error); }
            finally { Trace.WriteLine("Saved."); Trace.Unindent(); }
        }
        public List<FPLtype> FPL = null;
    }
}
