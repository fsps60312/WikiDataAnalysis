using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace WikiDataAnalysis
{
    static class Utils
    {
        public static void Swap<T>(ref T a,ref T b)
        {
            T c = a;
            a = b;
            b = c;
        }
        static int counter = 0;
        public static void Resize<T>(this List<T>s,int size,T v)
        {
            try
            {
                Trace.Indent();
                Trace.WriteLine($"Resize({size},{v}) #{++counter}...");
                while (s.Count < size)
                {
                    s.Add(v);
                }
                if (s.Count > size) s.RemoveRange(size, s.Count - size);
            }
            finally { Trace.Write("Done"); Trace.Unindent(); }
        }
    }
}
