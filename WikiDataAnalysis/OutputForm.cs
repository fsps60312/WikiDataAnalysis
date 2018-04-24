using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using Motivation;

namespace WikiDataAnalysis
{
    partial class OutputForm:Form
    {
        public class MyTraceListener : TraceListener
        {
            public delegate void NewMsgEventHandler(string msg,int indent);
            public event NewMsgEventHandler NewMsg;
            public override void Write(string message)
            {
                NewMsg?.Invoke(message, this.IndentLevel);
            }
            public delegate void NewLineEventHandler(string msg,int indent);
            public event NewLineEventHandler NewLine;
            public override void WriteLine(string message)
            {
                NewLine?.Invoke(message, this.IndentLevel);
            }
            public override bool IsThreadSafe => true;
        }
        List<string> msgs = new List<string>();
        MyTextBox TXB = new MyTextBox(true);
        void EnsureIndent(int indent)
        {
            while (msgs.Count < indent) msgs.Add(new string('\t', msgs.Count) + "(Nothing...)");
            if (msgs.Count > indent) msgs.RemoveRange(indent, msgs.Count - indent);
        }
        void ShowMsg()
        {
            TXB.Text = string.Join("\r\n", msgs);
            Application.DoEvents();
        }
        public OutputForm(MyTraceListener listener)
        {
            this.Controls.Add(TXB);
            this.Size = new System.Drawing.Size(700, 500);
            Queue<Tuple<Delegate, object[]>> toInvoke = new Queue<Tuple<Delegate, object[]>>();
            this.FormClosing += OutputForm_FormClosing;
            {
                MyTraceListener.NewMsgEventHandler f = null;
                f = new MyTraceListener.NewMsgEventHandler((msg, indent) =>
                {
                    if (InvokeRequired)
                    {
                        toInvoke.Enqueue(new Tuple<Delegate, object[]>(new MyTraceListener.NewMsgEventHandler(f), new object[] { msg, indent }));
                        return;
                    }
                    if(toInvoke.Count>0)
                    {
                        var v = toInvoke.Dequeue();
                        Invoke(v.Item1, v.Item2);
                    }
                    EnsureIndent(indent + 1);
                    msgs[indent] += msg;
                    ShowMsg();
                });
                listener.NewMsg += f;
            }
            {
                MyTraceListener.NewLineEventHandler f = null;
                f = new MyTraceListener.NewLineEventHandler((msg, indent) =>
                {
                    if (InvokeRequired)
                    {
                        toInvoke.Enqueue(new Tuple<Delegate, object[]>(new MyTraceListener.NewLineEventHandler(f), new object[] { msg, indent }));
                        return;
                    }
                    if (toInvoke.Count > 0)
                    {
                        var v = toInvoke.Dequeue();
                        Invoke(v.Item1, v.Item2);
                    }
                    EnsureIndent(indent);
                    msgs.Add(new string('\t', msgs.Count) + msg);
                    ShowMsg();
                });
                listener.NewLine += f;
            }
        }

        private void OutputForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            MessageBox.Show("Close the Main Window");
            e.Cancel = true;
        }
    }
}
