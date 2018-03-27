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

namespace WikiDataAnalysis
{
    public partial class Form1 : Form
    {
        MyTableLayoutPanel TLPmain = new MyTableLayoutPanel(1, 3, "P", "P2P2P");
        MyTextBox TXBin = new MyTextBox(true), TXBout = new MyTextBox(true),TXBdata=new MyTextBox(true);
        public Form1()
        {
            InitializeComponent();
            this.Size = new Size(1000, 600);
            TLPmain.Controls.Add(TXBin, 0, 0);
            TLPmain.Controls.Add(TXBout, 0, 1);
            TLPmain.Controls.Add(TXBdata, 0, 2);
            TXBdata.TextChanged += TXBdata_TextChanged;
            TXBdata.MouseDoubleClick += TXBdata_MouseDoubleClick;
            TXBin.TextChanged += TXBin_TextChanged;
            this.Controls.Add(TLPmain);
            sam = new SAM();
            sam.StatusChanged += (s) => { this.Invoke(new Action(() => this.Text = $"[*] {s}")); };
            this.Text = "Ready";
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
                    using (StreamReader reader = new StreamReader(s,Encoding.UTF8))
                    {
                        this.Text = "Reading...";
                        StringBuilder sb = new StringBuilder();
                        for(char[] buf=new char[1024*1024]; ;)
                        {
                            int n=await reader.ReadAsync(buf, 0, buf.Length);
                            if (n == 0) break;
                            for (int i = 0; i < n; i++) sb.Append(buf[i]);
                            this.Text = $"Reading...{s.Position}/{s.Length}";
                            if (s.Position > 10000000) break;
                        }
                        data = sb.ToString();
                    }
                    this.Text = $"{data.Length} charactors read";
                    BuildData();
                }
            }
        }
        string data = "";
        private void TXBin_TextChanged(object sender, EventArgs e)
        {
            TXBout.Text = $"Result:{sam.Count(TXBin.Text)}";
        }
        SAM sam;
        int counter = 0;
        private void BuildData()
        {
            sam.Initialize();
            sam.Extend(data);
            sam.Build();
            this.Text = $"#{++counter}";
        }
        private void TXBdata_TextChanged(object sender, EventArgs e)
        {
            data = TXBdata.Text;
            BuildData();
        }
    }
}
