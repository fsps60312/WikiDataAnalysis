using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace WikiDataAnalysis
{
    class Trie
    {
        public long Size { get { return CNT.Count(); } }
        public Trie() { Clear(); }
        public bool IsBuilt { get { return true; } }
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
        public void InsertNonemptySuffixes(string s)
        {
            int u = 0;
            foreach (var c in s)
            {
                u = GetNxt(u, c);
                ++CNT[u];
            }
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
        public async Task BuildAsync(string maindata,int maxWordLength)
        {
            Trace.Indent();
            try
            {
                Clear();
                await Task.Run(() =>
                {
                    int percent = -1;
                    var dataa = maindata.Split(' ', '\r', '\n');
                    long progress = 0, total_progress = dataa.Sum(s => (long)s.Length);
                    foreach (var data in dataa)
                    {
                        for (int i = 0; i < data.Length; i++, progress++)
                        {
                            if ((progress + 1) * 100L / total_progress > percent)
                            {
                                percent++;
                                Trace.WriteLine($"Trie.BuildAsync: {percent}%");
                            }
                            string s = data.Substring(i, Math.Min(maxWordLength, data.Length - i));
                            InsertNonemptySuffixes(s);
                        }
                    }
                });
                Trace.WriteLine("Trie.BuildAsync: OK");
            }
            finally { Trace.Unindent(); }
        }
        async Task ExportList(int u, string s, StreamWriter writer)
        {
            if (CNT[u] > 0) await writer.WriteLineAsync($"{s},{CNT[u]}");
            foreach (var p in CH[u])
            {
                await ExportList(p.Value, s + p.Key, writer);
            }
        }
        public async Task ExportList(Stream stream)
        {
            try
            {
                Trace.Indent();
                using (var writer = new StreamWriter(stream, Encoding.UTF8))
                {
                    await ExportList(0, "", writer);
                    Trace.WriteLine("Trie.ExportList: Done");
                }
            }
            finally { Trace.Unindent(); }
        }
        void Traverse(int u,Action<char> goDown,Action goUp,Action<long>reach)
        {
            reach(CNT[u]);
            foreach(var p in CH[u])
            {
                goDown(p.Key);
                Traverse(p.Value, goDown, goUp, reach);
                goUp();
            }
        }
        public void Traverse(Action<char> goDown, Action goUp, Action<long> reach)
        {
            Traverse(0, goDown, goUp, reach);
        }
        void Load(int u,BinaryReader reader)//1 true a 2 false false
        {
            //Trace.Write($"{u} ");
            CNT[u] = reader.ReadInt64();
            while(reader.ReadBoolean())
            {
                var c = reader.ReadString()[0];
                Expand();
                var nxt = CNT.Count - 1;
                try
                {
                    CH[u].Add(c, nxt);
                }
                catch(Exception)
                {
                    Trace.WriteLine($"u={u},nxt={nxt},c={c}/{((int) c).ToString("X")}");
                    //throw;
                }
                Load(nxt, reader);
            }
        }
        public void Load(Stream stream)
        {
            try
            {
                Trace.Indent();
                Trace.WriteLine("Trie.Load...");
                Clear();
                using (var reader = new BinaryReader(stream, Encoding.UTF8))
                {
                    Load(0, reader);
                }
                Trace.Write(" OK");
            }
            finally { Trace.Unindent(); }
        }
        public void Save(Stream stream)
        {
            try
            {
                Trace.Indent();
                long progress = 0, total_progress = CNT.Count,percent=-1;
                Trace.WriteLine("Trie.Save");
                using (var writer = new BinaryWriter(stream, Encoding.UTF8))
                {
                    Traverse(c =>
                    {
                        writer.Write(true);
                        writer.Write(c.ToString());
                    },()=>
                    {
                        writer.Write(false);
                    },c=>
                    {
                        if((++progress)*100/total_progress>percent)
                        {
                            Trace.WriteLine($"Trie.Save... {++percent}%");
                        }
                        writer.Write(c);
                    });
                    writer.Write(false);
                }
                Trace.WriteLine("OK");
            }
            finally { Trace.Unindent(); }
        }
    }
}
