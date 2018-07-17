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
        public double mean, stderr,sqrtSum,logSum;
        public int percent10;
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
        long Sq(int a) { return (long)a * a; }
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
                List<Tuple<int, int>> changes = new List<Tuple<int, int>>();
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
                        while (j >= 0 && h[j].Item1 >= gram)
                        {
                            int k = h[j].Item2;
                            int l = linkl[k], r = linkr[k];
                            changes.Add(new Tuple<int, int>(k - l, r - k));
                            linkl[r] = l;
                            linkr[l] = r;
                            --j;//j+1 is the num of splittings
                        }
                        changes.Add(new Tuple<int, int>(-1, j));
                    }
                }
                Trace.WriteLine("Building ans...");
                List<FPLtype> ans = new List<FPLtype>();
                ans.Resize(n + 1, default(FPLtype));
                //for (int i = 1; i <= n; i++) ans[i] = new FPLtype();
                Trace.WriteLine("Filling ans...");
                {
                    long sp2 = n;//sum of power 2
                    double sp0_5 = n;//sum of power 0.5
                    double slog = n * Math.Log(2);

                    int[] cnt = new int[n + 1];
                    for (int i = 0; i <= n; i++) Trace.Assert(cnt[i] == 0);
                    int cursor = 1, current_count = cnt[1] = n;
                    //System.Windows.Forms.MessageBox.Show("pass");

                    for (int gram = n, c = 0; gram >= 1; gram--)
                    {
                        {
                            int cc=c;
                            while(true)
                            {
                                var p=changes[cc++];
                                if(p.Item1==-1)break;
                                sp2 -= Sq(p.Item1);
                                sp2 -= Sq(p.Item2);
                                sp2 += Sq(p.Item1 + p.Item2);
                                sp0_5 -= Math.Sqrt(p.Item1);
                                sp0_5 -= Math.Sqrt(p.Item2);
                                sp0_5 += Math.Sqrt(p.Item1 + p.Item2);
                                slog -= Math.Log(p.Item1 + 1);
                                slog -= Math.Log(p.Item2 + 1);
                                slog += Math.Log(p.Item1 + p.Item2 + 1);
                                --cnt[p.Item1];if (p.Item1 <= cursor) --current_count;
                                --cnt[p.Item2];if (p.Item2 <= cursor) --current_count;
                                ++cnt[p.Item1 + p.Item2];if (p.Item1 + p.Item2 <= cursor) ++current_count;
                                //Trace.WriteLine($"{p.Item1}\t{p.Item2}");
                                //Trace.Assert(cnt[p.Item1] >= 0 && cnt[p.Item2] >= 0);
                            }
                        }
                        while (changes[c++].Item1 != -1) ;
                        int j = changes[c - 1].Item2;
                        try
                        {
                            while (current_count < 0.01 * (j + 2)) current_count += cnt[++cursor];
                            while (current_count - cnt[cursor] >= 0.01 * (j + 2)) current_count -= cnt[cursor--];
                        }
                        catch(Exception error) { System.Windows.Forms.MessageBox.Show($"cursor={cursor}, current_count={current_count}, i={gram}, j={j}, n={n}\r\n"+error.ToString()); }
                        double u = (double)n / (j + 2);
                        ans[gram] = new FPLtype
                        {
                            mean = u,
                            stderr = Math.Sqrt((double)sp2 / (j + 2) - u * u),
                            sqrtSum = sp0_5,
                            logSum = slog,
                            percent10 = cursor
                        };
                    }
                }
                Trace.Write("OK");
                //System.Windows.Forms.MessageBox.Show(string.Join(", ", ans.GetRange(0, 20)));
                return sa.FPL = ans;
            }
            catch(Exception error) { System.Windows.Forms.MessageBox.Show(error.ToString());throw; }
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
        public enum ProbTypeEnum { CdL, CdM, CxLdM, CmMdSTDE, sqCdS, sqCxLdS, lnCdS, lnCxLdS,Sigmoid }
        public const string probTypeString =
                                   "probType == ProbTypeEnum.CdL ? Math.Log(wordCount / (motherSA.S.Length - l + 1)) :\n" +
                                   "probType == ProbTypeEnum.CdM ? Math.Log(wordCount / fpl[l].mean) :\n" +
                                   "probType == ProbTypeEnum.CxLdM ? Math.Log(wordCount / fpl[l].mean) * l :\n" +
                                   "probType == ProbTypeEnum.CmMdSTDE ? (Count(motherSA, s.Substring(i, l)) - fpl[l].mean) / Math.Pow(fpl[l].stderr, 1.0) :\n" +
                                   "probType == ProbTypeEnum.sqCdS ? Math.Log((double)Math.Sqrt(wordCount) / (fpl[l].sqrtSum / (motherSA.S.Length / fpl[l].mean))) :\n" +
                                   "probType == ProbTypeEnum.sqCxLdS ? Math.Log((double)Math.Sqrt(wordCount) / (fpl[l].sqrtSum / (motherSA.S.Length / fpl[l].mean))) * l :\n" +
                                   "probType == ProbTypeEnum.lnCdS ? Math.Log(Math.Max(double.Epsilon, (double)Math.Log(wordCount) / (fpl[l].logSum / (motherSA.S.Length / fpl[l].mean)))) :\n" +
                                   "probType == ProbTypeEnum.lnCxLdS ? Math.Log(Math.Max(double.Epsilon, (double)Math.Log(wordCount) / (fpl[l].logSum / (motherSA.S.Length / fpl[l].mean)))) * l :\n" +
                                   "probType == ProbTypeEnum.Sigmoid ? 1.0 / (1.0 + Math.Exp(-(Count(motherSA, s.Substring(i, l)) - fpl[l].mean) / fpl[l].stderr)) * l :\n";
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
                               double wordCount = Count(motherSA, s.Substring(i, l));
                               //if (wordCount <= 5)//fpl[l].percent10)
                               //{
                               //    wordCount = Math.Max(wordCount - 5, Math.Pow(((fpl[1].sqrtSum / (motherSA.S.Length / fpl[1].mean))) / motherSA.S.Length, l));
                               //}
                               double probLog =
                                   probType == ProbTypeEnum.CdL ? Math.Log(wordCount / (motherSA.S.Length - l + 1)) :
                                   probType == ProbTypeEnum.CdM ? Math.Log(wordCount / fpl[l].mean) :
                                   probType == ProbTypeEnum.CxLdM ? Math.Log(wordCount / fpl[l].mean) * l :
                                   probType == ProbTypeEnum.CmMdSTDE ? (Count(motherSA, s.Substring(i, l)) - fpl[l].mean) / Math.Pow(fpl[l].stderr, 1.0) :
                                   probType == ProbTypeEnum.sqCdS ? Math.Log((double)Math.Sqrt(wordCount) / (fpl[l].sqrtSum / (motherSA.S.Length / fpl[l].mean))) :
                                   probType == ProbTypeEnum.sqCxLdS ? Math.Log((double)Math.Sqrt(wordCount) / (fpl[l].sqrtSum / (motherSA.S.Length / fpl[l].mean))) * l :
                                   probType == ProbTypeEnum.lnCdS ? Math.Log(Math.Max(double.Epsilon, (double)Math.Log(wordCount) / (fpl[l].logSum / (motherSA.S.Length / fpl[l].mean)))) :
                                   probType == ProbTypeEnum.lnCxLdS ? Math.Log(Math.Max(double.Epsilon, (double)Math.Log(wordCount) / (fpl[l].logSum / (motherSA.S.Length / fpl[l].mean)))) * l :
                                   probType == ProbTypeEnum.Sigmoid ? 1.0 / (1.0 + Math.Exp(-(Count(motherSA, s.Substring(i, l)) - fpl[l].mean) / fpl[l].stderr)) * l :
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
