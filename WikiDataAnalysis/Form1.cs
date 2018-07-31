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
        public Form1()
        {
            Trace.UseGlobalLock = false;
            //InitializeComponent();
            this.Size = new Size(1000, 600);
            this.FormClosed += Form1_FormClosed;
            //sam = new SAM();
            //sam.StatusChanged += (s) => { this.Invoke(new Action(() => this.Text = $"[*] {s}")); };
            //sm = new SimpleMethod();
            //sm.StatusChanged += (s) => { this.Invoke(new Action(() => this.Text = $"[*] {s}")); };
            //sa.StatusChanged += (s) => { this.Invoke(new Action(() => this.Text = $"[*] {s}")); };
            this.Shown += Form1_Shown;
            var tc = new MyTabControl();
            tc.TabPages.Add(new SATabPage());
            this.Controls.Add(tc);
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
            this.Location = new Point(0, 0);
            await TestCode.Run();
            GenerateOutputWindow();
            this.Text = "Ready";
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
        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            Process.GetCurrentProcess().Kill();
        }
    }
}
