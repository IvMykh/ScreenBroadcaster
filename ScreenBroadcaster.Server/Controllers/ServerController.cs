using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin.Hosting;

using ScreenBroadcaster.Common;

namespace ScreenBroadcaster.Server.Controllers
{
    public class ServerController
    {
        // Constants.
        const string                    SERVER_URI = "http://localhost:8080";

        // Instanse members.
        public IDisposable              SignalR                         { get; private set; }
        public ServerMainWindow         ServerMainWindow                { get; private set; }

        public ServerController(ServerMainWindow serverMainWindow)
        {
            ServerMainWindow = serverMainWindow;
            setServerMainWindowEventsHandlers();
        }

        private void setServerMainWindowEventsHandlers()
        {
            ServerMainWindow.StartButton.Click += StartButton_Click;
            ServerMainWindow.StopButton.Click += StopButton_Click;
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

            ServerMainWindow.ConsoleRichTextBox.AppendText(message + "\r");
        }

        private void StartServer()
        {
            try
            {
                SignalR = WebApp.Start(SERVER_URI);
            }
            catch (TargetInvocationException)
            {
                WriteToConsole("A server is already running at " + SERVER_URI);
                ServerMainWindow.Dispatcher.Invoke(() => ServerMainWindow.StartButton.IsEnabled = true);
                return;
            }

            ServerMainWindow.Dispatcher.Invoke(() => ServerMainWindow.StopButton.IsEnabled = true);
            WriteToConsole("Server started at " + SERVER_URI);
        }


        // Events handlers.
        void StartButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            WriteToConsole("Starting server...");
            ServerMainWindow.StartButton.IsEnabled = false;
            Task.Run(() => StartServer());
        }

        void StopButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            SignalR.Dispose();
            ServerMainWindow.Close();
        }
    }
}
