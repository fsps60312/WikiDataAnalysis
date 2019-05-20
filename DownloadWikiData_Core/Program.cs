using System;

namespace DownloadWikiData_Core
{
    partial class Program
    {
        static void Main(string[] args)
        {
            Console.Out.Write(new Runner().Run(Console.In.ReadToEnd()));
        }
    }
}
