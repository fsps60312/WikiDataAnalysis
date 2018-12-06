using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.IO;

namespace WikiDataAnalysis_WPF
{
    class TrieTabItem:TabItem
    {
        TextBox textBox_in, textBox_out, textBox_data;
        Button button_exportList,button_save,button_load,button_new,button_performIteration;
        CheckBox checkBox_split,checkBox_debugMode;
        string data;
        int baseDataLength;
        Trie trie = new Trie();
        void ProcessInput(string text)
        {
        }
        void InitializeViews()
        {
            textBox_in = new TextBox();
            textBox_out = new TextBox();
            textBox_data = new TextBox();
            button_exportList = new Button { Content = "Export List" };
            button_load = new Button { Content = "Load" };
            button_save = new Button { Content = "Save" };
            checkBox_split = new CheckBox { Content = "Split" };
            checkBox_debugMode = new CheckBox { Content = "Debug Mode", IsChecked = true };
            textBox_in.KeyDown += (sender, e) => { if (e.Key == Key.Enter && MainWindow.IsDown(Key.LeftCtrl)) ProcessInput(textBox_in.Text); };
            textBox_data.MouseDoubleClick += TextBox_data_MouseDoubleClick;
            button_exportList.Click += Button_exportList_Click;
            checkBox_split.Checked += CheckBox_split_Checked;
            this.Content = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition{Height=new GridLength(2,GridUnitType.Star)},
                    new RowDefinition{Height=new GridLength(2,GridUnitType.Star)},
                    new RowDefinition{Height=new GridLength(1,GridUnitType.Star)}
                },
                Children =
                {
                    new Grid
                    {
                    }.Set(0,0),
                    new Grid
                    {
                    }.Set(0,1),
                    new Grid
                    {
                    }.Set(0,2)
                }
            };
            button_load.Click += Button_load_Click;
            button_save.Click += Button_save_Click;
            button_new.Click += Button_new_Click;
            button_performIteration.Click += Button_performIteration_Click;
        }

        private void Button_performIteration_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private async void Button_new_Click(object sender, RoutedEventArgs e)
        {
            await Log.SubTask(() => NewData());
        }

        private void Button_save_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void Button_load_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void CheckBox_split_Checked(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void Button_exportList_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void TextBox_data_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            throw new NotImplementedException();
        }
        async Task NewData()
        {
            await Log.SubTask(async () =>
            {
                using (var s = MyLib.Open())
                {
                    if (s == null)
                    {
                        MessageBox.Show("File not opened");
                        return;
                    }
                    var encodingSelected = MessageBox.Show("\"Yes\" to use UTF-8\r\n\"No\" to use UTF-16 (Unicode)", "", MessageBoxButton.YesNo);
                    if (encodingSelected == MessageBoxResult.None) return;
                    Log.Assert(encodingSelected == MessageBoxResult.Yes || encodingSelected == MessageBoxResult.No);
                    using (StreamReader reader = new StreamReader
                        (s, encodingSelected == MessageBoxResult.Yes ? Encoding.UTF8 : Encoding.Unicode))
                    {
                        Log.WriteLine("Reading...");
                        var sb = new StringBuilder();
                        data = "";
                        for (char[] buf = new char[1024 * 1024]; ;)
                        {
                            int n = await reader.ReadAsync(buf, 0, buf.Length);
                            if (n == 0) break;
                            for (int i = 0; i < n; i++)
                            {
                                sb.Append(buf[i]);
                                const int stringMaxLength = int.MaxValue / 2 - 100;
                                if (sb.Length > stringMaxLength)
                                {
                                    sb.Remove(stringMaxLength, sb.Length - stringMaxLength);
                                    if (MessageBox.Show($"Reach C# string max length: {sb.Length}, break?", "Warning", MessageBoxButton.OKCancel) == MessageBoxResult.OK) goto index_skipRead;
                                    else
                                    {
                                        data += sb.ToString();
                                        sb.Clear();
                                    }
                                }
                            }
                            Log.WriteLine($"Reading...{s.Position}/{s.Length}");
                            if (checkBox_debugMode.IsChecked==true&& s.Position > 1000000) break;
                        }
                        index_skipRead:;
                        data += sb.ToString();//.Replace("\r\n"," ");
                    }
                    Log.WriteLine($"{data.Length} charactors read.");
                    textBox_out.Text = data.Length > 10000 ? data.Remove(10000) : data;
                    Log.Write(" Counting baseDataLength...");
                    await Task.Run(() =>
                    {
                        baseDataLength = data.Count(c => MyLib.IsChinese(c));
                    });
                    Log.WriteLine($"baseDataLength: {baseDataLength}");
                    await BuildDataAsync();
                    CheckBox_split_Checked(null, null);
                    //BTNsplit_Click(null, null);
                }
            });
        }
        private async Task BuildDataAsync()
        {
            await Log.SubTask(async() =>
            {
                Log.WriteLine("TrieTabPage.BuildData");
                int maxWordLength = int.Parse(IFdata.Get("maxWordLength"));
                await trie.BuildAsync(data, maxWordLength);
            });
        }

        public TrieTabItem()
        {
            this.Header = "Trie";
            Log.AppendLog("TrieTabItem OK.");
        }
    }
}
