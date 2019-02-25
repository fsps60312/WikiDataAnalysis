using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace WikiDataAnalysis_WPF
{
    partial class TrieTabItem
    {
        class TaskQueueStackPanel :StackPanel
        {
            class TaskItemGrid : Grid
            {
                Button button_trash = new Button { Content = "🗑" }, button_start = new Button { Content = "▶" };
                bool manual = false;
                public TaskItemGrid(string name, Func<Task> task)
                {
                    this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
                    this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
                    this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    this.Children.Add(button_start.Set(0, 0));
                    this.Children.Add(button_trash.Set(0, 1));
                    this.Children.Add(new Label { Content = name }.Set(0, 2));
                    this.task = new Func<Task>(async () =>
                    {
                        button_start.IsEnabled = button_trash.IsEnabled = false;
                        button_start.Content = manual ? "👤" : "⌛";
                        try
                        {
                            await task();
                        }
                        finally
                        {
                            OnRemoveTaskClicked();
                        }
                    });
                    button_trash.Click += delegate { if (MessageBox.Show("Remove the task?", "Sure?", MessageBoxButton.OKCancel) == MessageBoxResult.OK) OnRemoveTaskClicked(); };
                    button_start.Click += delegate { OnStartTaskClicked(); };
                }
                public delegate void ButtonClickedEventHandler();
                public event ButtonClickedEventHandler RemoveTaskClicked, StartTaskClicked;
                void OnRemoveTaskClicked() { RemoveTaskClicked?.Invoke(); }
                void OnStartTaskClicked() { manual = true; StartTaskClicked?.Invoke(); }
                Func<Task> task;
                public async Task RunTaskAsync()
                {
                    await task();
                }
            }
            public TaskQueueStackPanel()
            {
                this.Children.Add(new Label { Content = "Task Queue:" });
            }
            List<int> taskQueue = new List<int>();
            Dictionary<int, TaskItemGrid> tasks = new Dictionary<int, TaskItemGrid>();
            int taskIdCounter = 0;
            bool isQueueRunning = false;
            async Task CheckTaskQueue()
            {
                if (isQueueRunning) return;
                index_continue:;
                try
                {
                    isQueueRunning = true;
                    while (taskQueue.Count > 0)
                    {
                        var taskId = taskQueue[0];
                        var task = tasks[taskId];
                        taskQueue.RemoveAt(0); tasks.Remove(taskId);
                        await task.RunTaskAsync();
                    }
                }
                catch(Exception error)
                {
                    if (MessageBox.Show(error.ToString(), "Try Continue?", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        goto index_continue;
                    }
                }
                finally { isQueueRunning = false; }
            }
            public async Task EnqueueTaskAsync(string name,Func<Task> task)
            {
                int taskId=taskIdCounter++;
                var taskItem = new TaskItemGrid(name, task);
                tasks.Add(taskId, taskItem);
                taskQueue.Add(taskId);
                taskItem.RemoveTaskClicked += delegate
                  {
                      taskQueue.Remove(taskId);
                      tasks.Remove(taskId);
                      this.Children.Remove(taskItem);
                  };
                taskItem.StartTaskClicked += async delegate { await taskItem.RunTaskAsync(); };
                this.Children.Add(taskItem);
                await CheckTaskQueue();
            }
        }

    }
}
