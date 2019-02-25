using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.IO;
using System.Collections.Generic;

namespace WikiDataAnalysis_WPF
{
    partial class TrieTabItem:TabItem
    {
        static class TextSplit
        {
            public static void Split20181213(Trie trie, string data, int maxWordLength, Action<string> callBack)
            {
                double[,] dp = new double[data.Length + 1, 2];
                int[,] pre = new int[data.Length + 1, 2];
                dp[0, 0] = dp[0, 1] = 0;
                Log.WriteLine("Initializing...");
                Parallel.For(1, data.Length, i => dp[i, 0] = dp[i, 1] = double.MinValue);
                Log.WriteLine("Dping...");
                long progress = 0, total_progress = (long)data.Length * maxWordLength;
                throw new NotImplementedException();
                //Parallel.For(1,maxWordLength+1,wordLength=>
                //{
                //    for(int )
                //})
            }
        }
        Trie trie = new Trie();
        string mainData = null;

        private async void Button_wordsPerCount_Click(object sender, RoutedEventArgs e)
        {
            int targetWordLength = int.Parse(Microsoft.VisualBasic.Interaction.InputBox("targetWordLength?", "", "4"));
            var fileName = MyLib.Save();
            if (fileName == null) return;
            await stackPanel_tasksQueue.EnqueueTaskAsync($"Words per Count {fileName} {targetWordLength}", async () => await Task.Run(() =>
              {
                  long percent = -1, progress = 0, total_progress = trie.Size;
                  int wordLength = 0;
                  SortedDictionary<long, long> ans = new SortedDictionary<long, long>();
                  for (int i = 1; i <= 10; i++) ans[i] = 0;
                  trie.Traverse(c => wordLength++, () => wordLength--, cnt =>
                        {
                            if (++progress * 100 / total_progress > percent)
                            {
                                Log.WriteLine($"Words per Count... {++percent}% {ans[1]} {ans[2]} {ans[3]} {ans[4]} {ans[5]} | {ans[6]} {ans[7]} {ans[8]} {ans[9]} {ans[10]}");
                            }
                            if (wordLength != targetWordLength) return;
                            if (!ans.ContainsKey(cnt)) ans[cnt] = 0;
                            ans[cnt]++;
                        });
                  Log.WriteLine($"Writing...");
                  using (StreamWriter writer = new StreamWriter(fileName, false, Encoding.UTF8))
                  {
                      writer.WriteLine("Count,Words");
                      foreach (var p in ans) writer.WriteLine($"{p.Key},{p.Value}");
                  }
                  Log.Write(" OK");
              }));
        }

