using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;

namespace WikiDataAnalysis
{
    class BEMSmodel
    {
        const string dicUrl = "https://raw.githubusercontent.com/fxsjy/jieba/master/jieba/finalseg/prob_emit.py";
        Dictionary<char, double> dic = null;
        async Task DownloadDictionaryAsync()
        {
            HttpClient client = new HttpClient();
            var data=await client.GetStringAsync(dicUrl);
            data = data.Remove(data.LastIndexOf('}')).Substring(data.IndexOf('{') + 1);
            throw new NotImplementedException();
        }
        public async Task Run(SentenceSplitter ss)
        {
            throw new NotImplementedException();
        }
    }
}
