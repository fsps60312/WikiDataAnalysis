using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;
using System.Diagnostics;

namespace WikiDataAnalysis
{
    class BEMSmodel
    {
        public enum BEMS { B,E,M,S};
        const string dicUrl = "https://raw.githubusercontent.com/fxsjy/jieba/master/jieba/finalseg/prob_emit.py";
        public Dictionary<char, Dictionary<char, double>> dic { get; private set; }
        double defaultMinValue;
        public async Task DownloadDictionaryAsync()
        {
            try
            {
                Trace.Indent();
                HttpClient client = new HttpClient();
                Trace.WriteLine($"Downloading from {dicUrl}");
                var data = await client.GetStringAsync(dicUrl);
                data = data.Remove(data.LastIndexOf('}') + 1).Substring(data.IndexOf('{'));
                Trace.WriteLine("Deserializing BEMS...");
                //Console.WriteLine(data.Remove(1000) + "\r\n===============\r\n" + data.Substring(data.Length - 1000));
                dic = JsonConvert.DeserializeObject<Dictionary<char, Dictionary<char, double>>>(data);
                defaultMinValue = dic.Select(p => p.Value.Select(q => q.Value).Min()).Min();
                defaultMinValue *= 2;
                Trace.WriteLine("OK");
            }
            finally { Trace.Unindent(); }
        }
        public double Query(BEMS b,char c)
        {
            var d=dic[b == BEMS.B ? 'B' : b == BEMS.E ? 'E' : b == BEMS.M ? 'M' : 'S'];
            if (d.ContainsKey(c)) return d[c];
            return defaultMinValue;
        }
        public double Query(string s)
        {
            Trace.Assert(s.Length >= 1);
            if (s.Length == 1) return Query(BEMS.S, s[0]);
            double ans = Query(BEMS.B, s[0]) + Query(BEMS.E, s[s.Length - 1]);
            for (int i = 1; i + 1 < s.Length; i++) ans += Query(BEMS.M, s[i]);
            return ans;
        }
    }
}
