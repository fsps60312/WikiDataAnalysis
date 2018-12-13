using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;

namespace WikiDataAnalysis_WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MonitorKeysWindow
    {
        public new static Func<Key, bool> IsDown = k => false;
        public MainWindow():base()
        {
            //MessageBox.Show("Hi");
            IsDown = k => base.IsDown(k);
            InitializeComponent();
            Trace.UseGlobalLock = false;
            //InitializeComponent();
            this.Top = this.Left = 0;
            this.Width = MyLib.ScreenWidth / 2;
            this.Height = MyLib.ScreenHeight / 2;
            this.Closed += MainWindow_Closed;
            this.Initialized += MainWindow_Initialized;
            var tc = new TabControl();
            tc.Items.Add(new TrieTabItem());
            tc.Items.Add(new ToolsTabItem());
            this.Content = tc;
        }

        private void MainWindow_Initialized(object sender, EventArgs e)
        {
            //GenerateOutputWindow();
            this.Title = "Ready";
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            Process.GetCurrentProcess().Kill();
        }
    }
}
