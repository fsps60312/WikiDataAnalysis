using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Motivation;
using System.IO;
using System.Diagnostics;

namespace WikiDataAnalysis
{
    public partial class Form1 : Form
    {
        MyTableLayoutPanel TLPmain = new MyTableLayoutPanel(1, 3, "P", "P2P2P"),TLPtop=new MyTableLayoutPanel(2,1,"P2P","P");
        MyTableLayoutPanel TLPctrl = new MyTableLayoutPanel(1, 13, "P", "PPPPPPPPPAPPP") {Dock=DockStyle.Top};
        MyTextBox TXBin = new MyTextBox(true), TXBout = new MyTextBox(true),TXBdata=new MyTextBox(true);
        MyButton BTNexportSA=new MyButton("Export SA");
        MyButton BTNsave = new MyButton("Save SA"), BTNload = new MyButton("Load SA"),BTNnew=new MyButton("New data");
        MyCheckBox
            CHBdebugMode = new MyCheckBox("Debug Mode") { Checked = true },
            CHBreplaceWithEmptyExceptChinese = new MyCheckBox("Replace with Empty except Chinese") { Checked = true },
            CHBremoveEmpty = new MyCheckBox("Remove Empty") { Checked = true },
            CHBsplit = new MyCheckBox("Split") { Checked = false },
            CHBbems = new MyCheckBox("BEMS") { Checked = false },
            CHBlogPortion = new MyCheckBox("Log Portion") { Checked = true };
        ComboBox CBmethod = new ComboBox { Dock = DockStyle.Fill, Font = new Font("微軟正黑體", 15) };
        ComboBox CBprobType = new ComboBox { Dock = DockStyle.Fill, Font = new Font("微軟正黑體", 10) };
        MyInputField IFdata = new MyInputField();
        int maxWordLength = 4;
        double bemsRatio = 1;
        double probRatio = 1;
        SentenceSplitter.ProbTypeEnum probType = SentenceSplitter.ProbTypeEnum.CdL;
        string txbDataFileContent = null;
        public Form1()
        {
            Trace.UseGlobalLock = false;
            //InitializeComponent();
            this.Size = new Size(1000, 600);
            this.FormClosed += Form1_FormClosed;
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
                    }
                    TLPctrl.Controls.Add(BTNexportSA, 0, row++);
                    TLPctrl.Controls.Add(BTNsave, 0, row++);
                    TLPctrl.Controls.Add(BTNload, 0, row++);
                    TLPctrl.Controls.Add(BTNnew, 0, row++);
                    TLPctrl.Controls.Add(CHBdebugMode, 0, row++);
                    TLPctrl.Controls.Add(CHBreplaceWithEmptyExceptChinese, 0, row++);
                    TLPctrl.Controls.Add(CHBremoveEmpty, 0, row++);
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
                    TLPctrl.Controls.Add(CHBbems, 0, row++);
                }
            }
            TLPmain.Controls.Add(TXBout, 0, 1);
            TLPmain.Controls.Add(TXBdata, 0, 2);
            //TXBdata.TextChanged += TXBdata_TextChanged;
            TXBdata.MouseDoubleClick += TXBdata_MouseDoubleClick;
            TXBin.TextChanged += TXBin_TextChanged;
            BTNexportSA.Click += BTNexportSA_Click;
            CHBsplit.CheckedChanged += CHBsplit_CheckedChanged;
            CHBbems.CheckedChanged += CHBbems_CheckedChanged;
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
            this.Shown += Form1_Shown;
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

        private async void CHBbems_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                if (CHBbems.Checked)
                {
                    if (ss.IsBuilt)
                    {
                        await PerformBEMS();
                    }
                }
            }
            catch (Exception error) { TXBout.Text = error.ToString(); }
        }
        private async Task PerformBEMS()
        {
            try
            {
                Trace.Indent();
                CHBbems.Enabled = false;
                MessageBox.Show("BEMS!");
                await Task.Delay(1000);
                MessageBox.Show("YA!");
            }
            catch (Exception error) { TXBout.Text = error.ToString(); }
            finally { Trace.Unindent(); CHBbems.CheckState = CheckState.Indeterminate; CHBbems.Enabled = true; }
        }

        private async void CHBsplit_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                if (CHBsplit.Checked)
                {
                    maxWordLength = int.Parse(IFdata.GetField("maxWordLength"));
                    probRatio = double.Parse(IFdata.GetField("probRatio"));
                    bemsRatio = double.Parse(IFdata.GetField("bemsRatio"));
                    if (sa.IsBuilt)
                    {
                        await PerformSplit();
                        CHBbems_CheckedChanged(null, null);
                    }
                }
            }
            catch(Exception error) { TXBout.Text = error.ToString(); }
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
                        if (TXBout.TextLength < 100000)
                        {
                            TXBout.AppendText($"{word}\r\n");
                            if (TXBout.TextLength >= 100000) TXBout.AppendText("......(Cut)\r\n");
                        }
                    });
                    try
                    {
                        ss.WordIdentified += d;
                        Trace.WriteLine("Splitting...");
                        var ans = await ss.SplitAsync(
                            string.IsNullOrWhiteSpace(TXBdata.Text) ? (txbDataFileContent != null ? txbDataFileContent : sa.S) : TXBdata.Text,
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

        private void WriteJoin<T>(StreamWriter writer, string seperator,IEnumerable<T>o,bool writeLine=true)
        {
            bool first = true;
            Trace.WriteLine($"Writing {o.Count()} objects...");
            foreach(var v in o)
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
                            WriteJoin(writer," ", sa.S);
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

        private async void Form1_Shown(object sender, EventArgs e)
        {
            //TextBox textBox = new TextBox();//不知道TextBox的去Google圖片
            //this.Controls.Add(textBox);//把textBox顯示出來
            //Parallel.For(0, 100, i =>//使用多個Thread，從0到99平行跑i，全部跑完後再繼續
            //{
            //    System.Threading.Thread.Sleep(1000);//等待1000毫秒
            //    textBox.Invoke(new Action(() => textBox.AppendText(i.ToString() + "\r\n")));
            //    //將AppendText的工作交給textBox，等textBox反應過來並做完該工作再繼續
            //    //不Invoke的話textBox可能會因為被多個Thread同時修改導致undefined behavior
            //});
            await TestCode.Run();
            GenerateOutputWindow();
            this.Text = "Ready";
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            Process.GetCurrentProcess().Kill();
        }

        private void GenerateOutputWindow()
        {
            //Trace.Refresh();
            var tl = new OutputForm.MyTraceListener();
            Trace.Listeners.Add(tl);
            var f = new OutputForm(tl);
            f.Show();
            f.Location = this.Location;
            f.Top += this.Height;
            f.BringToFront();
            //System.Windows.Forms.MessageBox.Show($"{tl.IsThreadSafe} {Trace.UseGlobalLock}");
            //Trace.UseGlobalLock = true;
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
            }
            finally { Trace.Unindent(); }
            Trace.Write("Done");
            return sb.ToString();
        }
        string CountWord(string dataInput)
        {
            StringBuilder ans = new StringBuilder();
            foreach(var _s in dataInput.Split('\n'))
            {
                var s = _s.TrimEnd('\r');
                var ub = sa.UpperBound(s);
                var lb = sa.LowerBound(s);
                ans.AppendLine($"{s} \t{ub}:{lb}\t{ub-lb}");
            }
            return ans.ToString();
        }
        private void TXBin_TextChanged(object sender, EventArgs e)
        {
            try
            {
                Trace.Indent();
                Trace.WriteLine($"Method: {CBmethod.SelectedItem} \tInput: {TXBin.Text}");
                switch(CBmethod.SelectedItem)
                {
                    case "List Words": TXBout.Text = ListWords(TXBin.Text);break;
                    case "Count Word":TXBout.Text = CountWord(TXBin.Text);break;
                    default:TXBout.Text = TXBin.Text;break;
                }
            }
            catch(Exception error)
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
