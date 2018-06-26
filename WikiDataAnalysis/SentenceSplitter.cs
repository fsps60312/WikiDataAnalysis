using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace WikiDataAnalysis
{
    class FPLtype
    {
        public double mean, stderr,sqrtSum;
    }
    class SentenceSplitter
    {
        public SentenceSplitter(SuffixArray _motherSA) { motherSA = _motherSA; }
        public delegate void WordIdentifiedEventHandler(string word);
        public event WordIdentifiedEventHandler WordIdentified;
        //void DistributedSort<T>(List<T> s, Comparison<T> comparation)//too slow & assertion failed
        //{
        //    try
        //    {
        //        Trace.Indent();
        //        Trace.WriteLine("DistributedSorting...");
        //        var buf = new T[s.Count];
        //        for (int n = 2; n < s.Count; n <<= 1)
        //        {
        //            Parallel.For(0, (s.Count + n - 1) / n, it =>
        //                    {
        //                        int i = it * n, j = i + n / 2, k = i, l = i, m = j, r = Math.Min(i + n, s.Count);
        //                        for (int _ = 0; _ < r - l; _++) buf[k++] = (j >= r || (i < m && comparation(s[i], s[j]) <= 0)) ? s[i++] : s[j++];
        //                        Parallel.For(l, r, _ => s[_] = buf[_]);
        //                    });
        //        }
        //        Trace.Write("OK");
        //        Parallel.For(1, s.Count, i => Trace.Assert(comparation(s[i - 1], s[i]) <= 0));
        //        Trace.Write(" and Valid");
        //    }
        //    finally { Trace.Unindent(); }
        //}
        private List<FPLtype>FrequencyPerLength(SuffixArray sa)
        {
            Trace.WriteLine("FrequencyPerLength(SuffixArray sa)...");
            try
            {
                Trace.Indent();
                if (sa.FPL != null) return sa.FPL;
                int n = sa.S.Length;
                Trace.WriteLine("Copying height data...");
                List<Tuple<int, int>> h = new List<Tuple<int, int>>();
                for (int i = 1; i < n; i++) h.Add(new Tuple<int, int>(sa.HEIGHT[i], i));
                Trace.WriteLine("Sorting...");
                //DistributedSort(h, (a, b) => a.Item1.CompareTo(b.Item1));
                h.Sort((a, b) => a.Item1.CompareTo(b.Item1));
                Trace.WriteLine("Creating linked list...");
                int[] linkl = new int[n + 1], linkr = new int[n + 1];
                for (int i = 0; i < n; i++)
                {
                    linkl[i + 1] = i;
                    linkr[i] = i + 1;
                }
                Trace.WriteLine("Almost finish...");
                List<FPLtype> ans = new List<FPLtype>();
                ans.Resize(n + 1, default(FPLtype));
                int j = n - 2;
                long sp2 = n;//sum of power 2
                double sp0_5 = n;//sum of power 0.5
                for (int i = n; i >=1; i--)
                {
                    while (j >=0 && h[j].Item1 >= i)
                    {
                        int k = h[j].Item2;
                        int l = linkl[k], r = linkr[k];
                        sp2 -= (k - l) * (k - l);
                        sp2 -= (r - k) * (r - k);
                        sp2 += (r - l) * (r - l);
                        sp0_5 -= Math.Sqrt(k - l);
                        sp0_5 -= Math.Sqrt(r - k);
                        sp0_5 += Math.Sqrt(r - l);
                        linkl[r] = l;
                        linkr[l] = r;
                        --j;//j+1 is the num of splittings
                    }
                    double u = (double)n / (j + 2);
                    ans[i] = new FPLtype { mean = u, stderr = Math.Sqrt((double)sp2 / (j + 2) - u * u), sqrtSum = sp0_5 };
                }
                Trace.Write("OK");
                //System.Windows.Forms.MessageBox.Show(string.Join(", ", ans.GetRange(0, 20)));
                return sa.FPL = ans;
            }
            finally { Trace.Unindent(); }
        }
        private int Count(SuffixArray sa,string s)
        {
            return sa.UpperBound(s) - sa.LowerBound(s);
        }
        private int CountInsideSA(SuffixArray sa, int startIndex, int length)
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
                double ratio = (CountInsideSA(sa, startIndex, l) - Math.Pow(fpl[l].Item1, 1)) / fpl[l].Item2;
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
        BEMSmodel bm;
        public async Task<List<string>> SplitAsync(string sa, int maxWordLength,double probRatio, double bemsRatio, ProbTypeEnum probType,bool logPortion)
        {
            bm = new BEMSmodel();
            await bm.DownloadDictionaryAsync();
            return await Task.Run(() => Split(sa, maxWordLength,bm,probRatio,bemsRatio,probType,logPortion));
        }
        public bool IsBuilt { get; private set; } = false;
        public List<string> SplittedWords { get; private set; }
        public enum ProbTypeEnum { CdL,CdM,CxLdM,CmMdSTDE,sqCdS,sqCxLdS}
        public const string probTypeString =
                                   "probType == ProbTypeEnum.CdL ? Math.Log((double)Math.Max(Count(motherSA, s.Substring(i, l)), 1) / (motherSA.S.Length - l + 1)) :                                            \n" +
                                   "probType == ProbTypeEnum.CdM ? Math.Log((double)Math.Max(Count(motherSA, s.Substring(i, l)), 1) / fpl[l].mean) :                                                            \n" +
                                   "probType == ProbTypeEnum.CxLdM ? Math.Log((double)Math.Max(Count(motherSA, s.Substring(i, l)), 1) / fpl[l].mean) * l :                                                      \n" +
                                   "probType == ProbTypeEnum.CmMdSTDE ? (Count(motherSA, s.Substring(i, l)) - fpl[l].mean) / Math.Pow(fpl[l].stderr, 1.0) :                                                     \n" +
                                   "probType == ProbTypeEnum.sqCdS ? Math.Log((double)Math.Sqrt(Math.Max(Count(motherSA, s.Substring(i, l)), 1)) / (fpl[l].sqrtSum / (motherSA.S.Length / fpl[l].mean))) :      \n" +
                                   "probType == ProbTypeEnum.sqCxLdS ? Math.Log((double)Math.Sqrt(Math.Max(Count(motherSA, s.Substring(i, l)), 1)) / (fpl[l].sqrtSum / (motherSA.S.Length / fpl[l].mean))) * l :\n";
        List<string> Split(string s, int maxWordLength, BEMSmodel bm,double probRatio,double bemsRatio, ProbTypeEnum probType,bool logPortion)
        {
            try
            {
                Trace.Indent();
                Trace.WriteLine("Getting FPL...");
                var fpl = FrequencyPerLength(motherSA);
                Trace.Write("OK");
                int n = s.Length;
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
                               double probLog =
                                   probType == ProbTypeEnum.CdL ? Math.Log((double)Math.Max(Count(motherSA, s.Substring(i, l)), 1) / (motherSA.S.Length - l + 1)) :
                                   probType == ProbTypeEnum.CdM ? Math.Log((double)Math.Max(Count(motherSA, s.Substring(i, l)), 1) / fpl[l].mean) :
                                   probType == ProbTypeEnum.CxLdM ? Math.Log((double)Math.Max(Count(motherSA, s.Substring(i, l)), 1) / fpl[l].mean) * l :
                                   probType == ProbTypeEnum.CmMdSTDE ? (Count(motherSA, s.Substring(i, l)) - fpl[l].mean) / Math.Pow(fpl[l].stderr, 1.0) :
                                   probType == ProbTypeEnum.sqCdS ? Math.Log((double)Math.Sqrt(Math.Max(Count(motherSA, s.Substring(i, l)), 1)) / (fpl[l].sqrtSum / (motherSA.S.Length / fpl[l].mean))) :
                                   probType == ProbTypeEnum.sqCxLdS ? Math.Log((double)Math.Sqrt(Math.Max(Count(motherSA, s.Substring(i, l)), 1)) / (fpl[l].sqrtSum / (motherSA.S.Length / fpl[l].mean))) * l :
                                   throw new Exception($"Unknown probType: {probType}"); // (Count(sa, i, l) - fpl[l].Item1) / Math.Pow(fpl[l].Item2, 1.0);
                               double bemsLog = bm.Query(motherSA.S.Substring(i, l));
                               probLog = logPortion ? probLog * probRatio + bemsLog * bemsRatio : Math.Log(Math.Exp(probLog) * probRatio + Math.Exp(bemsLog) * bemsRatio);
                               //var v = (dp[i] * cnt[i] + ratio) / (cnt[i] + 1);
                               var v = dp[i] + probLog;
                               if (v > dp[i + l])
                               {
                                   dp[i + l] = v;
                                   cnt[i + l] = cnt[i] + 1;
                                   pre[i + l] = l;
                               }
                           });
                    if (i > 0 && (i + 1) * 100L / n > percentage)
                    {
                        Trace.WriteLine($"DPing... {++percentage}% Ex: {s.Substring(i - pre[i], pre[i])} scored {dp[i]} avg {(double)i / cnt[i]} words");
                    }
                }
                Trace.WriteLine("Tracing back...");
                List<int> idxs = new List<int>();
                for (int i = n; i >= 0; i -= pre[i]) idxs.Add(i);
                Trace.WriteLine("Picking words...");
                percentage = -1;
                for (int i = idxs.Count - 1; i > 0; i--)
                {
                    string _s = s.Substring(idxs[i], idxs[i - 1] - idxs[i]);
                    WordIdentified?.Invoke(_s);
                    ans.Add(_s);
                    if ((idxs.Count - i + 1) * 100L / idxs.Count > percentage)
                    {
                        Trace.WriteLine($"Picking words... {++percentage}% Ex: {_s}");
                    }
                }
                Trace.Write(" => OK");
                SplittedWords = ans;
                IsBuilt = true;
                return ans;
            }
            finally { Trace.Unindent(); }
        }
    }
}
