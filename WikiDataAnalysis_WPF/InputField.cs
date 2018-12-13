using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;

namespace WikiDataAnalysis_WPF
{
    class InputField : StackPanel
    {
        public InputField()
        {
        }
        Dictionary<string, TextBox> textBoxes = new Dictionary<string, TextBox>();
        public bool Contains(string key) { return textBoxes.ContainsKey(key); }
        public TextBox this[string key]
        {
            get { return textBoxes[key]; }
        }
        public void AddField(string title,string content)
        {
            MyLib.Assert(!textBoxes.ContainsKey(title));
            var textBox = new TextBox { Text = content };
            textBoxes.Add(title, textBox);
            this.Children.Add(new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition{Width=new GridLength(1,GridUnitType.Star)},
                    new ColumnDefinition{Width=new GridLength(1,GridUnitType.Star)}
                },
                Children =
                {
                    new Label{ Content=title}.Set(0,0),
                    textBox.Set(0,1)
                }
            });
        }
    }
}
