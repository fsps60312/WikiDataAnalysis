using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace WikiDataAnalysis
{
    class SentenceSplitter
    {
        public SentenceSplitter(SuffixArray _motherSA) { motherSA = _motherSA; }
        public delegate void WordIdentifiedEventHandler(string word);
        public event WordIdentifiedEventHandler WordIdentified;
        private List<Tuple<double,double, int>>FrequencyPerLength(SuffixArray sa)
        {
            Trace.WriteLine("FrequencyPerLength(SuffixArray sa)...");
            try
            {
                Trace.Indent();
                int n = sa.S.Length;
                Trace.WriteLine("Copying height data...");
                List<Tuple<int, int>> h = new List<Tuple<int, int>>();
                for (int i = 1; i < n; i++) h.Add(new Tuple<int, int>(sa.HEIGHT[i], i));
                Trace.WriteLine("Sorting...");
                h.Sort((a, b) => a.Item1.CompareTo(b.Item1));
                Trace.WriteLine("Creating linked list...");
                int[] linkl = new int[n + 1], linkr = new int[n + 1];
                for (int i = 0; i < n; i++)
                {
                    linkl[i + 1] = i;
                    linkr[i] = i + 1;
                }
                Trace.WriteLine("Almost finish...");
                List<Tuple<double,double,int>> ans = new List<Tuple<double, double, int>>();
                ans.Resize(n + 1, default(Tuple<double, double, int>));
                int j = n - 2;
                long ro = n;
                for (int i = n; i >=1; i--)
                {
                    while (j >=0 && h[j].Item1 >= i)
                    {
                        int k = h[j].Item2;
                        int l = linkl[k], r = linkr[k];
                        ro -= (k - l) * (k - l);
                        ro -= (r - k) * (r - k);
                        ro += (r - l) * (r - l);
                        linkl[r] = l;
                        linkr[l] = r;
                        --j;//j+1 is the num of splittings
                    }
                    double u = (double)n / (j + 2);
                    ans[i] = new Tuple<double, double,int>(u, Math.Sqrt((double)ro / (j + 2) - u * u),j+2);
                }
                Trace.Write("OK");
                //System.Windows.Forms.MessageBox.Show(string.Join(", ", ans.GetRange(0, 20)));
                return ans;
            }
            finally { Trace.Unindent(); }
        }
        private int Count(SuffixArray sa, int startIndex, int length)
        {
            //var s = sa.S.Substring(startIndex, length);
            //return sa.UpperBound(s) - sa.LowerBound(s);
            int l, r, n = sa.S.Length;
            l = r = sa.RANK[startIndex];
            int m = 1;
            for (; l - m >= 0 && string.Compare(sa.S, sa.SA[l], sa.S, sa.SA[l - m], length, StringComparison.Ordinal) == 0; m <<= 1) l -= m;
            for (; m > 0; m >>= 1) if (l - m >= 0 && string.Compare(sa.S, sa.SA[l], sa.S, sa.SA[l - m], length, StringComparison.Ordinal) == 0) l -= m;
            m = 1;
            for (; r + m < n && string.Compare(sa.S, sa.SA[r], sa.S, sa.SA[r + m], length, StringComparison.Ordinal) == 0; m <<= 1) r += m;
            for (; m > 0; m >>= 1) if (r + m < n && string.Compare(sa.S, sa.SA[r], sa.S, sa.SA[r + m], length, StringComparison.Ordinal) == 0) r += m;
            return r - l + 1;
        }
        private int Cut(SuffixArray sa, int startIndex, List<Tuple<double, double>> fpl, int maxWordLength)
        {
            double currentMax = double.NegativeInfinity;
            int ans = -1;
            List<double> t = new List<double>();
            for (int l = maxWordLength; l >= 1; l--)
            {
                double ratio = (Count(sa, startIndex, l) - Math.Pow(fpl[l].Item1, 1)) / fpl[l].Item2;
                t.Add(ratio);
                if (ratio > currentMax)
                {
                    currentMax = ratio;
                    ans = l;
                }
            }
            Trace.Assert(ans != -1);
            //System.Windows.Forms.MessageBox.Show($"{sa.S.Substring(startIndex, maxWordLength)}: {string.Join(", ", t)}");
            return ans;
        }
        SuffixArray motherSA;
        public List<string> Split(string sa, int maxWordLength)
        {
            try
            {
                Trace.Indent();
                Trace.WriteLine("Getting FPL...");
                var fpl = FrequencyPerLength(motherSA);
                Trace.Write("OK");
                int n = sa.Length;
                List<string> ans = new List<string>();
                int[] pre = new int[n + 1], cnt = new int[n + 1];
                double[] dp = new double[n + 1];
                dp[0] = 0;
                for (int i = 1; i <= n; i++) dp[i] = double.NegativeInfinity;
                pre[0] = 1;//crutial to make 0-pre[0]<0 when tracing back
                cnt[0] = 0;
                Trace.WriteLine("DPing...");
                int percentage = -1;
                for (int i = 0; i < n; i++)
                {
                    Parallel.For(1, Math.Min(n - i, maxWordLength) + 1, (l) =>
                           {
                               double problog = Math.Log((double)Count(motherSA, i, l) / (motherSA.S.Length - l + 1)); //Math.Log((double)Count(sa, i, l) / fpl[l].Item1); //(Count(sa, i, l) - fpl[l].Item1) / Math.Pow(fpl[l].Item2, 1.0);
                               //var v = (dp[i] * cnt[i] + ratio) / (cnt[i] + 1);
                               var v = dp[i] + problog;
                               if (v > dp[i + l])
                               {
                                   dp[i + l] = v;
                                   cnt[i + l] = cnt[i] + 1;
                                   pre[i + l] = l;
                               }
                           });
                    if (i > 0 && (i + 1) * 100L / n > percentage)
                    {
                        Trace.WriteLine($"DPing... {++percentage}% Ex: {sa.Substring(i - pre[i], pre[i])} scored {dp[i]} avg {(double)i / cnt[i]} words");
                    }
                }
                Trace.WriteLine("Tracing back...");
                List<int> idxs = new List<int>();
                for (int i = n; i >= 0; i -= pre[i]) idxs.Add(i);
                Trace.WriteLine("Picking words...");
                percentage = -1;
                for (int i = idxs.Count - 1; i > 0; i--)
                {
                    string s = sa.Substring(idxs[i], idxs[i - 1] - idxs[i]);
                    WordIdentified?.Invoke(s);
                    ans.Add(s);
                    if ((idxs.Count - i + 1) * 100L / idxs.Count > percentage)
                    {
                        Trace.WriteLine($"Picking words... {++percentage}% Ex: {s}");
                    }
                }
                Trace.Write(" => OK");
                return ans;
            }
            finally { Trace.Unindent(); }
        }
    }
}
