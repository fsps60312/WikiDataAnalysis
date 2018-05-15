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
        MyTableLayoutPanel TLPmain = new MyTableLayoutPanel(1, 3, "P", "P2P2P");
        MyTableLayoutPanel TLPctrl = new MyTableLayoutPanel(2, 4, "P2P", "PPPP");
        MyTextBox TXBin = new MyTextBox(true), TXBout = new MyTextBox(true),TXBdata=new MyTextBox(true);
        MyButton BTNsplit = new MyButton("Split"),BTNexportSA=new MyButton("Export SA");
        MyCheckBox CHBdebugMode = new MyCheckBox("Debug Mode") {Checked = false };
        ComboBox CBmethod = new ComboBox {Dock=DockStyle.Fill, Font = new Font("微軟正黑體", 15)};
        public Form1()
        {
            Trace.UseGlobalLock = false;
            //InitializeComponent();
            this.Size = new Size(1000, 600);
            this.FormClosed += Form1_FormClosed;
            TLPmain.Controls.Add(TLPctrl, 0, 0);
            {
                TLPctrl.Controls.Add(TXBin, 0, 0);
                TLPctrl.SetRowSpan(TXBin, TLPctrl.RowCount);
                TLPctrl.Controls.Add(CBmethod, 1, 0);
                {
                    CBmethod.Items.Add("Count Word");
                    CBmethod.Items.Add("List Words");
                }
                TLPctrl.Controls.Add(CHBdebugMode, 1, 1);
                TLPctrl.Controls.Add(BTNexportSA, 1, 2);
                TLPctrl.Controls.Add(BTNsplit, 1, 3);
            }
            TLPmain.Controls.Add(TXBout, 0, 1);
            TLPmain.Controls.Add(TXBdata, 0, 2);
            TXBdata.TextChanged += TXBdata_TextChanged;
            TXBdata.MouseDoubleClick += TXBdata_MouseDoubleClick;
            TXBin.TextChanged += TXBin_TextChanged;
            BTNexportSA.Click += BTNexportSA_Click;
            BTNsplit.Click += BTNsplit_Click;
            this.Controls.Add(TLPmain);
            //sam = new SAM();
            //sam.StatusChanged += (s) => { this.Invoke(new Action(() => this.Text = $"[*] {s}")); };
            //sm = new SimpleMethod();
            //sm.StatusChanged += (s) => { this.Invoke(new Action(() => this.Text = $"[*] {s}")); };
            sa = new SuffixArray();
            //sa.StatusChanged += (s) => { this.Invoke(new Action(() => this.Text = $"[*] {s}")); };
            this.Shown += Form1_Shown;
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
                            writer.WriteLine(string.Join(" ", sa.S));
                            writer.WriteLine("SA.SA");
                            writer.WriteLine(string.Join(" ", sa.SA));
                            writer.WriteLine("SA.RANK");
                            writer.WriteLine(string.Join(" ", sa.RANK));
                            writer.WriteLine("SA.HEIGHT");
                            writer.WriteLine(string.Join(" ", sa.HEIGHT));
                            writer.Close();
                        }
                        Trace.WriteLine("Done");
                    }
                }
            }
            finally { Trace.Unindent(); }
        }

        private void BTNsplit_Click(object sender, EventArgs e)
        {
            try
            {
                Trace.Indent();
                BTNsplit.Enabled = false;
                SentenceSplitter ss = new SentenceSplitter();
                using (var writer = new StreamWriter("output.txt", false, Encoding.UTF8))
                {
                    ss.WordIdentified += (word) => { writer.WriteLine(word); Application.DoEvents(); };
                    Trace.WriteLine("Splitting...");
                    var ans = ss.Split(sa);
                    writer.Close();
                    Trace.WriteLine($"{ans.Count} words identified.");
                }
            }
            finally { Trace.Unindent(); BTNsplit.Enabled = true; }
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
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

        private async void TXBdata_MouseDoubleClick(object sender, MouseEventArgs e)
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
                        (s,encodingSelected==DialogResult.Yes? Encoding.UTF8:Encoding.Unicode))
                    {
                        Trace.WriteLine( "Reading...");
                        StringBuilder sb = new StringBuilder();
                        for(char[] buf=new char[1024*1024]; ;)
                        {
                            int n=await reader.ReadAsync(buf, 0, buf.Length);
                            if (n == 0) break;
                            for (int i = 0; i < n; i++) sb.Append(buf[i]);
                            Trace.WriteLine( $"Reading...{s.Position}/{s.Length}");
                            if (CHBdebugMode.Checked && s.Position > 10000000) break;
                        }
                        data = sb.ToString().Replace("\r\n","");
                    }
                    Trace.WriteLine( $"{data.Length} charactors read");
                    BuildData();
                    //BTNsplit_Click(null, null);
                }
            }
        }
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
        int counter = 0;
        private void BuildData()
        {
            //sam.Initialize();
            //sam.Extend(data);
            //sam.Build();
            //sm.Calculate(data, 5);
            try
            {
                Trace.Indent();
                Trace.WriteLine("Form1.BuildData");
                sa.Build(data);
                this.Text = $"#{++counter}";
            }
            finally { Trace.Unindent(); }
        }
        private void TXBdata_TextChanged(object sender, EventArgs e)
        {
            data = TXBdata.Text;
            BuildData();
        }
    }
}
