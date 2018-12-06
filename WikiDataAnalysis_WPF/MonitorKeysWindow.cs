using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace WikiDataAnalysis_WPF
{
    public partial class MonitorKeysWindow:Window
    {
        Dictionary<Key, KeyStates> keyStates = new Dictionary<Key, KeyStates>();
        public MonitorKeysWindow()
        {
            this.KeyDown += MonitorKeysWindow_KeyDown;
            this.KeyUp += MonitorKeysWindow_KeyDown;
        }
        protected KeyStates GetKeyStates(params Key[] keys) { return keys.Select(k => keyStates.ContainsKey(k) ? keyStates[k] : KeyStates.None).Aggregate((a, b) => a | b); }
        protected bool IsDown(params Key[] keys) { return (GetKeyStates(keys) & KeyStates.Down) != 0; }
        private void MonitorKeysWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (!keyStates.ContainsKey(e.Key)) keyStates.Add(e.Key, e.KeyStates);
            else keyStates[e.Key] = e.KeyStates;
        }
    }
}
