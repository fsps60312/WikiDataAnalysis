using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Motivation;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;

namespace WikiDataAnalysis
{
    class ToolsTabPage:MyTabPage
    {
        Stream Open()
        {
            var fd = new OpenFileDialog();
            //fd.FileName = "wiki.sav";
            if (fd.ShowDialog() == DialogResult.OK)
            {
                return fd.OpenFile();
            }
            return null;
        }
        Stream Save()
        {
            var sd = new SaveFileDialog();
            if (sd.ShowDialog() == DialogResult.OK)
            {
                return sd.OpenFile();
            }
            return null;
        }
        void OpenSave(Action<Stream, Stream> action)
        {
            using (var fs = Open())
            {
                if (fs == null) return;
                using (var ss = Save())
                {
                    if (ss == null) return;
                    action(fs, ss);
                }
            }
        }
        async void OpenSave(Func<Stream, Stream,Task> action)
        {
            using (var fs = Open())
            {
                if (fs == null) return;
                using (var ss = Save())
                {
                    if (ss == null) return;
                    await action(fs, ss);
                }
            }
        }
        List<object>GetProperties(Type t)
        {
            return t.GetProperties(BindingFlags.Static|BindingFlags.Public|BindingFlags.GetField|BindingFlags.GetProperty).Select(p=>p.GetValue(null)).ToList();
        }
        async Task<T> Select<T>(List<T>s,string message="Please select")
        {
            Form f = new Form();
            var tlp = new MyTableLayoutPanel(1, s.Count + 2, "P", new string('A', s.Count+1) + "P");
            T selected = default(T);
            SemaphoreSlim semaphore = new SemaphoreSlim(0);
            tlp.Controls.Add(new MyLabel(message), 0, 0);
            for (int _ = 0; _ < s.Count; _++)
            {
                int i = _;
                var b = new MyButton(s[i].ToString());
                b.Click +=async delegate
                {
                    b.Enabled = false;
                    f.Enabled = false;
                    selected = s[i];
                    f.Text = $"#{i}: {b.Text}";
                    f.Close();
                    f.Dispose();
                };
                tlp.Controls.Add(b, 0, i+1);
            }
            f.FormClosed += delegate { lock (semaphore) semaphore.Release(); };
            tlp.Dock = DockStyle.Top;
            f.Controls.Add(new MyPanel() { Controls = { tlp }, AutoScroll = true });
            f.Show();
            await semaphore.WaitAsync();
            return selected;
        }
        void InitializeButtons(List<Tuple<string, Action>>bs)
        {
            bs.Add(new Tuple<string, Action>("Convert Encoding",async () =>
             {
                 Encoding encodingSource, encodingTarget;
                 var encodingList = GetProperties(typeof(Encoding)).Select(v => v as Encoding).ToList();
                 if ((encodingSource = await Select(encodingList, $"Source Encoding ({encodingList.Count})")) == null
                 || (encodingTarget = await Select(encodingList, $"Target Encoding ({encodingList.Count})")) == null) return;
                 OpenSave(async (fs, ss) =>
                 {
                     try
                     {
                         Trace.Indent();
                         Trace.WriteLine($"Converting: {encodingSource} → {encodingTarget}");
                         using (var reader = new StreamReader(fs, encodingSource))
                         {
                             using (var writer = new StreamWriter(ss, encodingTarget))
                             {
                                 const int bufLen = 1 << 20;
                                 var buf = new char[bufLen];
                                 long progress = 0,total_progress=reader.BaseStream.Length;
                                 DateTime time = DateTime.Now;
                                 for(int n;(n=await reader.ReadAsync(buf,0,buf.Length))>0;)
                                 {
                                     await writer.WriteAsync(buf, 0, n);
                                     progress += n;
                                     if((DateTime.Now-time).TotalSeconds>0.5)
                                     {
                                         Trace.WriteLine($"Converting: {(double)progress * 100 / total_progress}% {encodingSource} → {encodingTarget}");
                                         time = DateTime.Now;
                                     }
                                 }
                             }
                         }
                         Trace.WriteLine($"Convert OK: {encodingSource} → {encodingTarget}");
                     }
                     finally { Trace.Unindent(); }
                 });
             }));
            bs.Add(new Tuple<string, Action>("Convert Trie to WordList (file to file)", () =>
            {
                OpenSave(async (fs, ss) =>
                {
                    try
                    {
                        Trace.Indent();
                        Trace.WriteLine($"Reading... file size: {fs.Length}");
                        var trie = new Trie();
                        await Task.Run(() => trie.Load(fs));
                        Trace.WriteLine("Exporting...");
                        await trie.ExportList(ss);
                        Trace.WriteLine($"Done");
                    }
                    finally { Trace.Unindent(); }
                });
            }));
        }
        public ToolsTabPage():base("Tools")
        {
            List<Tuple<string, Action>> buttonSettings = new List<Tuple<string, Action>>();
            InitializeButtons(buttonSettings);
            var tlp = new MyTableLayoutPanel(1, buttonSettings.Count+1, "P", new string('A', buttonSettings.Count)+"P");
            for(int i=0;i<buttonSettings.Count;i++)
            {
                var p = buttonSettings[i];
                var b = new MyButton(p.Item1);
                b.Click += delegate { p.Item2(); };
                tlp.Controls.Add(b, 0, i);
            }
            tlp.Dock = DockStyle.Top;
            this.Controls.Add(new MyPanel() { Controls = { tlp }, AutoScroll = true });
        }
    }
}