        private void Button_split_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private async void TextBox_in_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && MainWindow.IsDown(Key.LeftCtrl))
            {
                string text = textBox_in.Text;
                int inputMethod = radioPanel_inputMethod.SelectedIndex;
                await stackPanel_tasksQueue.EnqueueTaskAsync($"ProcessInput({text},{inputMethod})", new Func<Task>(async () => await Task.Run(() => ProcessInput(text, inputMethod))));
            }
        }

        void ProcessInput(string text,int inputMethod)
        {
            OutputText = "";
            switch(inputMethod)
            {
                case 0://Count Word
                    {
                        foreach(var s in text.Split('\n').Select(s=>s.TrimEnd()))
                        {
                            OutputText += $"{s}\t{trie.Count(s)}\r\n";
                        }
                    }break;
                default:throw new Exception($"Unknown inputMethod: {inputMethod}");
            }
        }

        private void Button_performIteration_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private async void Button_new_Click(object sender, RoutedEventArgs e)
        {
            var fileName = MyLib.Open();
            if (fileName == null) return;
            var encodingSelected = MessageBox.Show("\"Yes\" to use UTF-8\r\n\"No\" to use UTF-16 (Unicode)", "", MessageBoxButton.YesNo);
            if (encodingSelected == MessageBoxResult.None) return;
            Log.Assert(encodingSelected == MessageBoxResult.Yes || encodingSelected == MessageBoxResult.No);
            var encoding = encodingSelected == MessageBoxResult.Yes ? Encoding.UTF8 : Encoding.Unicode;
            int maxWordLength = int.Parse(inputField_data["maxWordLength"].Text);
            bool debugMode = (bool)checkBox_debugMode.IsChecked;
            int processMethod = radioPanel_newData.SelectedIndex;
            await stackPanel_tasksQueue.EnqueueTaskAsync($"BuildDataAsync({fileName},{encoding},{processMethod},{maxWordLength},{debugMode})", new Func<Task>(async () => await Task.Run(() => BuildData(
                new FileStream(fileName, FileMode.Open, FileAccess.Read),
                encoding,
                processMethod,
                maxWordLength,
                debugMode))));
        }

        private async void Button_save_Click(object sender, RoutedEventArgs e)
        {
            var fileName = MyLib.Save();
            if (fileName == null) return;
            await stackPanel_tasksQueue.EnqueueTaskAsync($"Save Trie {fileName}", new Func<Task>(async () =>
            {
                Log.WriteLine("Saving trie...");
                await Task.Run(() => trie.Save(new FileStream(fileName, FileMode.Create, FileAccess.Write)));
                Log.Write(" OK");
            }));
        }

        private async void Button_load_Click(object sender, RoutedEventArgs e)
        {
            var fileName = MyLib.Open();
            if (fileName == null) return;
            await stackPanel_tasksQueue.EnqueueTaskAsync($"Read Trie {fileName}", new Func<Task>(async () =>
            {
                Log.WriteLine("Loading trie...");
                await Task.Run(() => trie.Load(new FileStream(fileName, FileMode.Open, FileAccess.Read)));
                Log.Write(" OK");
            }));
        }


        private async void Button_exportList_Click(object sender, RoutedEventArgs e)
        {
            var fileName = MyLib.Save();
            if (fileName == null) return;
            await stackPanel_tasksQueue.EnqueueTaskAsync($"Export Word List {fileName}", new Func<Task>(async () =>
            {
                Log.WriteLine("Exporting Word List...");
                await trie.ExportList(new FileStream(fileName, FileMode.Create, FileAccess.Write));
                Log.Write(" OK");
            }));
        }

        private async void TextBox_data_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var fileName = MyLib.Open();
            if (fileName == null) return;
            var encodingSelected = MessageBox.Show("\"Yes\" to use UTF-8\r\n\"No\" to use UTF-16 (Unicode)", "", MessageBoxButton.YesNo);
            if (encodingSelected == MessageBoxResult.None) return;
            Log.Assert(encodingSelected == MessageBoxResult.Yes || encodingSelected == MessageBoxResult.No);
            var encoding = encodingSelected == MessageBoxResult.Yes ? Encoding.UTF8 : Encoding.Unicode;
            int dataPreprocessMethodId = radioPanel_newData.SelectedIndex;
            bool debugMode = (bool)checkBox_debugMode.IsChecked;
            await stackPanel_tasksQueue.EnqueueTaskAsync($"Read Data {fileName} {encoding} {debugMode} {dataPreprocessMethodId}", new Func<Task>(async () =>
            {
                mainData = await Task.Run(() =>TextProcess.ReadTextStream(new FileStream(fileName, FileMode.Open, FileAccess.Read), encoding, debugMode));
                await Task.Run(() =>TextProcess.Process(ref mainData, dataPreprocessMethodId));
                Log.Write(" OK");
            }));
        }
        void BuildData(Stream s,Encoding encoding,int dataPreprocessMethodId,int maxWordLength,bool debugMode)
        {
            Log.SubTask( () =>
            {
                Log.AppendLog($"maxWordLength: {maxWordLength}");
                int methodId = radioPanel_newData.SelectedIndex;
                Log.WriteLine($"Data preprocessing method: {dataPreprocessMethodId}");
                string data = TextProcess.ReadTextStream(s, encoding, debugMode);
                Log.AppendLog($"Charactors Read = {data.Length}");
                Log.WriteLine("Preprocessing Data...");
                TextProcess.Process(ref data, dataPreprocessMethodId);
                OutputText = data.Length > 10000 ? data.Remove(10000) : data;
                long baseDataLength = data.Length;
                Log.AppendLog($"baseDataLength: {baseDataLength}");
                Dispatcher.Invoke(() => inputField_data["baseDataLength"].Text = baseDataLength.ToString());
                Log.WriteLine("TrieTabPage.BuildData...");
                trie.Build(data, maxWordLength);
                Log.Write(" OK");
            });
        }
    }
}
