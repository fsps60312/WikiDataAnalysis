using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace WikiDataAnalysis
{
    class _StringBuilder
    {
        List<System.Text.StringBuilder> stringBuilders = new List<System.Text.StringBuilder>();
        public void Clear()
        {
            stringBuilders.Clear();
        }
        private void Check()
        {
            if (stringBuilders.Count == 0 || stringBuilders.Last().Length > int.MaxValue / 10) stringBuilders.Add(new System.Text.StringBuilder());
        }
        public void Append(char c)
        {
            Check();
            stringBuilders.Last().Append(c);
        }
        public void AppendLine(string s)
        {
            Check();
            stringBuilders.Last().AppendLine(s);
        }
        public long Length { get { return stringBuilders.Sum(v => (long)v.Length); } }
        public new List<char> ToString()
        {
            try
            {
                Trace.Indent();
                List<char> ans =new List<char>();
                for (int i = 0; i < stringBuilders.Count; i++)
                {
                    Trace.WriteLine($"Concat {ans.LongCount()}\t{stringBuilders[i].Length}");
                    ans.AddRange(stringBuilders[i].ToString());
                }
                return ans;
            }
            finally { Trace.Unindent(); }
        }
    }
}
