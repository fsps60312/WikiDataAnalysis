using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace WikiDataAnalysis_WPF
{
    class Trie
    {
        public long Size { get { return CNT.Count(); } }
        public Trie() { Clear(); }
        private bool _IsBuilt = false;
        public bool IsBuilt { get { return _IsBuilt; } }
        List<Dictionary<char, int>> CH = new List<Dictionary<char, int>>();
        List<long> CNT = new List<long>();
        public void Clear()
        {
            CH.Clear(); CNT.Clear();
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
        public void Insert(string s, long count = 1)
        {
            int u = 0;
            foreach (var c in s)
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
            foreach (var c in s)
            {
                if (!CH[u].ContainsKey(c)) return 0;
                u = CH[u][c];
            }
            return CNT[u];
        }
        public List<char> NextChars(string s)
        {
            int u = 0;
            foreach (var c in s)
            {
                if (!CH[u].ContainsKey(c)) return new List<char>();
                u = CH[u][c];
            }
            return CH[u].Keys.ToList();
        }
        public void Build(string maindata, int maxWordLength)
        {
            Log.SubTask(() =>
            {
                Clear();
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
                            Log.WriteLine($"Trie.BuildAsync: {percent}%");
                        }
                        string s = data.Substring(i, Math.Min(maxWordLength, data.Length - i));
                        InsertNonemptySuffixes(s);
                    }
                }
                Log.WriteLine("Trie.BuildAsync: OK");
                _IsBuilt = true;
            });
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
            await Log.SubTask(async() =>
            {
                using (var writer = new StreamWriter(stream, Encoding.UTF8))
                {
                    await ExportList(0, "", writer);
                    Log.WriteLine("Trie.ExportList: Done");
                }
            });
        }
        void Traverse(int u, Action<char> goDown, Action goUp, Action<long> reach)
        {
            reach(CNT[u]);
            foreach (var p in CH[u])
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
        void Load(int u, BinaryReader reader)//1 true a 2 false false
        {
            //Trace.Write($"{u} ");
            CNT[u] = reader.ReadInt64();
            while (reader.ReadBoolean())
            {
                var c = reader.ReadString()[0];
                Expand();
                var nxt = CNT.Count - 1;
                try
                {
                    CH[u].Add(c, nxt);
                }
                catch (Exception)
                {
                    Log.WriteLine($"u={u},nxt={nxt},c={c}/{((int)c).ToString("X")}");
                    //throw;
                }
                Load(nxt, reader);
            }
        }
        public void Load(Stream stream)
        {
            Log.SubTask(() =>
            {
                Log.WriteLine("Trie.Load...");
                Clear();
                using (var reader = new BinaryReader(stream, Encoding.UTF8))
                {
                    Load(0, reader);
                }
                Log.Write(" OK");
            });
        }
        public void Save(Stream stream)
        {
            Log.SubTask(() =>
            {
                long progress = 0, total_progress = CNT.Count, percent = -1;
                Log.WriteLine("Trie.Save");
                using (var writer = new BinaryWriter(stream, Encoding.UTF8))
                {
                    Traverse(c =>
                    {
                        writer.Write(true);
                        writer.Write(c.ToString());
                    }, () =>
                    {
                        writer.Write(false);
                    }, c =>
                    {
                        if ((++progress) * 100 / total_progress > percent)
                        {
                            Log.WriteLine($"Trie.Save... {++percent}%");
                        }
                        writer.Write(c);
                    });
                    writer.Write(false);
                }
                Log.WriteLine("OK");
            });
        }
        public void Decay(double decayRatio)
        {
            for (int i = 0; i < CNT.Count; i++) CNT[i] = (long)Math.Floor(CNT[i] * decayRatio);
        }
    }
}
