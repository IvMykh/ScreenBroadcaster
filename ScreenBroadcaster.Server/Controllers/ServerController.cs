using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin.Hosting;
using ScreenBroadcaster.Common;
using ScreenBroadcaster.Server.Properties;

namespace ScreenBroadcaster.Server.Controllers
{
    public class ServerController
    {

        // Instanse members.
        public string           ServerUri           { get; private set; }
        public IDisposable      SignalR             { get; private set; }
        public ServerMainWindow ServerMainWindow    { get; private set; }

        public ServerController(ServerMainWindow serverMainWindow)
        {
            ServerUri = ConfigurationManager.AppSettings["ServerUri"];
            ServerMainWindow = serverMainWindow;
            setServerMainWindowEventsHandlers();
        }

        public void WriteToConsole(String message)
        {
            if (!(ServerMainWindow.ConsoleRichTextBox.CheckAccess()))
            {
                this.ServerMainWindow.Dispatcher.Invoke(() =>
                    WriteToConsole(message)
                );
                return;
            }

            ServerMainWindow.ConsoleRichTextBox.AppendText(message + '\r');
        }

        private void setServerMainWindowEventsHandlers()
        {
            ServerMainWindow.StartButton.Click += StartButton_Click;
            ServerMainWindow.StopButton.Click += StopButton_Click;
        }
        private void startServer()
        {
            try
            {
                SignalR = WebApp.Start(ServerUri);
            }
            catch (TargetInvocationException)
            {
                WriteToConsole(string.Format(Resources.ServerAlreadyRunningMsgFormat, ServerUri));
                ServerMainWindow.Dispatcher.Invoke(() => ServerMainWindow.StartButton.IsEnabled = true);
                return;
            }

            ServerMainWindow.Dispatcher.Invoke(() => ServerMainWindow.StopButton.IsEnabled = true);
            WriteToConsole(string.Format(Resources.ServerStartedAtMsgFormat, ServerUri));
        }


        // Events handlers.
        void StartButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            WriteToConsole(Resources.StartingServerMsg);
            ServerMainWindow.StartButton.IsEnabled = false;
            Task.Run(() => startServer());
        }
        void StopButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            WriteToConsole(Resources.StartingServerMsg);
            SignalR.Dispose();
            ServerMainWindow.Close();
        }
    }
}
