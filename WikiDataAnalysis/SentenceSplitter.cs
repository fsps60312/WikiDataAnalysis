using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace WikiDataAnalysis
{
    class FPLtype
    {
        public double mean, stderr,sqrtSum,logSum;
        public int percent10;
    }
    class SentenceSplitter
    {
        static class MethodsForSuffixArray
        {
            static long Sq(int a) { return (long)a * a; }
            public static int Count(SuffixArray sa, string s)
            {
                return sa.UpperBound(s) - sa.LowerBound(s);
            }
            private static int CountInsideSA(SuffixArray sa, int startIndex, int length)
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
            public static List<FPLtype> FrequencyPerLength(SuffixArray sa)
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
                                int cc = c;
                                while (true)
                                {
                                    var p = changes[cc++];
                                    if (p.Item1 == -1) break;
                                    sp2 -= Sq(p.Item1);
                                    sp2 -= Sq(p.Item2);
                                    sp2 += Sq(p.Item1 + p.Item2);
                                    sp0_5 -= Math.Sqrt(p.Item1);
                                    sp0_5 -= Math.Sqrt(p.Item2);
                                    sp0_5 += Math.Sqrt(p.Item1 + p.Item2);
                                    slog -= Math.Log(p.Item1 + 1);
                                    slog -= Math.Log(p.Item2 + 1);
                                    slog += Math.Log(p.Item1 + p.Item2 + 1);
                                    --cnt[p.Item1]; if (p.Item1 <= cursor) --current_count;
                                    --cnt[p.Item2]; if (p.Item2 <= cursor) --current_count;
                                    ++cnt[p.Item1 + p.Item2]; if (p.Item1 + p.Item2 <= cursor) ++current_count;
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
                            catch (Exception error) { System.Windows.Forms.MessageBox.Show($"cursor={cursor}, current_count={current_count}, i={gram}, j={j}, n={n}\r\n" + error.ToString()); }
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
                catch (Exception error) { System.Windows.Forms.MessageBox.Show(error.ToString()); throw; }
                finally { Trace.Unindent(); }
            }
        }
        public static class MethodsForTrie
        {
            public static List<FPLtype> FrequencyPerLength(Trie trie)
            {
                try
                {
                    Trace.Indent();
                    List<FPLtype> ans = new List<FPLtype>();
                    List<long> cnt = new List<long>();
                    int l = 0;
                    Trace.WriteLine($"Traversing... Size: {trie.Size}");
                    trie.Traverse(c => { l++; }, () => l--, _c =>
                         {
                             if (_c == 0) return;
                             while (ans.Count <= l)
                             {
                                 cnt.Add(0);
                                 ans.Add(new FPLtype());
                             }
                             var c = (double)_c;
                             ++cnt[l];
                             ans[l].mean += c;
                             ans[l].stderr += c * c;
                             ans[l].sqrtSum += Math.Sqrt(c);
                             ans[l].logSum += Math.Log(c);
                         });
                    Trace.WriteLine("Building ans...");
                    for (int i = 0; i < ans.Count; i++)
                    {
                        ans[i].mean /= cnt[i];
                        ans[i].stderr = Math.Sqrt(ans[i].stderr / cnt[i] - ans[i].mean * ans[i].mean);
                        ans[i].percent10 = 0;//not implemented
                    }
                    Trace.Write("OK");
                    return ans;
                }
                finally { Trace.Unindent(); }
            }
        }
        List<FPLtype> fpl_cache = null;
        Func<List<FPLtype>> FrequencyPerLength;
        Func<string,int> Count;
        Func<int> BaseDataLength;
        Func<string, List<char>> NextChars;
        public SentenceSplitter(SuffixArray sa)
        {
            FrequencyPerLength = () => MethodsForSuffixArray.FrequencyPerLength(sa);
            Count = s => MethodsForSuffixArray.Count(sa, s);
            BaseDataLength = () => sa.S.Length;
            NextChars = delegate { throw new NotImplementedException(); };
        }
        public SentenceSplitter(Trie trie,int baseDataLength)
        {
            FrequencyPerLength = () => { if (fpl_cache == null) fpl_cache = MethodsForTrie.FrequencyPerLength(trie); return fpl_cache; };
            Count = s => (int)trie.Count(s);
            BaseDataLength = () => baseDataLength;
            NextChars = s => trie.NextChars(s);
        }
        public delegate void WordIdentifiedEventHandler(string word);
        public event WordIdentifiedEventHandler WordIdentified;
        BEMSmodel bm=null;
        public async Task<List<string>> SplitAsync(string s, int maxWordLength, double probRatio, double bemsRatio, ProbTypeEnum probType, bool logPortion, bool verbose = true)
        {
            if (bemsRatio != 0 && bm == null)
            {
                bm = new BEMSmodel();
                await bm.DownloadDictionaryAsync();
            }
            return await Task.Run(() => Split(s, maxWordLength, bm
                , probType == ProbTypeEnum.Entropy || probType == ProbTypeEnum.Entropy_L ? null : FrequencyPerLength()
                , probRatio, bemsRatio, probType, logPortion, verbose));
        }
        public bool IsBuilt { get; private set; } = false;
        public List<string> SplittedWords { get; private set; }
        public enum ProbTypeEnum { CdL, CdM, CxLdM, CmMdSTDE, sqCdS, sqCxLdS, lnCdS, lnCxLdS,Sigmoid,Entropy, Entropy_L }
        public const string probTypeString =
                                    "probType == ProbTypeEnum.CdL ? Math.Log(wordCount / (BaseDataLength() - l + 1)) :                                                                            \n" +
                                    "probType == ProbTypeEnum.CdM ? Math.Log(wordCount / fpl[l].mean) :                                                                                           \n" +
                                    "probType == ProbTypeEnum.CxLdM ? Math.Log(wordCount / fpl[l].mean) * l :                                                                                     \n" +
                                    "probType == ProbTypeEnum.CmMdSTDE ? (Count(s.Substring(i, l)) - fpl[l].mean) / Math.Pow(fpl[l].stderr, 1.0) :                                                \n" +
                                    "probType == ProbTypeEnum.sqCdS ? Math.Log((double)Math.Sqrt(wordCount) / (fpl[l].sqrtSum / (BaseDataLength() / fpl[l].mean))) :                              \n" +
                                    "probType == ProbTypeEnum.sqCxLdS ? Math.Log((double)Math.Sqrt(wordCount) / (fpl[l].sqrtSum / (BaseDataLength() / fpl[l].mean))) * l :                        \n" +
                                    "probType == ProbTypeEnum.lnCdS ? Math.Log(Math.Max(double.Epsilon, (double)Math.Log(wordCount) / (fpl[l].logSum / (BaseDataLength() / fpl[l].mean)))) :      \n" +
                                    "probType == ProbTypeEnum.lnCxLdS ? Math.Log(Math.Max(double.Epsilon, (double)Math.Log(wordCount) / (fpl[l].logSum / (BaseDataLength() / fpl[l].mean)))) * l :\n" +
                                    "probType == ProbTypeEnum.Sigmoid ? 1.0 / (1.0 + Math.Exp(-(Count(s.Substring(i, l)) - fpl[l].mean) / fpl[l].stderr)) * l :                                   \n" +
                                    "probType == ProbTypeEnum.Entropy ? Entropy(s.Substring(i, l)) :                                                                                              \n" +
                                    "probType == ProbTypeEnum.Entropy_L ? Entropy(s.Substring(i, l)) * l :                                                                                        \n";
        double Entropy(string s)
        {
            var cs = NextChars(s);
            var ns = cs.Select(c =>(double) Count(s + c));
            var sum = ns.Sum();
            return ns.Sum(v =>
            {
                if (v == 0) return 0;
                var p = v / sum;
                return -p * Math.Log(p);
            });
        }
        public List<string> Split(string s, int maxWordLength,Func<double,double,double, double,double, double> method, bool verbose)
        {
            try
            {
                if (verbose) Trace.Indent();
                if (verbose) Trace.WriteLine("Getting FPL...");
                var fpl = FrequencyPerLength();
                if (verbose) Trace.Write("OK");
                int n = s.Length;
                List<string> ans = new List<string>();
                int[] pre = new int[n + 1], cnt = new int[n + 1];
                double[] dp = new double[n + 1];
                dp[0] = 0;
                for (int i = 1; i <= n; i++) dp[i] = double.NegativeInfinity;
                pre[0] = 1;//crutial to make 0-pre[0]<0 when tracing back
                cnt[0] = 0;
                if (verbose) Trace.WriteLine("DPing...");
                int percentage = -1;
                for (int i = 0; i < n; i++)
                {
                    Parallel.For(1, Math.Min(n - i, maxWordLength) + 1, (l) =>
                    {
                        double probLog = method(l,Count(s.Substring(i, l)), Entropy(s.Substring(i, l)), fpl[l].mean, fpl[l].stderr);//count,entropy
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
                        if (verbose) Trace.WriteLine($"DPing... {++percentage}% Ex: {s.Substring(i - pre[i], pre[i])} scored {dp[i]} avg {(double)i / cnt[i]} words");
                    }
                }
                if (verbose) Trace.WriteLine("Tracing back...");
                List<int> idxs = new List<int>();
                for (int i = n; i >= 0; i -= pre[i]) idxs.Add(i);
                if (verbose) Trace.WriteLine("Picking words...");
                percentage = -1;
                for (int i = idxs.Count - 1; i > 0; i--)
                {
                    string _s = s.Substring(idxs[i], idxs[i - 1] - idxs[i]);
                    WordIdentified?.Invoke(_s);
                    ans.Add(_s);
                    if ((idxs.Count - i + 1) * 100L / idxs.Count > percentage)
                    {
                        if (verbose) Trace.WriteLine($"Picking words... {++percentage}% Ex: {_s}");
                    }
                }
                if (verbose) Trace.Write(" => OK");
                SplittedWords = ans;
                IsBuilt = true;
                return ans;
            }
            finally { if (verbose) Trace.Unindent(); }
        }
        public List<string> Split(string s, int maxWordLength, BEMSmodel bm,List<FPLtype>fpl, double probRatio, double bemsRatio, ProbTypeEnum probType, bool logPortion, bool verbose)
        {
            try
            {
                if (verbose) Trace.Indent();
                if (verbose) Trace.WriteLine("Getting FPL...");
                if (verbose) Trace.Write("OK");
                int n = s.Length;
                List<string> ans = new List<string>();
                int[] pre = new int[n + 1], cnt = new int[n + 1];
                double[] dp = new double[n + 1];
                dp[0] = 0;
                for (int i = 1; i <= n; i++) dp[i] = double.NegativeInfinity;
                pre[0] = 1;//crutial to make 0-pre[0]<0 when tracing back
                cnt[0] = 0;
                if (verbose) Trace.WriteLine("DPing...");
                int percentage = -1;
                for (int i = 0; i < n; i++)
                {
                    Parallel.For(1, Math.Min(n - i, maxWordLength) + 1, (l) =>
                           {
                               double wordCount = Count(s.Substring(i, l));
                               //if (wordCount <= 5)//fpl[l].percent10)
                               //{
                               //    wordCount = Math.Max(wordCount - 5, Math.Pow(((fpl[1].sqrtSum / (motherSA.S.Length / fpl[1].mean))) / motherSA.S.Length, l));
                               //}
                               double probLog =
                                   probType == ProbTypeEnum.CdL ? Math.Log(wordCount / (BaseDataLength() - l + 1)) :
                                   probType == ProbTypeEnum.CdM ? Math.Log(wordCount / fpl[l].mean) :
                                   probType == ProbTypeEnum.CxLdM ? Math.Log(wordCount / fpl[l].mean) * l :
                                   probType == ProbTypeEnum.CmMdSTDE ? (Count(s.Substring(i, l)) - fpl[l].mean) / Math.Pow(fpl[l].stderr, 1.0) :
                                   probType == ProbTypeEnum.sqCdS ? Math.Log((double)Math.Sqrt(wordCount) / (fpl[l].sqrtSum / (BaseDataLength() / fpl[l].mean))) :
                                   probType == ProbTypeEnum.sqCxLdS ? Math.Log((double)Math.Sqrt(wordCount) / (fpl[l].sqrtSum / (BaseDataLength() / fpl[l].mean))) * l :
                                   probType == ProbTypeEnum.lnCdS ? Math.Log(Math.Max(double.Epsilon, (double)Math.Log(wordCount) / (fpl[l].logSum / (BaseDataLength() / fpl[l].mean)))) :
                                   probType == ProbTypeEnum.lnCxLdS ? Math.Log(Math.Max(double.Epsilon, (double)Math.Log(wordCount) / (fpl[l].logSum / (BaseDataLength() / fpl[l].mean)))) * l :
                                   probType == ProbTypeEnum.Sigmoid ? 1.0 / (1.0 + Math.Exp(-(Count(s.Substring(i, l)) - fpl[l].mean) / fpl[l].stderr)) * l :
                                   probType == ProbTypeEnum.Entropy ? Entropy(s.Substring(i, l)) :
                                   probType == ProbTypeEnum.Entropy_L ? Entropy(s.Substring(i, l)) * l :
                                   throw new Exception($"Unknown probType: {probType}"); // (Count(sa, i, l) - fpl[l].Item1) / Math.Pow(fpl[l].Item2, 1.0);
                               if (bemsRatio != 0)
                               {
                                   double bemsLog = bm.Query(s.Substring(i, l));
                                   probLog = logPortion ? probLog * probRatio + bemsLog * bemsRatio : Math.Log(Math.Exp(probLog) * probRatio + Math.Exp(bemsLog) * bemsRatio);
                               }
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
                        if (verbose) Trace.WriteLine($"DPing... {++percentage}% Ex: {s.Substring(i - pre[i], pre[i])} scored {dp[i]} avg {(double)i / cnt[i]} words");
                    }
                }
                if (verbose) Trace.WriteLine("Tracing back...");
                List<int> idxs = new List<int>();
                for (int i = n; i >= 0; i -= pre[i]) idxs.Add(i);
                if (verbose) Trace.WriteLine("Picking words...");
                percentage = -1;
                for (int i = idxs.Count - 1; i > 0; i--)
                {
                    string _s = s.Substring(idxs[i], idxs[i - 1] - idxs[i]);
                    WordIdentified?.Invoke(_s);
                    ans.Add(_s);
                    if ((idxs.Count - i + 1) * 100L / idxs.Count > percentage)
                    {
                        if (verbose) Trace.WriteLine($"Picking words... {++percentage}% Ex: {_s}");
                    }
                }
                if (verbose) Trace.Write(" => OK");
                SplittedWords = ans;
                IsBuilt = true;
                return ans;
            }
            finally { if (verbose) Trace.Unindent(); }
        }
    }
}
