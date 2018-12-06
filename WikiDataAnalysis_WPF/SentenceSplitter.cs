using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WikiDataAnalysis_WPF
{
    class FPLtype
    {
        public double mean, stderr, sqrtSum, logSum;
    }
    class SentenceSplitter
    {
        public static class MethodsForTrie
        {
            public static List<FPLtype> FrequencyPerLength(Trie trie)
            {
                return Log.SubTask(() =>
                {
                    List<FPLtype> ans = new List<FPLtype>();
                    List<long> cnt = new List<long>();
                    int l = 0;
                    Log.WriteLine($"Traversing... Size: {trie.Size}");
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
                    Log.WriteLine("Building ans...");
                    for (int i = 0; i < ans.Count; i++)
                    {
                        ans[i].mean /= cnt[i];
                        ans[i].stderr = Math.Sqrt(ans[i].stderr / cnt[i] - ans[i].mean * ans[i].mean);
                    }
                    Log.Write("OK");
                    return ans;
                });
            }
        }
        List<FPLtype> fpl_cache = null;
        Func<List<FPLtype>> FrequencyPerLength;
        Func<string, int> Count;
        Func<int> BaseDataLength;
        Func<string, List<char>> NextChars;
        public SentenceSplitter(Trie trie, int baseDataLength)
        {
            FrequencyPerLength = () => { if (fpl_cache == null) fpl_cache = MethodsForTrie.FrequencyPerLength(trie); return fpl_cache; };
            Count = s => (int)trie.Count(s);
            BaseDataLength = () => baseDataLength;
            NextChars = s => trie.NextChars(s);
        }
        public delegate void WordIdentifiedEventHandler(string word);
        public event WordIdentifiedEventHandler WordIdentified;
        public async Task<List<string>> SplitAsync(string s, int maxWordLength, ProbTypeEnum probType, bool verbose)
        {
            return await Task.Run(() => Split(s, maxWordLength
                , probType == ProbTypeEnum.Entropy || probType == ProbTypeEnum.Entropy_L ? null : FrequencyPerLength()
                , probType, verbose));
        }
        public bool IsBuilt { get; private set; } = false;
        public List<string> SplittedWords { get; private set; }
        public enum ProbTypeEnum { CdL, CdM, CxLdM, CmMdSTDE, sqCdS, sqCxLdS, lnCdS, lnCxLdS, Sigmoid, Entropy, Entropy_L }
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
            var ns = cs.Select(c => (double)Count(s + c));
            var sum = ns.Sum();
            return ns.Sum(v =>
            {
                if (v == 0) return 0;
                var p = v / sum;
                return -p * Math.Log(p);
            });
        }
        public List<string> Split(string s, int maxWordLength, Func<double, double, double, double, double, double> method, bool verbose)
        {
            try
            {
                if (verbose) Log.Indent();
                if (verbose) Log.WriteLine("Getting FPL...");
                var fpl = FrequencyPerLength();
                if (verbose) Log.Write("OK");
                int n = s.Length;
                List<string> ans = new List<string>();
                int[] pre = new int[n + 1], cnt = new int[n + 1];
                double[] dp = new double[n + 1];
                dp[0] = 0;
                for (int i = 1; i <= n; i++) dp[i] = double.NegativeInfinity;
                pre[0] = 1;//crutial to make 0-pre[0]<0 when tracing back
                cnt[0] = 0;
                if (verbose) Log.WriteLine("DPing...");
                int percentage = -1;
                for (int i = 0; i < n; i++)
                {
                    Parallel.For(1, Math.Min(n - i, maxWordLength) + 1, (l) =>
                    {
                        double probLog = method(l, Count(s.Substring(i, l)), Entropy(s.Substring(i, l)), fpl[l].mean, fpl[l].stderr);//count,entropy
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
                        if (verbose) Log.WriteLine($"DPing... {++percentage}% Ex: {s.Substring(i - pre[i], pre[i])} scored {dp[i]} avg {(double)i / cnt[i]} words");
                    }
                }
                if (verbose) Log.WriteLine("Tracing back...");
                List<int> idxs = new List<int>();
                for (int i = n; i >= 0; i -= pre[i]) idxs.Add(i);
                if (verbose) Log.WriteLine("Picking words...");
                percentage = -1;
                for (int i = idxs.Count - 1; i > 0; i--)
                {
                    string _s = s.Substring(idxs[i], idxs[i - 1] - idxs[i]);
                    WordIdentified?.Invoke(_s);
                    ans.Add(_s);
                    if ((idxs.Count - i + 1) * 100L / idxs.Count > percentage)
                    {
                        if (verbose) Log.WriteLine($"Picking words... {++percentage}% Ex: {_s}");
                    }
                }
                if (verbose) Log.Write(" => OK");
                SplittedWords = ans;
                IsBuilt = true;
                return ans;
            }
            finally { if (verbose) Log.Unindent(); }
        }
        public List<string> Split(string s, int maxWordLength, List<FPLtype> fpl, ProbTypeEnum probType, bool verbose)
        {
            try
            {
                if (verbose) Log.Indent();
                if (verbose) Log.WriteLine("Getting FPL...");
                if (verbose) Log.Write("OK");
                int n = s.Length;
                List<string> ans = new List<string>();
                int[] pre = new int[n + 1], cnt = new int[n + 1];
                double[] dp = new double[n + 1];
                dp[0] = 0;
                for (int i = 1; i <= n; i++) dp[i] = double.NegativeInfinity;
                pre[0] = 1;//crutial to make 0-pre[0]<0 when tracing back
                cnt[0] = 0;
                if (verbose) Log.WriteLine("DPing...");
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
                        if (verbose) Log.WriteLine($"DPing... {++percentage}% Ex: {s.Substring(i - pre[i], pre[i])} scored {dp[i]} avg {(double)i / cnt[i]} words");
                    }
                }
                if (verbose) Log.WriteLine("Tracing back...");
                List<int> idxs = new List<int>();
                for (int i = n; i >= 0; i -= pre[i]) idxs.Add(i);
                if (verbose) Log.WriteLine("Picking words...");
                percentage = -1;
                for (int i = idxs.Count - 1; i > 0; i--)
                {
                    string _s = s.Substring(idxs[i], idxs[i - 1] - idxs[i]);
                    WordIdentified?.Invoke(_s);
                    ans.Add(_s);
                    if ((idxs.Count - i + 1) * 100L / idxs.Count > percentage)
                    {
                        if (verbose) Log.WriteLine($"Picking words... {++percentage}% Ex: {_s}");
                    }
                }
                if (verbose) Log.Write(" => OK");
                SplittedWords = ans;
                IsBuilt = true;
                return ans;
            }
            finally { if (verbose) Log.Unindent(); }
        }
    }
}
