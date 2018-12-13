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
    partial class TrieTabItem
    {
        TextBox textBox_in, textBox_out, textBox_data;
        Button button_exportList, button_save, button_load, button_new, button_split, button_performIteration;
        CheckBox checkBox_debugMode;
        TaskQueueStackPanel stackPanel_tasksQueue;
        InputField inputField_data;
        RadioPanel radioPanel_newData;
        string OutputText
        {
            get
            {
                return Dispatcher.Invoke(() =>textBox_out.Text);
            }
            set
            {
                Dispatcher.Invoke(() => textBox_out.Text = value);
            }
        }
        void InitializeViews()
        {
            textBox_in = new TextBox();
            textBox_out = new TextBox();
            textBox_data = new TextBox();
            button_exportList = new Button { Content = "Export List" };
            button_load = new Button { Content = "Load" };
            button_save = new Button { Content = "Save" };
            button_new = new Button { Content = "Build" };
            button_performIteration = new Button { Content = "Perform Iteration" };
            button_split = new Button { Content = "Split" };
            checkBox_debugMode = new CheckBox { Content = "Debug Mode", IsChecked = true };
            stackPanel_tasksQueue = new TaskQueueStackPanel();
            inputField_data = new InputField();
            inputField_data.AddField("maxWordLength", "4");
            inputField_data.AddField("baseDataLength", "-1");
            radioPanel_newData = new RadioPanel("Date preprocessing", "None", "Non-Chinese => Blank", "Non-Chinese => Removed");
            this.Content = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition{Height=new GridLength(2,GridUnitType.Star)},
                    new RowDefinition{Height=new GridLength(2,GridUnitType.Star)},
                    new RowDefinition{Height=new GridLength(1,GridUnitType.Star)}
                },
                ColumnDefinitions =
                {
                    new ColumnDefinition{Width=new GridLength(2,GridUnitType.Star)},
                    new ColumnDefinition{Width=new GridLength(1,GridUnitType.Star)}
                },
                Children =
                {
                    new Grid
                    {
                        Children={textBox_in}
                    }.Set(0,0),
                    new Grid
                    {
                        Children={textBox_out}
                    }.Set(1,0),
                    new Grid
                    {
                        Children={textBox_data}
                    }.Set(2,0),
                    new StackPanel
                    {
                        CanVerticallyScroll=true,
                        Children=
                        {
                            checkBox_debugMode,
                            button_load,
                            button_split,
                            button_performIteration,
                            button_save,
                            button_new,
                            radioPanel_newData,
                            button_exportList,
                            inputField_data,
                            stackPanel_tasksQueue
                        }
                    }.Set(0,1).SetSpan(3,1)
                }
            };
        }
        void RegisterEvents()
        {
            textBox_in.KeyDown += (sender, e) => { if (e.Key == Key.Enter && MainWindow.IsDown(Key.LeftCtrl)) ProcessInput(textBox_in.Text); };
            textBox_data.MouseDoubleClick += TextBox_data_MouseDoubleClick;
            button_exportList.Click += Button_exportList_Click;
            button_split.Click += Button_split_Click;
            button_load.Click += Button_load_Click;
            button_save.Click += Button_save_Click;
            button_new.Click += Button_new_Click;
            button_performIteration.Click += Button_performIteration_Click;
        }
        public TrieTabItem()
        {
            this.Header = "Trie";
            InitializeViews();
            RegisterEvents();
            Log.AppendLog("TrieTabItem OK.");
        }
    }
}
