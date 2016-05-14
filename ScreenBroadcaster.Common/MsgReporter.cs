using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ScreenBroadcaster.Common
{
    public class MsgReporter
    {
        public static MsgReporter Instance { get; private set; }

        static MsgReporter()
        {
            Instance = new MsgReporter();
        }


        private MsgReporter()
        {
        }

        public void ReportInfo(string msg, string caption)
        {
            MessageBox.Show(msg, caption, MessageBoxButton.OK, MessageBoxImage.Information);
        }
        public void ReportError(string msg, string caption)
        {
            MessageBox.Show(msg, caption, MessageBoxButton.OK, MessageBoxImage.Error);
        }
        public void ReportWarning(string msg, string caption)
        {
            MessageBox.Show(msg, caption, MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
