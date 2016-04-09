using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin.Hosting;

namespace ScreenBroadcaster.Server.Controllers
{
    public class ServerController
    {
        // Constants.
        const string                SERVER_URI = "http://localhost:8080";

        // Instanse members.
        public IDisposable          SignalR { get; set; }

        private ServerMainWindow    _serverMainWindow;

        public ServerController(ServerMainWindow serverMainWindow)
        {
            _serverMainWindow = serverMainWindow;
            subscribeForServerMainWindowEvents();
        }

        private void subscribeForServerMainWindowEvents()
        {
            _serverMainWindow.StartButton.Click += StartButton_Click;
            _serverMainWindow.StopButton.Click += StopButton_Click;
        }

        public void WriteToConsole(String message)
        {
            if (!(_serverMainWindow.ConsoleRichTextBox.CheckAccess()))
            {
                this._serverMainWindow.Dispatcher.Invoke(() =>
                    WriteToConsole(message)
                );
                return;
            }

            _serverMainWindow.ConsoleRichTextBox.AppendText(message + "\r");
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
                _serverMainWindow.Dispatcher.Invoke(() => _serverMainWindow.StartButton.IsEnabled = true);
                return;
            }

            _serverMainWindow.Dispatcher.Invoke(() => _serverMainWindow.StopButton.IsEnabled = true);
            WriteToConsole("Server started at " + SERVER_URI);
        }


        // Events handlers.
        void StartButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            WriteToConsole("Starting server...");
            _serverMainWindow.StartButton.IsEnabled = false;
            Task.Run(() => StartServer());
        }

        void StopButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            SignalR.Dispose();
            _serverMainWindow.Close();
        }
    }
}
