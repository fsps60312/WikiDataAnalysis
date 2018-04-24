using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;

namespace WikiDataAnalysis
{
    class SimpleMethod
    {
        public delegate void StringEventHandler(string s);
        public event StringEventHandler StatusChanged;
        void UpdateStatus(string s)
        {
            Trace.WriteLine(s);
            StatusChanged?.Invoke(s);
            System.Windows.Forms.Application.DoEvents();
        }
        long ope_counter = 0;
        public void Calculate(string data,int depth)
        {
            Thread thread = new Thread(() =>
            {
                while (true)
                {
                    Thread.Sleep(5000);
                    UpdateStatus($"{ope_counter} / {data.Length} operations done");
                }
            });
            new Thread(() =>
            {
                var dict = new List<Dictionary<string, int>>();
                dict.Add(new Dictionary<string, int>());
                thread.Start();
                for (int len = depth; len <= depth; len++)
                {
                    while(dict.Count<=len)dict.Add(new Dictionary<string, int>());
                    UpdateStatus($"len={len}");
                    ope_counter = 0;
                    for (int i = len; i <= data.Length; i++)
                    {
                        string s = data.Substring(i - len, len);
                        if (!dict[len].ContainsKey(s)) dict[len].Add(s, 0);
                        dict[len][s]++;
                        ope_counter++;
                    }
                }
                thread.Abort();
                var result = new List<KeyValuePair<string, int>>();
                foreach (var s in dict) foreach (var p in s) result.Add(p);
                result.Sort((a, b) => { return a.Value == b.Value ? 0 : (a.Value < b.Value ? 1 : -1); });
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < 100 && i < result.Count; i++)
                {
                    var msg = $"{result[i].Key}: \t{result[i].Value}";
                    Trace.WriteLine(msg);
                    sb.AppendLine(msg);
                }
                System.Windows.Forms.MessageBox.Show(sb.ToString());
            }).Start();
        }
    }
}
