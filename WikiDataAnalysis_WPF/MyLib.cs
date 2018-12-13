using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using Microsoft.Win32;
using System.Diagnostics;

namespace WikiDataAnalysis_WPF
{
    static class MyLib
    {
        public static UIElement Do(this UIElement uIElement, Action<UIElement> action)
        {
            action(uIElement);
            return uIElement;
        }
        public static UIElement Set(this UIElement uIElement, int row, int column)
        {
            Grid.SetRow(uIElement, row);
            Grid.SetColumn(uIElement, column);
            return uIElement;
        }
        public static UIElement SetSpan(this UIElement uIElement, int rowSpan, int columnSpan)
        {
            Grid.SetRowSpan(uIElement, rowSpan);
            Grid.SetColumnSpan(uIElement, columnSpan);
            return uIElement;
        }
        public static double ScreenWidth { get { return SystemParameters.PrimaryScreenWidth; } }
        public static double ScreenHeight { get { return SystemParameters.PrimaryScreenHeight; } }
        public static bool IsChinese(char c)
        {
            return '\u4e00' <= c && c <= '\u9fff';
        }
        public static string Open()
        {
            var fd = new OpenFileDialog();
            //fd.FileName = "wiki.sav";
            if (fd.ShowDialog() == true)
            {
                return fd.FileName;
            }
            return null;
        }
        public static string Save()
        {
            var sd = new SaveFileDialog();
            if (sd.ShowDialog() == true)
            {
                return sd.FileName;
            }
            return null;
        }
        //public static Stream OpenStream()
        //{
        //    var fd = new OpenFileDialog();
        //    //fd.FileName = "wiki.sav";
        //    if (fd.ShowDialog() == true)
        //    {
        //        return fd.OpenFile();
        //    }
        //    return null;
        //}
        //public static Stream SaveStream()
        //{
        //    var sd = new SaveFileDialog();
        //    if (sd.ShowDialog() == true)
        //    {
        //        return sd.OpenFile();
        //    }
        //    return null;
        //}
        public static void OpenSave(Action<string, string> action)
        {
            var fs = Open();
            if (fs == null) return;
            var ss = Save();
            if (ss == null) return;
            action(fs, ss);
        }
        public static async Task OpenSave(Func<string, string, Task> action)
        {
            var fs = Open();
            if (fs == null) return;
            var ss = Save();
            if (ss == null) return;
            await action(fs, ss);
        }
        public static void Assert(bool condition) { Trace.Assert(condition); }
    }
}
