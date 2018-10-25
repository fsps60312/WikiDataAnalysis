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
    partial class TrieTabPage:MyTabPage
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
        const int default_maxWordLength = 4;
        const double default_bemsRatio = 0;
        const double default_probRatio = 1;
        const int default_baseDataLength = -1;
        const double default_decayRatio = 1.0;
        int baseDataLength
        {
            get { return int.Parse(IFdata.GetField("baseDataLength")); }
            set { SetIFdata("baseDataLength", value.ToString()); }
        }
        void SetIFdata(string key,string value)
        {
            if (IFdata.InvokeRequired) IFdata.Invoke(new Action(() => IFdata.GetTextBox(key).Text = value));
            else IFdata.GetTextBox(key).Text = value;
        }
        SentenceSplitter.ProbTypeEnum probType = SentenceSplitter.ProbTypeEnum.Sigmoid;
        string data = null;
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
                        data = "";
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
                                    if(MessageBox.Show($"Reach C# string max length: {sb.Length}, break?","Warning",MessageBoxButtons.OKCancel)==DialogResult.OK) goto index_skipRead;
                                    else
                                    {
                                        data += sb.ToString();
                                        sb.Clear();
                                    }
                                }
                            }
                            Trace.WriteLine($"Reading...{s.Position}/{s.Length}");
                            if (CHBdebugMode.Checked && s.Position > 1000000) break;
                        }
                        index_skipRead:;
                        data += sb.ToString();//.Replace("\r\n"," ");
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
                var maxWordLength = int.Parse(IFdata.GetField("maxWordLength"));
                var probRatio = double.Parse(IFdata.GetField("probRatio"));
                var bemsRatio = double.Parse(IFdata.GetField("bemsRatio"));
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
                        var mainInputs = string.IsNullOrWhiteSpace(TXBdata.Text) ? (txbDataFileContent != null ? txbDataFileContent : data) : TXBdata.Text;
                        var inputs = mainInputs.Split(' ', '\r', '\n', '\t');
                        long cnt = 0;
                        foreach (var input in inputs)
                        {
                            cnt += (await ss.SplitAsync(
                                input,
                                maxWordLength,
                                probRatio,
                                bemsRatio,
                                probType,
                                CHBlogPortion.Checked)).Count;
                        }
                        Trace.WriteLine($"{cnt} words identified.");
                    }
                    catch (Exception error) { TXBout.Text = error.ToString(); }
                    finally { ss.WordIdentified -= d; }
                    writer.Close();
                }
            }
            catch (Exception error) { TXBout.Text = error.ToString(); }
            finally { Trace.Unindent(); CHBsplit.CheckState = CheckState.Indeterminate; CHBsplit.Enabled = true; }
        }
        private async void TXBin_TextChanged(object sender, EventArgs e)
        {
            try
            {
                Trace.Indent();
                Trace.WriteLine($"Method: {CBmethod.SelectedItem} \tInput: {TXBin.Text}");
                switch (CBmethod.SelectedItem)
                {
                    case "Count Word": TXBout.Text = CountWord(TXBin.Text); break;
                    case "Cut by Code":string s = await CutByCode(TXBin.Text);if (s != null) TXBout.Text = s; break;
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
            var getEntropy = new Func<string, double>(s =>
            {
                var cs = trie.NextChars(s);
                var ns = cs.Select(c => (double)trie.Count(s + c));
                var sum = ns.Sum();
                return ns.Sum(v =>
                {
                    if (v == 0) return 0;
                    var p = v / sum;
                    return -p * Math.Log(p);
                });
            });
            StringBuilder ans = new StringBuilder();
            foreach (var _s in dataInput.Split('\n'))
            {
                var s = _s.TrimEnd('\r');
                ans.AppendLine($"{s.PadRight(4, '　')}\t Count: {trie.Count(s)}\t Entropy: {getEntropy(s)}");
            }
            return ans.ToString();
        }
        System.Threading.SemaphoreSlim SemaphoreSlim_CutByCode = new System.Threading.SemaphoreSlim(1);
        long counter_CutByCode = 0;
        SentenceSplitter ss_CutByCode = null;
        async Task<string> CutByCode(string dataInput)//the method: double(double C,double E) //count, entropy, return score
        {
            var counter = System.Threading.Interlocked.Increment(ref counter_CutByCode);
            try
            {
                await SemaphoreSlim_CutByCode.WaitAsync();
                if (counter != System.Threading.Interlocked.Read(ref counter_CutByCode)) return null;
                const string namespaceName = "WikiDataAnalysis", className = "FooClass", methodName = "FooMethod";
                string code =
                    "using System;" +
                   $"namespace {namespaceName}" +
                    "{" +
                   $"   class {className}" +
                    "   {" +
                   $"       public static double {methodName}(double L,double C,double E,double M,double S)" +
                    "       {" +
                   $"           {dataInput}" +
                    "       }" +
                    "   }" +
                    "}";
                System.Reflection.MethodInfo methodInfo;
                try
                {
                    Trace.Indent();
                    Trace.WriteLine($"Compiling... code length = {code.Length}");
                    methodInfo = Utils.DynamicCompile.GetMethod(code, namespaceName, className, methodName, "System");
                    var method = new Func<double,double, double, double, double, double>((l,c, e, m, s) => (double)methodInfo.Invoke(null, new object[] {l, c, e, m, s }));
                    Trace.WriteLine("Splitting...");
                    var maxWordLength = int.Parse(IFdata.GetField("maxWordLength"));
                    var probRatio = double.Parse(IFdata.GetField("probRatio"));
                    var bemsRatio = double.Parse(IFdata.GetField("bemsRatio"));
                    StringBuilder sb_ret = new StringBuilder();
                    long cnt = 0;
                    await Task.Run(() =>
                    {
                        var mainInputs = string.IsNullOrWhiteSpace(TXBdata.Text) ? (txbDataFileContent != null ? txbDataFileContent : data) : TXBdata.Text;
                        var inputs = mainInputs.Split(' ', '\r', '\n', '\t');
                        if (ss_CutByCode == null) ss_CutByCode = new SentenceSplitter(trie, baseDataLength);
                        const int maxoutputLength = 10000;
                        bool appending = true;
                        int progress = 0, total_progress = inputs.Length;
                        var lastUpdateTime = DateTime.MinValue;
                        foreach (var input in inputs)
                        {
                            ++progress;
                            if ((DateTime.Now - lastUpdateTime).TotalSeconds > 0.5)
                            {
                                Trace.WriteLine($"Splitting... {progress}/{total_progress}");
                                lastUpdateTime = DateTime.Now;
                            }
                            var cutResult = ss_CutByCode.Split(input, maxWordLength, method, false);
                            cnt += cutResult.Count;
                            if (sb_ret.Length + cutResult.Sum(s => (long)s.Length) > maxoutputLength) appending = false;
                            if (appending) sb_ret.AppendLine(string.Join(" ", cutResult));
                        }
                    });
                    Trace.WriteLine($"{cnt} words identified.");
                    return sb_ret.ToString();
                }
                catch (Exception error) { return error.ToString(); }
                finally { Trace.Unindent(); }
            }
            finally { lock (SemaphoreSlim_CutByCode) SemaphoreSlim_CutByCode.Release(); }
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
                        CBmethod.Items.Add("Cut by Code");
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
                        IFdata.AddField("maxWordLength", default_maxWordLength.ToString());
                        IFdata.AddField("bemsRatio", default_bemsRatio.ToString());
                        IFdata.AddField("probRatio", default_probRatio.ToString());
                        IFdata.AddField("baseDataLength", default_baseDataLength.ToString());
                        IFdata.AddField("decayRatio", default_decayRatio.ToString());
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
                        var iterationStatus = $"Iteration: {iterIdx + 1}/{iterCount}";
                        TXBout.AppendText(iterationStatus + "\r\n");
                        Trace.WriteLine(iterationStatus);
                    try
                    {
                        Trace.Indent();
                        var maxWordLength = int.Parse(IFdata.GetField("maxWordLength"));
                        var probRatio = double.Parse(IFdata.GetField("probRatio"));
                        var bemsRatio = double.Parse(IFdata.GetField("bemsRatio"));
                        var words = new List<string>();
                        var ss = new SentenceSplitter(trie, baseDataLength);
                        List<FPLtype> fpl = null;
                        Trace.WriteLine("Getting FPL...");
                        await Task.Run(() => fpl = SentenceSplitter.MethodsForTrie.FrequencyPerLength(trie));
                        string[] ddd = null;
                        Trace.WriteLine("Preprocessing data...");
                        var data = string.IsNullOrWhiteSpace(TXBdata.Text) ? (txbDataFileContent != null ? txbDataFileContent : this.data) : TXBdata.Text;
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
                        TXBout.Text = iterationStatus + "\r\n";
                        for (int i = 0; i < 1000 && i < words.Count; i++) TXBout.AppendText(words[i] + " ");
                        var decayRatio = double.Parse(IFdata.GetField("decayRatio"));
                        await Task.Run(() =>
                        {
                            Trace.WriteLine($"Decaying... ratio = {decayRatio}");
                            long cnt = 0;
                            trie.Traverse(c => { }, () => { }, c => cnt += c);
                            Trace.Write($"\t{cnt}→");
                            trie.Decay(decayRatio);
                            cnt = 0;
                            trie.Traverse(c => { }, () => { }, c => cnt += c);
                            Trace.Write($"{cnt} OK");
                            try
                            {
                                Trace.Indent();
                                int progress = 0, total_progress = words.Count, percent = -1;
                                foreach (var word in words)
                                {
                                    if (++progress * 100L / total_progress > percent) Trace.WriteLine($"{words.Count} words / {data.Length} chars inserted. {++percent}%");
                                    trie.Insert(word);
                                }
                            }
                            finally { Trace.Unindent(); }
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
