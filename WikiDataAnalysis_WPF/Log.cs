using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Threading;

namespace WikiDataAnalysis_WPF
{
    class Log
    {
        class LogWindow : Window
        {
            int IndentLevel = 0;
            public void Indent() { Interlocked.Increment(ref IndentLevel); }
            public void Unindent() { Interlocked.Decrement(ref IndentLevel); }
            List<string> messages = new List<string>();
            List<string> logs = new List<string>();
            object syncRoot_messages = new object(),syncRoot_logs=new object();
            int counter_UpdateMsg = 0, counter_UpdateLog = 0;
            int token_UpdateMsg = 0, token_UpdateLog = 0;
            TextBox textBox_log, textBox_msg;
            int EnsureIndent()
            {
                int i = IndentLevel;
                if (messages.Count > i + 1) messages.RemoveRange(i + 1, messages.Count - (i + 1));
                while (messages.Count < i + 1) messages.Add(null);
                return i;
            }
            async void UpdateMsg()
            {
                int counter = Interlocked.Increment(ref counter_UpdateMsg);
                if (Interlocked.CompareExchange(ref token_UpdateMsg, 0, 1) == 1) goto index_pass;
                await Task.Delay(100);
                if (counter == counter_UpdateMsg) goto index_pass;
                return;
                index_pass:;
                string txt;
                lock (syncRoot_messages) txt = string.Join("\r\n", messages);
                Dispatcher.Invoke(() => { textBox_msg.Text = txt; });
            }
            public void ReplaceLine(string msg)
            {
                lock(syncRoot_messages)
                {
                    int i=EnsureIndent();
                    messages[i] = msg;
                }
                UpdateMsg();
            }
            public void Write(string msg)
            {
                lock(syncRoot_messages)
                {
                    int i = EnsureIndent();
                    messages[i] += msg;
                }
                UpdateMsg();
            }
            async void UpdateLog()
            {
                int counter = Interlocked.Increment(ref counter_UpdateLog);
                if (Interlocked.CompareExchange(ref token_UpdateLog, 0, 1) == 1) goto index_pass;
                await Task.Delay(100);
                if (counter == counter_UpdateLog) goto index_pass;
                index_pass:;
                string txt;
                lock (syncRoot_logs)
                {
                    txt = string.Join("\r\n", logs) + "\r\n";
                    logs.Clear();
                }
                Dispatcher.Invoke(() => textBox_log.AppendText(txt));
            }
            public void AppendLog(string msg)
            {
                lock (syncRoot_logs) logs.Add(msg);
                UpdateLog();
            }
            void InitializeViews()
            {
                textBox_log = new TextBox();
                textBox_msg = new TextBox();
                textBox_log.TextChanged += delegate { textBox_log.ScrollToEnd(); };
                textBox_msg.TextChanged += delegate { textBox_msg.ScrollToEnd(); };
                this.Content = new Grid
                {
                    ColumnDefinitions =
                    {
                        new ColumnDefinition{Width=new GridLength(1,GridUnitType.Star)},
                        new ColumnDefinition{Width=new GridLength(1,GridUnitType.Star)}
                    },
                    Children =
                    {
                        textBox_msg.Set(0,0),
                        textBox_log.Set(0,1)
                    }
                };
            }
            async void SynchronizeJob()
            {
                while(true)
                {
                    token_UpdateLog = 1; token_UpdateMsg = 1;
                    await Task.Delay(500);
                }
            }
            public LogWindow()
            {
                this.Left = 0;
                this.Top = this.Height =MyLib.ScreenHeight / 2;
                this.Width = MyLib.ScreenWidth / 2;
                this.Closing += (sender, e) => { MessageBox.Show("Close the MainWindow, not me."); e.Cancel = true; };
                InitializeViews();
                this.Show();
                SynchronizeJob();
            }
        }
        static LogWindow logWindow = new LogWindow();
        public static void Indent() { logWindow.Indent(); }
        public static void Unindent() { logWindow.Unindent(); }
        public static void WriteLine(string msg) { logWindow.ReplaceLine(msg); }
        public static void Write(string msg) { logWindow.Write(msg); }
        public static void AppendLog(string msg) { logWindow.AppendLog(msg); }
        public static void Assert(bool condition,string msg=null) { System.Diagnostics.Trace.Assert(condition, msg); }
        public static async Task<T> SubTask<T>(Func<Task<T>>action)
        {
            try
            {
                Indent();
                return await action();
            }
            finally { Unindent(); }
        }
        public static T SubTask<T>(Func<T>action)
        {
            try
            {
                Indent();
                return action();
            }
            finally { Unindent(); }
        }
        public static async Task SubTask(Func<Task>action)
        {
            try
            {
                Indent();
                await action();
            }
            finally { Unindent(); }
        }
        public static void SubTask(Action action)
        {
            try
            {
                Indent();
                action();
            }
            finally { Unindent(); }
        }
    }
}
