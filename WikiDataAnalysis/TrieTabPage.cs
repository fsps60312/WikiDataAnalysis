using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Motivation;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;
using System.IO;

namespace WikiDataAnalysis
{
    class TrieTabPage:MyTabPage
    {
        MyTableLayoutPanel TLPmain = new MyTableLayoutPanel(1, 3, "P", "P2P2P"), TLPtop = new MyTableLayoutPanel(2, 1, "P2P", "P");
        MyTableLayoutPanel TLPctrl = new MyTableLayoutPanel(1, 11, "P", "PPPPPPPPAPP") { Dock = DockStyle.Top };
        MyTextBox TXBin = new MyTextBox(true), TXBout = new MyTextBox(true), TXBdata = new MyTextBox(true);
        MyButton BTNexportList = new MyButton("Export List"),BTNnew = new MyButton("New data");
        MyButton BTNsave = new MyButton("Save Trie"), BTNload = new MyButton("Load Trie");
        MyButton BTNiteration = new MyButton("Perform Iteration");
        ComboBox CBmethod = new ComboBox { Dock = DockStyle.Fill, Font = new Font("微軟正黑體", 15) };
        ComboBox CBprobType = new ComboBox { Dock = DockStyle.Fill, Font = new Font("微軟正黑體", 10) };
        MyInputField IFdata = new MyInputField();
        MyCheckBox
            CHBdebugMode = new MyCheckBox("Debug Mode") { Checked = true },
            CHBlogPortion = new MyCheckBox("Log Portion") { Checked = true },
            CHBsplit = new MyCheckBox("Split") { Checked = false };
        int maxWordLength = 4;
        double bemsRatio = 0;
        double probRatio = 1;
        SentenceSplitter.ProbTypeEnum probType = SentenceSplitter.ProbTypeEnum.Sigmoid;
        string data = null;
        int baseDataLength;
        string txbDataFileContent = null;
        async Task NewData()
        {
            var fd = new OpenFileDialog();
            if (fd.ShowDialog() == DialogResult.OK)
            {
                using (var s = fd.OpenFile())
                {
                    if (s == null)
                    {
                        MessageBox.Show("File not opened");
                        return;
                    }
                    var encodingSelected = MessageBox.Show("\"Yes\" to use UTF-8\r\n\"No\" to use UTF-16 (Unicode)", "", MessageBoxButtons.YesNoCancel);
                    if (encodingSelected == DialogResult.Cancel) return;
                    Trace.Assert(encodingSelected == DialogResult.Yes || encodingSelected == DialogResult.No);
                    using (StreamReader reader = new StreamReader
                        (s, encodingSelected == DialogResult.Yes ? Encoding.UTF8 : Encoding.Unicode))
                    {
                        Trace.WriteLine("Reading...");
                        var sb = new StringBuilder();
                        for (char[] buf = new char[1024 * 1024]; ;)
                        {
                            int n = await reader.ReadAsync(buf, 0, buf.Length);
                            if (n == 0) break;
                            for (int i = 0; i < n; i++)
                            {
                                sb.Append(buf[i]);
                                const int stringMaxLength = int.MaxValue / 2 - 100;
                                if (sb.Length > stringMaxLength)
                                {
                                    sb.Remove(stringMaxLength, sb.Length - stringMaxLength);
                                    MessageBox.Show($"Reach C# string max length: {sb.Length}, breaking");
                                    goto index_skipRead;
                                }
                            }
                            Trace.WriteLine($"Reading...{s.Position}/{s.Length}");
                            if (CHBdebugMode.Checked && s.Position > 1000000) break;
                        }
                        index_skipRead:;
                        data = sb.ToString();//.Replace("\r\n"," ");
                    }
                    Trace.WriteLine($"{data.Length} charactors read.");
                    TXBout.Text = data.Length > 10000 ? data.Remove(10000) : data;
                    Trace.Write(" Counting baseDataLength...");
                    await Task.Run(() =>
                    {
                        baseDataLength = data.Count(c => IsChinese(c));
                    });
                    Trace.WriteLine($"baseDataLength: {baseDataLength}");
                    await BuildDataAsync();
                    CHBsplit_CheckedChanged(null, null);
                    //BTNsplit_Click(null, null);
                }
            }
        }
        private static bool IsChinese(char c)
        {
            return '\u4e00' <= c && c <= '\u9fff';
        }
        private async void CHBsplit_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                if (CHBsplit.Checked)
                {
                    maxWordLength = int.Parse(IFdata.GetField("maxWordLength"));
                    if (trie.IsBuilt)
                    {
                        await PerformSplit();
                    }
                }
            }
            catch (Exception error) { TXBout.Text = error.ToString(); }
        }
        private async void TXBdata_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            try
            {
                Trace.Indent();
                var fd = new OpenFileDialog();
                if (fd.ShowDialog() == DialogResult.OK)
                {
                    using (var s = fd.OpenFile())
                    {
                        if (s == null)
                        {
                            MessageBox.Show("File not opened");
                            return;
                        }
                        var encodingSelected = MessageBox.Show("\"Yes\" to use UTF-8\r\n\"No\" to use UTF-16 (Unicode)", "", MessageBoxButtons.YesNoCancel);
                        if (encodingSelected == DialogResult.Cancel) return;
                        Trace.Assert(encodingSelected == DialogResult.Yes || encodingSelected == DialogResult.No);
                        using (StreamReader reader = new StreamReader
                            (s, encodingSelected == DialogResult.Yes ? Encoding.UTF8 : Encoding.Unicode))
                        {
                            Trace.WriteLine("Reading...");
                            StringBuilder sb = new StringBuilder();
                            for (char[] buf = new char[1024 * 1024]; ;)
                            {
                                int n = await reader.ReadAsync(buf, 0, buf.Length);
                                if (n == 0) break;
                                for (int i = 0; i < n; i++) sb.Append(buf[i]);
                                Trace.WriteLine($"Reading...{s.Position}/{s.Length}");
                                if (CHBdebugMode.Checked && s.Position > 1000000) break;
                            }
                            data = sb.ToString();//.Replace("\r\n"," ");
                        }
                        Trace.WriteLine($"{data.Length} charactors read...");
                        TXBout.Text = data.Length > 10000 ? data.Remove(10000) : data;
                        txbDataFileContent = data;
                        Trace.Write("OK");
                    }
                }
            }
            finally { Trace.Unindent(); }
        }
        private async Task PerformSplit()
        {
            try
            {
                Trace.Indent();
                CHBsplit.Enabled = false;
                string fileName = "output.txt";
                var encoding = Encoding.UTF8;
                using (var writer = new StreamWriter(fileName, false, encoding))
                {
                    TXBout.Clear();
                    var d = new SentenceSplitter.WordIdentifiedEventHandler((word) =>
                    {
                        writer.WriteLine(word);
                        TXBout.Invoke(new Action(() =>
                        {
                            if (TXBout.TextLength < 10000)
                            {
                                TXBout.AppendText($"{word}\r\n");
                                if (TXBout.TextLength >= 10000) TXBout.AppendText("......(Cut)\r\n");
                            }
                        }));
                    });
                    var ss = new SentenceSplitter(trie, baseDataLength);
                    try
                    {
                        ss.WordIdentified += d;
                        Trace.WriteLine("Splitting...");
                        var ans = await ss.SplitAsync(
                            string.IsNullOrWhiteSpace(TXBdata.Text) ? (txbDataFileContent != null ? txbDataFileContent : data) : TXBdata.Text,
                            maxWordLength,
                            probRatio,
                            bemsRatio,
                            probType,
                            CHBlogPortion.Checked);
                        Trace.WriteLine($"{ans.Count} words identified.");
                    }
                    catch (Exception error) { TXBout.Text = error.ToString(); }
                    finally { ss.WordIdentified -= d; }
                    writer.Close();
                }
            }
            catch (Exception error) { TXBout.Text = error.ToString(); }
            finally { Trace.Unindent(); CHBsplit.CheckState = CheckState.Indeterminate; CHBsplit.Enabled = true; }
        }
        private void TXBin_TextChanged(object sender, EventArgs e)
        {
            try
            {
                Trace.Indent();
                Trace.WriteLine($"Method: {CBmethod.SelectedItem} \tInput: {TXBin.Text}");
                switch (CBmethod.SelectedItem)
                {
                    case "Count Word": TXBout.Text = CountWord(TXBin.Text); break;
                    default: TXBout.Text = TXBin.Text; break;
                }
            }
            catch (Exception error)
            {
                TXBout.Text = error.ToString();
            }
            finally { Trace.Unindent(); }
        }
        string CountWord(string dataInput)
        {
            StringBuilder ans = new StringBuilder();
            foreach (var _s in dataInput.Split('\n'))
            {
                var s = _s.TrimEnd('\r');
                ans.AppendLine($"{s}\t {trie.Count(s)}");
            }
            return ans.ToString();
        }
        Trie trie = new Trie();
        int counter = 0;
        private async Task BuildDataAsync()
        {
            //sam.Initialize();
            //sam.Extend(data);
            //sam.Build();
            //sm.Calculate(data, 5);
            try
            {
                Trace.Indent();
                Trace.WriteLine("TrieTabPage.BuildData");
                await trie.BuildAsync(data, int.Parse(IFdata.GetField("maxWordLength")));
                this.Text = $"#{++counter}";
            }
            finally { Trace.Unindent(); }
        }
        public TrieTabPage():base("Trie")
        {
            TLPmain.Controls.Add(TLPtop, 0, 0);
            {
                TLPtop.Controls.Add(TXBin, 0, 0);
                TLPtop.Controls.Add(new MyPanel() { Controls = { TLPctrl }, AutoScroll = true, Dock = DockStyle.Top }, 1, 0);
                {
                    //TLPctrl.SetRowSpan(TXBin, TLPctrl.RowCount);
                    int row = 0;
                    TLPctrl.Controls.Add(CBmethod, 0, row++);
                    {
                        CBmethod.Items.Add("Count Word");
                    }
                    TLPctrl.Controls.Add(BTNexportList, 0, row++);
                    TLPctrl.Controls.Add(BTNsave, 0, row++);
                    TLPctrl.Controls.Add(BTNload, 0, row++);
                    TLPctrl.Controls.Add(BTNnew, 0, row++);
                    TLPctrl.Controls.Add(BTNiteration, 0, row++);
                    TLPctrl.Controls.Add(CHBdebugMode, 0, row++);
                    TLPctrl.Controls.Add(CHBlogPortion, 0, row++);
                    TLPctrl.Controls.Add(IFdata, 0, row++);
                    {
                        IFdata.AddField("maxWordLength", maxWordLength.ToString());
                        IFdata.AddField("bemsRatio", bemsRatio.ToString());
                        IFdata.AddField("probRatio", probRatio.ToString());
                    }
                    TLPctrl.Controls.Add(CBprobType, 0, row++);
                    {
                        foreach (var s in SentenceSplitter.probTypeString.Split('\n'))
                        {
                            CBprobType.Items.Add(s);
                        }
                        CBprobType.SelectedValueChanged += (sender, e) =>
                        {
                            probType = Enum.GetValues(typeof(SentenceSplitter.ProbTypeEnum)).Cast<SentenceSplitter.ProbTypeEnum>().FirstOrDefault(v => CBprobType.Text.IndexOf($"probType == ProbTypeEnum.{v}") != -1);
                            //MessageBox.Show(probType.ToString());
                        };
                    }
                    TLPctrl.Controls.Add(CHBsplit, 0, row++);
                }
            }
            TLPmain.Controls.Add(TXBout, 0, 1);
            TLPmain.Controls.Add(TXBdata, 0, 2);
            //TXBdata.TextChanged += TXBdata_TextChanged;
            TXBdata.MouseDoubleClick += TXBdata_MouseDoubleClick;
            TXBin.TextChanged += TXBin_TextChanged;
            //BTNexportSA.Click += BTNexportSA_Click;
            BTNexportList.Click += BTNexportList_Click;
            CHBsplit.CheckedChanged += CHBsplit_CheckedChanged;
            //CHBbems.CheckedChanged += CHBbems_CheckedChanged;
            BTNsave.Click += BTNsave_Click;
            BTNload.Click += BTNload_Click;
            //BTNnew.Click += BTNnew_Click;
            BTNnew.Click += async delegate
            {
                try
                {
                    Trace.Indent();
                    await NewData();
                    //System.Diagnostics.Trace.WriteLine("A");
                    //string s;
                    //System.Diagnostics.Trace.WriteLine("B");

                    //string s = "";
                    //string v = new string('0', 10000000);
                    //while (true)
                    //{
                    //    System.Diagnostics.Trace.WriteLine($"{s.Length}");
                    //    s += v;
                    //}
                }
                finally { Trace.Unindent(); }
            };
            BTNiteration.Click += BTNiteration_Click;
            this.Controls.Add(TLPmain);
        }

        private async void BTNiteration_Click(object sender, EventArgs e)
        {
            try
            {
                Trace.Indent();
                int iterCount = int.Parse(Microsoft.VisualBasic.Interaction.InputBox("Iteration count?", "", "1"));
                for(int iterIdx=0;iterIdx<iterCount;iterIdx++)
                {
                    Trace.Unindent();
                    Trace.Indent();
                    Trace.WriteLine(TXBout.Text = $"Iteration: {iterIdx + 1}/{iterCount}");
                    try
                    {
                        Trace.Indent();
                        var words = new List<string>();
                        var ss = new SentenceSplitter(trie, baseDataLength);
                        List<FPLtype> fpl = null;
                        Trace.WriteLine("Getting FPL...");
                        await Task.Run(() => fpl = SentenceSplitter.MethodsForTrie.FrequencyPerLength(trie));
                        string[] ddd = null;
                        Trace.WriteLine("Preprocessing data...");
                        await Task.Run(() => ddd = data.Split(' '));
                        {
                            int progress = 0, total_progress = ddd.Length, percent = -1;
                            object syncRoot = new object();
                            Trace.WriteLine("Splitting...");
                            await Task.Run(() => Parallel.For(0, (ddd.Length+9)/10, _ =>
                            {
                                List<string> ans = new List<string>();
                                for (int i = _ * 10; i < (_ + 1) * 10 && i < ddd.Length; i++)
                                {
                                    {
                                        var p = System.Threading.Interlocked.Increment(ref progress) * 1000L / total_progress;
                                        if (p > percent)
                                        {
                                            percent = (int)p;
                                            Trace.WriteLine($"Splitting... {0.1 * percent}%");
                                        }
                                    }
                                    ans.AddRange(ss.Split(
                                        ddd[i],
                                        maxWordLength,
                                        null,
                                        fpl,
                                        probRatio,
                                        bemsRatio,
                                        probType,
                                        CHBlogPortion.Checked,
                                        false));
                                }
                                lock (syncRoot) words.AddRange(ans);
                            }));
                            Trace.Assert(progress == total_progress);
                        }
                        Trace.WriteLine($"{words.Count} words / {data.Length} chars identified.");
                        for (int i = 0; i < 1000 && i < words.Count; i++) TXBout.AppendText(words[i] + " ");
                        await Task.Run(() =>
                        {
                            int progress = 0, total_progress = words.Count,percent=-1;
                            foreach (var word in words)
                            {
                                if (++progress * 100L / total_progress > percent) Trace.WriteLine($"{words.Count} words / {data.Length} chars identified. {++percent}%");
                                trie.Insert(word);
                            }
                        });
                        Trace.WriteLine("Saving Trie...");
                        var fileName = $"Trie {DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss.fffffff")}.sav";
                        using (var stream = new FileStream(fileName, FileMode.Create))
                        {
                            await Task.Run(() => trie.Save(stream));
                        }
                        Trace.Unindent();
                        Trace.Indent();
                        Trace.WriteLine("OK: " + fileName);
                    }
                    catch (Exception error) { TXBout.Text = error.ToString(); }
                    finally { Trace.Unindent(); }
                }
                TXBout.AppendText("\r\nOK");
            }
            catch(Exception error) { TXBout.Text = error.ToString(); }
            finally { Trace.Unindent(); }
        }

        private async void BTNload_Click(object sender, EventArgs e)
        {
            try
            {
                Trace.Indent();
                BTNload.Enabled = false;
                Trace.WriteLine("Selecting file...");
                var fd = new OpenFileDialog();
                if (fd.ShowDialog() == DialogResult.OK)
                {
                    using (var s = fd.OpenFile())
                    {
                        if (s == null)
                        {
                            MessageBox.Show("File not opened");
                            return;
                        }
                        Trace.WriteLine($"Loading data... Size: {s.Length}");
                        await Task.Run(() => trie.Load(s));
                        Trace.Unindent();
                        Trace.Indent();
                        Trace.Write(" OK");
                    }
                }
            }
            catch (Exception error) { TXBout.Text = error.ToString(); }
            finally { BTNload.Enabled = true; Trace.Unindent(); }
        }

        private async void BTNsave_Click(object sender, EventArgs e)
        {
            try
            {
                Trace.Indent();
                BTNsave.Enabled = false;
                Trace.WriteLine("Saving Trie...");
                var fileName = $"Trie {DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss.fffffff")}.sav";
                using (var stream = new FileStream(fileName, FileMode.Create))
                {
                    await Task.Run(() => trie.Save(stream));
                }
                Trace.Unindent();
                Trace.Indent();
                Trace.WriteLine("OK: " + fileName);
            }
            catch(Exception error) { TXBout.Text = error.ToString(); }
            finally { BTNsave.Enabled = true; Trace.Unindent(); }
        }

        private async void BTNexportList_Click(object sender, EventArgs e)
        {
            try
            {
                Trace.Indent();
                Trace.WriteLine("Exporting List...");
                var fileName = $"WordList {DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss.fffffff")}.txt";
                using (var stream = new FileStream(fileName, FileMode.Create))
                {
                    await trie.ExportList(stream);
                }
                Trace.Unindent();
                Trace.Indent();
                Trace.WriteLine("OK: "+fileName);
            }
            catch (Exception error) { TXBout.Text = error.ToString(); }
            finally { Trace.Unindent(); }
        }
    }
}
