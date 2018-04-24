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
        public delegate void WordIdentifiedEventHandler(string word);
        public event WordIdentifiedEventHandler WordIdentified;
        private List<Tuple<double,double>>FrequencyPerLength(SuffixArray sa)
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
                List<Tuple<double,double>> ans = new List<Tuple<double, double>>();
                ans.Resize(n + 1, default(Tuple<double, double>));
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
                    ans[i] = new Tuple<double, double>(u, Math.Sqrt((double)ro / (j + 2) - u * u));
                }
                Trace.Write("OK");
                //System.Windows.Forms.MessageBox.Show(string.Join(", ", ans.GetRange(0, 20)));
                return ans;
            }
            finally { Trace.Unindent(); }
        }
        private int Count(SuffixArray sa, int startIndex, int length)
        {
            int l, r, n = sa.S.Length;
            l = r = sa.RANK[startIndex];
            int m = 1;
            for (; l - m >= 0 && string.Compare(sa.S, sa.SA[l], sa.S, sa.SA[l - m], length) == 0; m <<= 1) l -= m;
            for (; m > 0 && l - m >= 0 && string.Compare(sa.S, sa.SA[l], sa.S, sa.SA[l - m], length) == 0; m >>= 1) l -= m;
            m = 1;
            for (; r + m < n && string.Compare(sa.S, sa.SA[r], sa.S, sa.SA[r + m], length) == 0; m <<= 1) r += m;
            for (; m > 0 && r + m < n && string.Compare(sa.S, sa.SA[r], sa.S, sa.SA[r + m], length) == 0; m >>= 1) r += m;
            return r - l + 1;
        }
        private int Cut(SuffixArray sa,int startIndex,List<Tuple<double,double>>fpl,int maxWordLength)
        {
            double currentMax = double.NegativeInfinity;
            int ans = -1;
            List<double> t = new List<double>();
            for(int l=maxWordLength;l>=1;l--)
            {
                double ratio = (Count(sa, startIndex, l) - Math.Pow(fpl[l].Item1, 1)) / fpl[l].Item2;
                t.Add(ratio);
                if(ratio>currentMax)
                {
                    currentMax=ratio;
                    ans = l;
                }
            }
            Trace.Assert(ans != -1);
            //System.Windows.Forms.MessageBox.Show($"{sa.S.Substring(startIndex, maxWordLength)}: {string.Join(", ", t)}");
            return ans;
        }
        public List<string> Split(SuffixArray sa, int maxWordLength = 10)
        {
            try
            {
                Trace.Indent();
                Trace.WriteLine("Getting FPL...");
                var fpl = FrequencyPerLength(sa);
                Trace.Write("OK");
                int i = 0, n = sa.S.Length;
                List<string> ans = new List<string>();
                Trace.WriteLine("Cutting...");
                int percentage = -1;
                while (i < n)
                {
                    int l = Cut(sa, i, fpl, Math.Min(maxWordLength, n - i));
                    string s = sa.S.Substring(i, l);
                    WordIdentified?.Invoke(s);
                    ans.Add(s);
                    i += l;
                    if ((i + 1) * 100 / n > percentage)
                    {
                        Trace.WriteLine($"Cutting... {++percentage}% Ex: {s}");
                    }
                }
                Trace.WriteLine("OK");
                return ans;
            }
            finally { Trace.Unindent(); }
        }
    }
}
