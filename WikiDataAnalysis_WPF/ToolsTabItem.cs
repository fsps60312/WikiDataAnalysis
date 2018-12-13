using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.Reflection;
using System.Threading;

namespace WikiDataAnalysis_WPF
{
    class ToolsTabItem:TabItem
    {
        List<object> GetProperties(Type t)
        {
            return t.GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.GetField | BindingFlags.GetProperty).Select(p => p.GetValue(null)).ToList();
        }
        async Task<T> Select<T>(List<T> s, string message = "Please select")
        {
            Window f = new Window();
            var grid = new Grid();
            for (int i = 0; i < s.Count + 1; i++) grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            T selected = default(T);
            SemaphoreSlim semaphore = new SemaphoreSlim(0);
            grid.Children.Add(new Label { Content = message }.Set(0, 0));
            for (int _ = 0; _ < s.Count; _++)
            {
                int i = _;
                var b = new Button { Content = s[i].ToString() };
                b.Click += delegate
                {
                    b.IsEnabled = false;
                    f.IsEnabled = false;
                    selected = s[i];
                    f.Title = $"#{i}: {b.Content}";
                    f.Close();
                };
                grid.Children.Add(b.Set(i + 1, 0));
            }
            f.Closed += delegate { lock (semaphore) semaphore.Release(); };
            f.Content = new ScrollViewer { Content = grid };
            f.Show();
            await semaphore.WaitAsync();
            return selected;
        }
        void InitializeButtons(List<Tuple<string, Action>> bs)
        {
            bs.Add(new Tuple<string, Action>("Convert Encoding", async () =>
            {
                Encoding encodingSource, encodingTarget;
                var encodingList = GetProperties(typeof(Encoding)).Select(v => v as Encoding).ToList();
                if ((encodingSource = await Select(encodingList, $"Source Encoding ({encodingList.Count})")) == null
                || (encodingTarget = await Select(encodingList, $"Target Encoding ({encodingList.Count})")) == null) return;
                await MyLib.OpenSave(async (fs, ss) =>
                {
                    try
                    {
                        Log.Indent();
                        Log.WriteLine($"Converting: {encodingSource} → {encodingTarget}");
                        using (var reader = new StreamReader(fs, encodingSource))
                        {
                            using (var writer = new StreamWriter(ss,false, encodingTarget))
                            {
                                const int bufLen = 1 << 20;
                                var buf = new char[bufLen];
                                long progress = 0, total_progress = reader.BaseStream.Length;
                                DateTime time = DateTime.Now;
                                for (int n; (n = await reader.ReadAsync(buf, 0, buf.Length)) > 0;)
                                {
                                    await writer.WriteAsync(buf, 0, n);
                                    progress += n;
                                    if ((DateTime.Now - time).TotalSeconds > 0.5)
                                    {
                                        Log.WriteLine($"Converting: {(double)progress * 100 / total_progress}% {encodingSource} → {encodingTarget}");
                                        time = DateTime.Now;
                                    }
                                }
                            }
                        }
                        Log.WriteLine($"Convert OK: {encodingSource} → {encodingTarget}");
                    }
                    finally { Log.Unindent(); }
                });
            }));
            bs.Add(new Tuple<string, Action>("Convert Trie to WordList (file to file)", async () =>
            {
                await MyLib.OpenSave((fs, ss) => Log.SubTask(async () =>
                 {
                     Log.WriteLine($"Reading... file size: {fs.Length}");
                     var trie = new Trie();
                     await Task.Run(() => trie.Load(new FileStream(fs, FileMode.Open, FileAccess.Read)));
                     Log.WriteLine("Exporting...");
                     await trie.ExportList(new FileStream(ss, FileMode.Create, FileAccess.Write));
                     Log.WriteLine($"Done");
                 }));
            }));
        }
        public ToolsTabItem()
        {
            this.Header = "Tools";
            List<Tuple<string, Action>> buttonSettings = new List<Tuple<string, Action>>();
            InitializeButtons(buttonSettings);
            this.Content = new ScrollViewer
            {
                Content = new Grid().Do(o=>
                {
                    var g = o as Grid;
                    for (int i = 0; i < buttonSettings.Count; i++)
                    {
                        g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                        var p = buttonSettings[i];
                        var b = new Button { Content = p.Item1 };
                        b.Click += delegate { p.Item2(); };
                        g.Children.Add(b.Set(i, 0));
                    }
                })
            };
            Log.AppendLog("ToolsTabItem OK.");
        }
    }
}
