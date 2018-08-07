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
        MyButton BTNexportSA = new MyButton("Export SA"), BTNexportList = new MyButton("Export List");
        MyButton BTNsave = new MyButton("Save SA"), BTNload = new MyButton("Load SA"), BTNnew = new MyButton("New data");
        MyCheckBox
            CHBdebugMode = new MyCheckBox("Debug Mode") { Checked = true },
            CHBreplaceWithEmptyExceptChinese = new MyCheckBox("Replace with Empty except Chinese") { Checked = true },
            CHBremoveEmpty = new MyCheckBox("Remove Empty") { Checked = true },
            CHBsplit = new MyCheckBox("Split") { Checked = false },
            CHBlogPortion = new MyCheckBox("Log Portion") { Checked = true };
        ComboBox CBmethod = new ComboBox { Dock = DockStyle.Fill, Font = new Font("微軟正黑體", 15) };
        ComboBox CBprobType = new ComboBox { Dock = DockStyle.Fill, Font = new Font("微軟正黑體", 10) };
        MyInputField IFdata = new MyInputField();
        int maxWordLength = 4;
        double bemsRatio = 0;
        double probRatio = 1;
        SentenceSplitter.ProbTypeEnum probType = SentenceSplitter.ProbTypeEnum.CdL;
        string txbDataFileContent = null;
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
                        CBmethod.Items.Add("List Words");
                    }
                    TLPctrl.Controls.Add(BTNexportSA, 0, row++);
                    TLPctrl.Controls.Add(BTNsave, 0, row++);
                    TLPctrl.Controls.Add(BTNload, 0, row++);
                    TLPctrl.Controls.Add(BTNexportList, 0, row++);
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
                string s = new string('0', 1200000000);
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
