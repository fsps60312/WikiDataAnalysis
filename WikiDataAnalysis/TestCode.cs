using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WikiDataAnalysis
{
    class TestCode
    {
        public static async Task Run()
        {
            //await Run1();
        }
        static async Task Run1()
        {
            var bm = new BEMSmodel();
            await bm.DownloadDictionaryAsync();
            MessageBox.Show(string.Join("\r\n", bm.dic.Select(p => $"{p.Key}: {p.Value.Sum(new Func<KeyValuePair<char, double>, double>(a => Math.Exp(a.Value)))}")));
        }
    }
}
