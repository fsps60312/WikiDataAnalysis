using System;
using System.Text;
using System.Windows;
using System.IO;

namespace WikiDataAnalysis_WPF
{
    partial class TrieTabItem
    {
        static class TextProcess
        {
            public static string ReadTextStream(Stream s, Encoding encoding, bool debugMode)
            {
                var sb = new StringBuilder();
                using (StreamReader reader = new StreamReader(s, encoding))
                {
                    Log.WriteLine("Reading...");
                    bool warning = true;
                    for (char[] buf = new char[1024 * 1024]; ;)
                    {
                        int n = reader.ReadBlock(buf, 0, buf.Length);
                        if (n == 0) break;
                        for (int i = 0; i < n; i++)
                        {
                            sb.Append(buf[i]);
                            const int stringMaxLength = int.MaxValue / 2 - 100;
                            if (warning && sb.Length > stringMaxLength)
                            {
                                warning = false;
                                if (MessageBox.Show($"Reach C# string max length: {sb.Length}, break?", "Warning", MessageBoxButton.OKCancel) == MessageBoxResult.OK) goto index_skipRead;
                            }
                        }
                        Log.WriteLine($"Reading...{s.Position}/{s.Length}");
                        if (debugMode && s.Position > 1000000) break;
                    }
                    index_skipRead:;
                    return sb.ToString();
                }
            }
            public static void Process(ref string data, int methodId)
            {
                var sb = new StringBuilder();
                switch (methodId)
                {
                    case 0: return;
                    case 1://non-chinese => space
                        {
                            int percentage = -1;
                            long progress = 0, total_progress = data.Length;
                            char last_char = '\0';
                            foreach (char c in data)
                            {
                                if (++progress * 100 / total_progress > percentage) Log.WriteLine($"Preprocessing {++percentage}%: non-chinese => space");
                                if (!MyLib.IsChinese(c))
                                {
                                    if (last_char != ' ') sb.Append(last_char = ' ');
                                }
                                else sb.Append(last_char = c);
                            }
                            data = sb.ToString();
                            return;
                        }
                    case 2://non-chinese => removed
                        {
                            int percentage = -1;
                            long progress = 0, total_progress = data.Length;
                            char last_char = '\0';
                            foreach (char c in data)
                            {
                                if (++progress * 100 / total_progress > percentage) Log.WriteLine($"Preprocessing {++percentage}%: non-chinese => space");
                                if (MyLib.IsChinese(c)) sb.Append(last_char = c);
                            }
                            data = sb.ToString();
                            return;
                        }
                    default: throw new Exception($"Unknown methodId: {methodId}");
                }
            }
        }
    }
}
