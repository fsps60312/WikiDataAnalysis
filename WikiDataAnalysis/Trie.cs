using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace WikiDataAnalysis
{
    class Trie
    {
        List<Dictionary<char, int>> CH = new List<Dictionary<char, int>>();
        List<long> CNT = new List<long>();
        public void Clear()
        {
            CH.Clear();CNT.Clear();
            Expand();
        }
        void Expand()
        {
            CH.Add(new Dictionary<char, int>());
            CNT.Add(0);
        }
        int GetNxt(int u, char c)
        {
            if (!CH[u].ContainsKey(c))
            {
                Expand();
                CH[u].Add(c, CNT.Count - 1);
            }
            return CH[u][c];
        }
        public void Insert(string s,long count=1)
        {
            int u = 0;
            foreach(var c in s)
            {
                u = GetNxt(u, c);
            }
            CNT[u] += count;
        }
        public long Count(string s)
        {
            int u = 0;
            foreach(var c in s)
            {
                if (!CH[u].ContainsKey(c)) return 0;
                u = CH[u][c];
            }
            return CNT[u];
        }
        async Task ExportList(int u,string s,Stream stream)
        {
        }
        public async Task ExportList(Stream stream)
        {

        }
    }
}
