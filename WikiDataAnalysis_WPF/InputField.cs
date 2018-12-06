using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;

namespace WikiDataAnalysis_WPF
{
    class InputField:Grid
    {
        public InputField()
        {
        }
        Dictionary<string, TextBox> textBoxes = new Dictionary<string, TextBox>();
        public void AddField(string title,string content)
        {
            MyLib.Assert(!textBoxes.ContainsKey(title));
            this.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            var textBox = new TextBox();
            textBoxes.Add(title, textBox);
            this.Children.Add(textBox);
        }
    }
}
