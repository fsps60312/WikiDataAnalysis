using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;

namespace WikiDataAnalysis_WPF
{
    class RadioPanel:StackPanel
    {
        public string SelectedText { get; private set; } = null;
        public int SelectedIndex { get; private set; } = 0;
        public RadioPanel(string title,params string[]options)
        {
            this.Children.Add(new Label { Content = title }.Set(0, 0));
            int i = 0;
            foreach(var opt in options)
            {
                string my_opt = opt;
                int my_i = i++;
                var btn = new RadioButton { Content = opt };
                btn.Checked += delegate { SelectedText = my_opt;SelectedIndex = my_i; };
                this.Children.Add(btn);
                if (SelectedText == null) btn.IsChecked = true;
            }
        }
    }
}
