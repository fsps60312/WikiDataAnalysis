using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Windows.Forms;
using System.Drawing;
using System.IO;
using Motivation;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace WikiDataAnalysis
{
    class SATabPage:MyTabPage
    {
        MyTableLayoutPanel TLPmain = new MyTableLayoutPanel(1, 3, "P", "P2P2P"), TLPtop = new MyTableLayoutPanel(2, 1, "P2P", "P");
        MyTableLayoutPanel TLPctrl = new MyTableLayoutPanel(1, 14, "P", "PPPPPPPPPPAPPP") { Dock = DockStyle.Top };
        MyTextBox TXBin = new MyTextBox(true), TXBout = new MyTextBox(true), TXBdata = new MyTextBox(true);
        MyButton BTNexportSA = new MyButton("Export SA"), BTNexportList = new MyButton("Export List");
        MyButton BTNsave = new MyButton("Save SA"), BTNload = new MyButton("Load SA"), BTNnew = new MyButton("New data");
        MyCheckBox
            CHBdebugMode = new MyCheckBox("Debug Mode") { Checked = true },
            CHBreplaceWithEmptyExceptChinese = new MyCheckBox("Replace with Empty except Chinese") { Checked = true },
            CHBremoveEmpty = new MyCheckBox("Remove Empty") { Checked = true },
            CHBsplit = new MyCheckBox("Split") { Checked = false },
            CHBverbose = new MyCheckBox("Verbose") { Checked = true };
        ComboBox CBmethod = new ComboBox { Dock = DockStyle.Fill, Font = new Font("微軟正黑體", 15) };
        ComboBox CBprobType = new ComboBox { Dock = DockStyle.Fill, Font = new Font("微軟正黑體", 10) };
        MyInputField IFdata = new MyInputField();
        int maxWordLength = 4;
        SentenceSplitter.ProbTypeEnum probType = SentenceSplitter.ProbTypeEnum.Hank;
        string txbDataFileContent = null;
        readonly string sampleCode =
                   $"//public static double FooMethod(string S,int N,Func<string,int> C)\r\n" +
                    "return (double)C(S)/N";
        public SATabPage():base("SA")
        {
            //InitializeComponent();
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
                        CBmethod.Items.Add("List Words");
                        CBmethod.Items.Add("Send Socket");
                        CBmethod.Items.Add("Cut by Code");
                    }
                    TLPctrl.Controls.Add(BTNexportSA, 0, row++);
                    TLPctrl.Controls.Add(BTNsave, 0, row++);
                    TLPctrl.Controls.Add(BTNload, 0, row++);
                    TLPctrl.Controls.Add(BTNexportList, 0, row++);
                    TLPctrl.Controls.Add(BTNnew, 0, row++);
                    TLPctrl.Controls.Add(CHBdebugMode, 0, row++);
                    TLPctrl.Controls.Add(CHBreplaceWithEmptyExceptChinese, 0, row++);
                    TLPctrl.Controls.Add(CHBremoveEmpty, 0, row++);
                    TLPctrl.Controls.Add(CHBverbose, 0, row++);
                    TLPctrl.Controls.Add(IFdata, 0, row++);
                    {
                        IFdata.AddField("maxWordLength", maxWordLength.ToString());
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
            TXBin.KeyDown += TXBin_KeyDown;
            TXBin.ContextMenu = new ContextMenu(new[] { new MenuItem("sample code", delegate { TXBin.Text = sampleCode; }) });
            BTNexportSA.Click += BTNexportSA_Click;
            BTNexportList.Click += BTNexportList_Click;
            CHBsplit.CheckedChanged += CHBsplit_CheckedChanged;
            BTNsave.Click += BTNsave_Click;
            BTNload.Click += BTNload_Click;
            BTNnew.Click += BTNnew_Click;
            this.Controls.Add(TLPmain);
            //sam = new SAM();
            //sam.StatusChanged += (s) => { this.Invoke(new Action(() => this.Text = $"[*] {s}")); };
            //sm = new SimpleMethod();
            //sm.StatusChanged += (s) => { this.Invoke(new Action(() => this.Text = $"[*] {s}")); };
            //sa.StatusChanged += (s) => { this.Invoke(new Action(() => this.Text = $"[*] {s}")); };
            sa = new SuffixArray();
            ss = new SentenceSplitter(sa);
            StartServices();
        }
        private void StartServices()
        {
            SocketService.StartService(7122, s =>
            {
                try
                {
                    //MessageBox.Show($"Receive: {s}\r\nBytes: {string.Join(" ", Encoding.UTF8.GetBytes(s).Select(v => (int)v))}");
                    return (sa.UpperBound(s) - sa.LowerBound(s)).ToString();
                }
                catch (Exception error) { return error.ToString(); }
            });
            bool fpl_isbuilt=false, fpl_isbuilding = false;
            SocketService.StartService(7123, s =>
             {
                 try
                 {
                     int l = int.Parse(s);
                     if (!sa.IsBuilt) return "Not yet, try again later!";
                     if (!fpl_isbuilt)
                     {
                         if(!fpl_isbuilding)
                         {
                             fpl_isbuilding = true;
                             var ans= SentenceSplitter.MethodsForSuffixArray.FrequencyPerLength(sa)[l].uniqCnt.ToString();
                             fpl_isbuilt = true;
                             return ans;
                         }
                         else return "Preprocessing, please wait!";
                     }
                     else
                     {
                         return SentenceSplitter.MethodsForSuffixArray.FrequencyPerLength(sa)[l].uniqCnt.ToString();
                     }
                 }
                 catch (Exception error) { return error.ToString(); }
             });
        }

        System.Threading.SemaphoreSlim SemaphoreSlim_CutByCode = new System.Threading.SemaphoreSlim(1);
        long counter_CutByCode = 0;
        SentenceSplitter ss_CutByCode;
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
                   $"       public static double {methodName}(string S,int N,Func<string,int> C)" +
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
                    var method = new Func<string, int, Func<string, int>, double>((s, n, c) => (double)methodInfo.Invoke(null, new object[] { s, n, c }));
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
                        if (ss_CutByCode == null) ss_CutByCode = new SentenceSplitter(sa);
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
        private async void TXBin_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && e.Control)
            {
                switch (CBmethod.SelectedItem)
                {
                    case "Send Socket":
                        {
                            try
                            {
                                Trace.Indent();
                                Trace.WriteLine($"Initializing Socket...");
                                var input = TXBin.Text.Split('\n').Select(s => s.TrimEnd('\r')).ToArray();
                                var port = int.Parse(input[0]);
                                TXBout.Text = $"port: {port}\r\nmsg: {input[1]}\r\n";
                                Socket client_sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                                client_sock.Connect(new IPEndPoint(IPAddress.Parse("127.0.0.1"), port));
                                Trace.WriteLine($"Connected: {client_sock.Connected}");
                                client_sock.Send(Encoding.UTF8.GetBytes(input[1]));
                                client_sock.Send(new[] { (byte)'\0' });
                                var thread = new Thread(() =>
                                {
                                    using (var stream = new NetworkStream(client_sock))
                                    {
                                        List<byte> data = new List<byte>();
                                        const byte target = (byte)'\0';
                                        while (true)
                                        {
                                            var _b = stream.ReadByte();
                                            if (_b == -1)
                                            {
                                                Trace.WriteLine("Connection closed.");
                                            }
                                            var b = (byte)_b;
                                            if (b == target)
                                            {
                                                Trace.WriteLine($"receive_length={data.Count}");
                                                var s = Encoding.UTF8.GetString(data.ToArray());
                                                TXBout.Invoke(new Action(() => TXBout.AppendText(s + "\r\n")));
                                            }
                                            else data.Add(b);
                                        }
                                    }
                                });
                                thread.Start();
                                new Thread(() => { Thread.Sleep(1000 * 60); thread.Abort(); });
                            }
                            catch (Exception error)
                            {
                                TXBout.Text = error.ToString();
                            }
                            finally { Trace.Unindent(); }
                        }
                        break;
                    case "Cut by Code":
                        {
                            string s = await CutByCode(TXBin.Text); if (s != null) TXBout.Text = s;
                        }
                        break;
                }
            }
        }

        static void AddToMultiset<T>(SortedDictionary<T, int> dict, T v)
        {
            if (dict.ContainsKey(v)) dict[v]++;
            else dict.Add(v, 1);
        }
        static void RemoveFromMultiset<T>(SortedDictionary<T, int> dict, T v)
        {
            if (--dict[v] == 0) dict.Remove(v);
        }
        private async void BTNexportList_Click(object sender, EventArgs e)
        {
            try
            {
                Trace.Indent();
                int threshold = int.Parse(Microsoft.VisualBasic.Interaction.InputBox("Threshold?", "", "25"));

                List<Tuple<int, int>> s = new List<Tuple<int, int>>();
                Trace.WriteLine("Searching...");
                try
                {
                    Trace.Indent();
                    using (StreamWriter writer = new StreamWriter("output.txt", false, Encoding.UTF8))
                    {
                        TXBout.Clear();
                        long progress = 0;
                        var lastUpdateTime = DateTime.Now;
                        await sa.ListFrequentWords(threshold, new Func<string, Task>(async (str) =>
                        {
                            ++progress;
                            var count = sa.UpperBound(str) - sa.LowerBound(str);
                            await writer.WriteLineAsync($"{str},{count}");
                            if (TXBout.TextLength < 100000)
                            {
                                TXBout.AppendText($"{str}\t{count}\r\n");
                                if (TXBout.TextLength >= 100000)
                                {
                                    TXBout.AppendText("......(Cut)");
                                }
                            }
                            if ((DateTime.Now - lastUpdateTime).TotalSeconds > 0.5)
                            {
                                Trace.WriteLine($"{progress} words listed, Ex:{str}\t{count}");
                                lastUpdateTime = DateTime.Now;
                            }
                        }));
                    }

                }
                finally { Trace.Unindent(); }
                Trace.Write("Done");
            }
            catch (Exception error)
            {
                TXBout.Text = error.ToString();
            }
            finally { Trace.Unindent(); }
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
                            if (CHBreplaceWithEmptyExceptChinese.Checked)
                            {
                                CHBreplaceWithEmptyExceptChinese.Enabled = false;
                                Trace.WriteLine("Replacing with Empty except Chinese...");
                                sb.Clear();
                                bool isSpace = false;
                                foreach (var c in data)
                                {
                                    if (IsChinese(c))
                                    {
                                        sb.Append(c);
                                        isSpace = false;
                                    }
                                    else if (!isSpace)
                                    {
                                        sb.Append(' ');
                                        isSpace = true;
                                    }
                                }
                                data = sb.ToString();
                                Trace.Write("OK");
                                CHBreplaceWithEmptyExceptChinese.Enabled = true;
                            }
                            if (CHBremoveEmpty.Checked)
                            {
                                CHBremoveEmpty.Enabled = false;
                                Trace.WriteLine("Removing empties...");
                                sb.Clear();
                                foreach (var c in data)
                                {
                                    switch (char.GetUnicodeCategory(c))
                                    {
                                        //case System.Globalization.UnicodeCategory.SpacingCombiningMark:
                                        //case System.Globalization.UnicodeCategory.Format:
                                        case System.Globalization.UnicodeCategory.Control:
                                        case System.Globalization.UnicodeCategory.SpaceSeparator:
                                            break;
                                        default: sb.Append(c); break;
                                    }
                                }
                                data = sb.ToString();
                                Trace.Write("OK");
                                CHBremoveEmpty.Enabled = true;
                            }
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
        

        private async void CHBsplit_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                if (CHBsplit.Checked)
                {
                    maxWordLength = int.Parse(IFdata.GetField("maxWordLength"));
                    if (sa.IsBuilt)
                    {
                        await PerformSplit();
                    }
                }
            }
            catch (Exception error) { TXBout.Text = error.ToString(); }
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
                    try
                    {
                        ss.WordIdentified += d;
                        Trace.WriteLine("Splitting...");
                        var ans = await ss.SplitAsync(
                            string.IsNullOrWhiteSpace(TXBdata.Text) ? (txbDataFileContent != null ? txbDataFileContent : sa.S) : TXBdata.Text,
                            maxWordLength,
                            probType,
                            CHBverbose.Checked);
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

        private async void BTNnew_Click(object sender, EventArgs e)
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
                        if (CHBreplaceWithEmptyExceptChinese.Checked)
                        {
                            CHBreplaceWithEmptyExceptChinese.Enabled = false;
                            Trace.WriteLine("Replacing with Empty except Chinese...");
                            sb.Clear();
                            bool isSpace = false;
                            foreach (var c in data)
                            {
                                if (IsChinese(c))
                                {
                                    sb.Append(c);
                                    isSpace = false;
                                }
                                else if (!isSpace)
                                {
                                    sb.Append(' ');
                                    isSpace = true;
                                }
                            }
                            data = sb.ToString();
                            Trace.Write("OK");
                            CHBreplaceWithEmptyExceptChinese.Enabled = true;
                        }
                        if (CHBremoveEmpty.Checked)
                        {
                            CHBremoveEmpty.Enabled = false;
                            Trace.WriteLine("Removing empties...");
                            sb.Clear();
                            foreach (var c in data)
                            {
                                switch (char.GetUnicodeCategory(c))
                                {
                                    //case System.Globalization.UnicodeCategory.SpacingCombiningMark:
                                    //case System.Globalization.UnicodeCategory.Format:
                                    case System.Globalization.UnicodeCategory.Control:
                                    case System.Globalization.UnicodeCategory.SpaceSeparator:
                                        break;
                                    default: sb.Append(c); break;
                                }
                            }
                            data = sb.ToString();
                            Trace.Write("OK");
                            CHBremoveEmpty.Enabled = true;
                        }
                    }
                    Trace.WriteLine($"{data.Length} charactors read");
                    TXBout.Text = data.Length > 10000 ? data.Remove(10000) : data;
                    await BuildDataAsync();
                    CHBsplit_CheckedChanged(null, null);
                    //BTNsplit_Click(null, null);
                }
            }
        }

        private async void BTNload_Click(object sender, EventArgs e)
        {
            try
            {
                BTNload.Enabled = false;
                Trace.Indent();
                var fd = new OpenFileDialog();
                fd.FileName = "wiki.sav";
                if (fd.ShowDialog() == DialogResult.OK)
                {
                    using (var s = fd.OpenFile())
                    {
                        if (s == null)
                        {
                            MessageBox.Show("File not opened");
                            return;
                        }
                        await sa.LoadAsync(s);
                        s.Close();
                        Trace.WriteLine("Done");
                    }
                }
            }
            finally { Trace.Unindent(); BTNload.Enabled = true; }
        }

        private async void BTNsave_Click(object sender, EventArgs e)
        {
            try
            {
                BTNsave.Enabled = false;
                Trace.Indent();
                var sd = new SaveFileDialog();
                sd.FileName = "wiki.sav";
                if (sd.ShowDialog() == DialogResult.OK)
                {
                    using (var s = sd.OpenFile())
                    {
                        if (s == null)
                        {
                            MessageBox.Show("File not opened");
                            return;
                        }
                        await sa.SaveAsync(s);
                        s.Close();
                        Trace.WriteLine("Done");
                    }
                }
            }
            finally { Trace.Unindent(); BTNsave.Enabled = true; }
        }

        private void WriteJoin<T>(StreamWriter writer, string seperator, IEnumerable<T> o, bool writeLine = true)
        {
            bool first = true;
            Trace.WriteLine($"Writing {o.Count()} objects...");
            foreach (var v in o)
            {
                if (first) first = false;
                else writer.Write(seperator);
                writer.Write(v);
            }
            if (writeLine) writer.WriteLine();
        }
        private void BTNexportSA_Click(object sender, EventArgs e)
        {
            try
            {
                Trace.Indent();
                var sd = new SaveFileDialog();
                if (sd.ShowDialog() == DialogResult.OK)
                {
                    using (var s = sd.OpenFile())
                    {
                        if (s == null)
                        {
                            MessageBox.Show("File not opened");
                            return;
                        }
                        using (var writer = new StreamWriter(s, Encoding.UTF8))
                        {
                            Trace.WriteLine("Writing...");
                            writer.WriteLine("SA.S");
                            WriteJoin(writer, " ", sa.S);
                            writer.WriteLine("SA.SA");
                            WriteJoin(writer, " ", sa.SA);
                            writer.WriteLine("SA.RANK");
                            WriteJoin(writer, " ", sa.RANK);
                            writer.WriteLine("SA.HEIGHT");
                            WriteJoin(writer, " ", sa.HEIGHT);
                            writer.Close();
                        }
                        Trace.WriteLine("Done");
                    }
                }
            }
            finally { Trace.Unindent(); }
        }
        private bool IsChinese(char c)
        {
            return '\u4e00' <= c && c <= '\u9fff';
        }

        //private async void TXBdata_MouseDoubleClick(object sender, MouseEventArgs e)
        //{
        //}
        string data = "";
        string ListWords(string dataInput)
        {
            int n = int.Parse(dataInput);
            List<Tuple<int, int>> s = new List<Tuple<int, int>>();
            int pre = 0;
            Trace.WriteLine("Searching...");
            try
            {
                Trace.Indent();
                int percentage = -1;
                for (int i = 1; ; i++)
                {
                    if ((i + 1) * 100L / sa.S.Length > percentage)
                    {
                        Trace.WriteLine($"{++percentage}%");
                    }
                    if (i == sa.S.Length || sa.HEIGHT[i] < n)
                    {
                        int len = (i - 1) - (pre - 1);
                        if (len > 1)
                        {
                            s.Add(new Tuple<int, int>(sa.SA[i - 1], len));
                        }
                        pre = i;
                        if (i == sa.S.Length) break;
                    }
                }
            }
            finally { Trace.Unindent(); }
            Trace.WriteLine($"Constructing results...({s.Count})");
            StringBuilder sb = new StringBuilder();
            try
            {
                Trace.Indent();
                Trace.WriteLine("Sorting...");
                s.Sort((a, b) => -a.Item2.CompareTo(b.Item2));
                Trace.WriteLine("Sorted.");
                int percentage = -1;
                for (int i = 0; i < s.Count; i++)
                {
                    if (Math.Max((i + 1) * 100L / s.Count, sb.Length * 100L / 10000000) > percentage)
                    {
                        Trace.WriteLine($"{++percentage}%");
                    }
                    if (sb.Length > 10000000)
                    {
                        sb.AppendLine($"{s.Count - i} more lines...");
                        break;
                    }
                    var t = sa.S.Substring(s[i].Item1, n);
                    if (t.IndexOf('\r') == -1 && t.IndexOf('\n') == -1) sb.AppendLine($"{s[i].Item2}\t{t}");
                }
                sb.AppendLine($"Total Count: {s.Sum(v => (long)v.Item2)}");
            }
            finally { Trace.Unindent(); }
            Trace.Write("Done");
            return sb.ToString();
        }
        string CountWord(string dataInput)
        {
            StringBuilder ans = new StringBuilder();
            foreach (var _s in dataInput.Split('\n'))
            {
                var s = _s.TrimEnd('\r');
                var ub = sa.UpperBound(s);
                var lb = sa.LowerBound(s);
                ans.AppendLine($"{s} \t{ub}:{lb}\t{ub - lb}");
            }
            return ans.ToString();
        }
        private void TXBin_TextChanged(object sender, EventArgs e)
        {
            try
            {
                Trace.Indent();
                Trace.WriteLine($"Method: {CBmethod.SelectedItem} \tInput: {TXBin.Text}");
                switch (CBmethod.SelectedItem)
                {
                    case "List Words": TXBout.Text = ListWords(TXBin.Text); break;
                    case "Count Word": TXBout.Text = CountWord(TXBin.Text); break;
                    case "Send Socket":
                    case "Cut by Code":
                        TXBout.Text = "Press Ctrl+Enter to send";break;
                    default: TXBout.Text = TXBin.Text; break;
                }
            }
            catch (Exception error)
            {
                TXBout.Text = error.ToString();
            }
            finally { Trace.Unindent(); }
        }
        //SAM sam;
        //SimpleMethod sm;
        SuffixArray sa;
        SentenceSplitter ss;
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
                Trace.WriteLine("Form1.BuildData");
                await sa.BuildAsync(data);
                this.Text = $"#{++counter}";
            }
            finally { Trace.Unindent(); }
        }
        //private async void TXBdata_TextChanged(object sender, EventArgs e)
        //{
        //    data = TXBdata.Text;
        //    await BuildDataAsync();
        //}
    }
}
