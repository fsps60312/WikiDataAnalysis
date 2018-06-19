using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;

namespace DownloadWikiData
{
    class BEMSmodel
    {
        const string dicUrl = "https://raw.githubusercontent.com/fxsjy/jieba/master/jieba/finalseg/prob_emit.py";
        Dictionary<char, Dictionary<char,double>> dic = null;
        public async Task DownloadDictionaryAsync()
        {
            HttpClient client = new HttpClient();
            var data=await client.GetStringAsync(dicUrl);
            data = data.Remove(data.LastIndexOf('}')+1).Substring(data.IndexOf('{'));
            //Console.WriteLine(data.Remove(1000) + "\r\n===============\r\n" + data.Substring(data.Length - 1000));
            dic = JsonConvert.DeserializeObject<Dictionary<char, Dictionary<char, double>>>(data);
            //var s = dic.Select(p => $"Key={p.Key}: {p.Value.Count}").ToList();
            //Console.WriteLine(s.Count);
            //Console.WriteLine(string.Join("\r\n", s));
        }
    }
}
