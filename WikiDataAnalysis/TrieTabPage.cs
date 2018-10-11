using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Motivation;
using System.Windows.Forms;
using System.Drawing;

namespace WikiDataAnalysis
{
    class TrieTabPage:MyTabPage
    {
        MyTableLayoutPanel TLPmain = new MyTableLayoutPanel(1, 3, "P", "P2P2P"), TLPtop = new MyTableLayoutPanel(2, 1, "P2P", "P");
        MyTableLayoutPanel TLPctrl = new MyTableLayoutPanel(1, 13, "P", "PPPPPPPPPPAPP") { Dock = DockStyle.Top };
        MyTextBox TXBin = new MyTextBox(true), TXBout = new MyTextBox(true), TXBdata = new MyTextBox(true);
        MyButton BTNexportList = new MyButton("Export List"),BTNnew = new MyButton("New data");
        ComboBox CBmethod = new ComboBox { Dock = DockStyle.Fill, Font = new Font("微軟正黑體", 15) };
        ComboBox CBprobType = new ComboBox { Dock = DockStyle.Fill, Font = new Font("微軟正黑體", 10) };
        MyInputField IFdata = new MyInputField();
        MyCheckBox CHBsplit = new MyCheckBox("Split") { Checked = false };
        int maxWordLength = 4;
        SentenceSplitter.ProbTypeEnum probType = SentenceSplitter.ProbTypeEnum.Sigmoid;
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
                    TLPctrl.Controls.Add(BTNnew, 0, row++);
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
            //TXBdata.MouseDoubleClick += TXBdata_MouseDoubleClick;
            //TXBin.TextChanged += TXBin_TextChanged;
            //BTNexportSA.Click += BTNexportSA_Click;
            //BTNexportList.Click += BTNexportList_Click;
            //CHBsplit.CheckedChanged += CHBsplit_CheckedChanged;
            //CHBbems.CheckedChanged += CHBbems_CheckedChanged;
            //BTNsave.Click += BTNsave_Click;
            //BTNload.Click += BTNload_Click;
            //BTNnew.Click += BTNnew_Click;
            BTNnew.Click += delegate
            {
                System.Diagnostics.Trace.WriteLine("A");
                string s;
                System.Diagnostics.Trace.WriteLine("B");

                //string s = "";
                //string v = new string('0', 10000000);
                //while (true)
                //{
                //    System.Diagnostics.Trace.WriteLine($"{s.Length}");
                //    s += v;
                //}
            };
            this.Controls.Add(TLPmain);
        }
    }
}
