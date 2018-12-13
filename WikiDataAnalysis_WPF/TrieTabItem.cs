using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.IO;

namespace WikiDataAnalysis_WPF
{
    partial class TrieTabItem:TabItem
    {
        Trie trie = new Trie();
        string mainData = null;
        private void Button_split_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        void ProcessInput(string text)
        {
        }

        private void Button_performIteration_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private async void Button_new_Click(object sender, RoutedEventArgs e)
        {
            var s = MyLib.Open();
            if (s == null) return;
            var encodingSelected = MessageBox.Show("\"Yes\" to use UTF-8\r\n\"No\" to use UTF-16 (Unicode)", "", MessageBoxButton.YesNo);
            if (encodingSelected == MessageBoxResult.None) return;
            Log.Assert(encodingSelected == MessageBoxResult.Yes || encodingSelected == MessageBoxResult.No);
            var encoding = encodingSelected == MessageBoxResult.Yes ? Encoding.UTF8 : Encoding.Unicode;
            int maxWordLength = int.Parse(inputField_data["maxWordLength"].Text);
            bool debugMode = (bool)checkBox_debugMode.IsChecked;
            int processMethod = radioPanel_newData.SelectedIndex;
            await stackPanel_tasksQueue.EnqueueTaskAsync($"BuildDataAsync({s},{encoding},{processMethod},{maxWordLength},{debugMode})", new Func<Task>(async () => await Task.Run(() => BuildData(
                new FileStream(s, FileMode.Open, FileAccess.Read),
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
            var s = MyLib.Open();
            if (s == null) return;
            var encodingSelected = MessageBox.Show("\"Yes\" to use UTF-8\r\n\"No\" to use UTF-16 (Unicode)", "", MessageBoxButton.YesNo);
            if (encodingSelected == MessageBoxResult.None) return;
            Log.Assert(encodingSelected == MessageBoxResult.Yes || encodingSelected == MessageBoxResult.No);
            var encoding = encodingSelected == MessageBoxResult.Yes ? Encoding.UTF8 : Encoding.Unicode;
            int dataPreprocessMethodId = radioPanel_newData.SelectedIndex;
            bool debugMode = (bool)checkBox_debugMode.IsChecked;
            await stackPanel_tasksQueue.EnqueueTaskAsync($"Read Data {s} {encoding} {debugMode} {dataPreprocessMethodId}", new Func<Task>(async () =>
            {
                mainData = await Task.Run(() => ReadTextStream(new FileStream(s, FileMode.Open, FileAccess.Read), encoding, debugMode));
                await Task.Run(() => ProcessText(ref mainData, dataPreprocessMethodId));
                Log.Write(" OK");
            }));
        }
        string ReadTextStream(Stream s,Encoding encoding,bool debugMode)
        {
            var sb = new StringBuilder();
            using (StreamReader reader = new StreamReader(s, encoding))
            {
                Log.WriteLine("Reading...");
                bool warning = true;
                for (char[] buf = new char[1024 * 1024]; ;)
                {
                    int n = reader.ReadBlock(buf, 0, buf.Length);
                    if (n == 0) break;
                    for (int i = 0; i < n; i++)
                    {
                        sb.Append(buf[i]);
                        const int stringMaxLength = int.MaxValue / 2 - 100;
                        if (warning&& sb.Length > stringMaxLength)
                        {
                            warning = false;
                            if (MessageBox.Show($"Reach C# string max length: {sb.Length}, break?", "Warning", MessageBoxButton.OKCancel) == MessageBoxResult.OK) goto index_skipRead;
                        }
                    }
                    Log.WriteLine($"Reading...{s.Position}/{s.Length}");
                    if (debugMode && s.Position > 1000000) break;
                }
                index_skipRead:;
                return sb.ToString();
            }
        }
        void ProcessText(ref string data, int methodId)
        {
            var sb = new StringBuilder();
            switch (methodId)
            {
                case 0:return;
                case 1://non-chinese => space
                    {
                        int percentage = -1;
                        long progress = 0, total_progress = data.Length;
                        char last_char = '\0';
                        foreach (char c in data)
                        {
                            if (++progress * 100 / total_progress > percentage) Log.WriteLine($"Preprocessing {++percentage}%: non-chinese => space");
                            if (!MyLib.IsChinese(c))
                            {
                                if (last_char != ' ') sb.Append(last_char = ' ');
                            }
                            else sb.Append(last_char = c);
                        }
                        data = sb.ToString();
                        return;
                    }
                case 2://non-chinese => removed
                    {
                        int percentage = -1;
                        long progress = 0, total_progress = data.Length;
                        char last_char = '\0';
                        foreach (char c in data)
                        {
                            if (++progress * 100 / total_progress > percentage) Log.WriteLine($"Preprocessing {++percentage}%: non-chinese => space");
                            if (MyLib.IsChinese(c)) sb.Append(last_char = c);
                        }
                        data = sb.ToString();
                        return;
                    }
                default: throw new Exception($"Unknown methodId: {methodId}");
            }
        }
        void BuildData(Stream s,Encoding encoding,int dataPreprocessMethodId,int maxWordLength,bool debugMode)
        {
            Log.SubTask( () =>
            {
                Log.AppendLog($"maxWordLength: {maxWordLength}");
                int methodId = radioPanel_newData.SelectedIndex;
                Log.WriteLine($"Data preprocessing method: {dataPreprocessMethodId}");
                string data = ReadTextStream(s, encoding, debugMode);
                Log.AppendLog($"Charactors Read = {data.Length}");
                Log.WriteLine("Preprocessing Data...");
                ProcessText(ref data, dataPreprocessMethodId);
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
