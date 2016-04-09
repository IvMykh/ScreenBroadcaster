using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.AspNet.SignalR.Client;

namespace ScreenBroadcaster.Client.Controllers
{
    public class ClientController
    {
        // Constants.
        private const string    SERVER_URI = "http://localhost:8080/signalr";

        // Instanse members.
        private IntStriker      _intStriker;
        //private NumberGenerator _numGen = new NumberGenerator();

        public string           UserName    { get; set; }
        public IHubProxy        HubProxy    { get; set; }
        public HubConnection    Connection  { get; set; }


        private ClientMainWindow _clientMainWindow;

        public ClientController(ClientMainWindow clientMainWindow)
        {
            _clientMainWindow   = clientMainWindow;
            subscribeForClientMainWindowEvents();

            _intStriker     = new IntStriker(500);
        }

        private void subscribeForClientMainWindowEvents()
        {
            _clientMainWindow.SignInButton.Click += SignInButton_Click;
            _clientMainWindow.SendAllButton.Click += SendAllButton_Click;
            _clientMainWindow.Closing += _clientMainWindow_Closing;
            _clientMainWindow.StartStopGenButton.Click += StartStopGenButton_Click;
        }

        private async void ConnectAsync()
        {
            Connection = new HubConnection(SERVER_URI);
            Connection.Closed += Connection_Closed;

            HubProxy = Connection.CreateHubProxy("MyHub");

            HubProxy.On<string, string>("addMessage", (name, message) =>
                _clientMainWindow.Dispatcher.Invoke(() =>
                    _clientMainWindow.ConsoleRichTextBox.AppendText(string.Format("{0}: {1}\r", name, message))
                    )
                );

            try
            {
                await Connection.Start();
            }
            catch (HttpRequestException)
            {
                _clientMainWindow.StatusText.Content = "Unable to connect to server: Start server before connecting clients.";
                return;
            }

            //Show chat UI; hide login UI
            _clientMainWindow.SignInPanel.Visibility    = Visibility.Collapsed;
            _clientMainWindow.ChatPanel.Visibility      = Visibility.Visible;
            _clientMainWindow.SendAllButton.IsEnabled   = true;
            _clientMainWindow.ConsoleRichTextBox.AppendText(
                string.Format("Connected to server at {0} \r", SERVER_URI));
            _clientMainWindow.MessageTextBox.Focus();
        }


        // Events handlers.
        private void StartStopGenButton_Click(object sender, RoutedEventArgs e)
        {
            if (_intStriker.IsDisposed)
            {
                _intStriker.NewPictureGenerated +=
                    new EventHandler<NewPictureEventArgs>((newSender, newE) =>
                    {
                        _clientMainWindow.Dispatcher.Invoke(() => _clientMainWindow.NumDisplayLabel.Content =
                            string.Format("New picture: {0}", newE.NewPicture.ToString()));
                    });

                _intStriker.Start();
                _clientMainWindow.StartStopGenButton.Content = "Stop Generation";
            }
            else
            {
                _intStriker.Stop();
                _clientMainWindow.StartStopGenButton.Content = "Start Generation";
            }
        }

        private void _clientMainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (Connection != null)
            {
                Connection.Stop();
                Connection.Dispose();
            }

            if (!_intStriker.IsDisposed)
            {
                _intStriker.Dispose();
            }
        }

        private void SignInButton_Click(object sender, RoutedEventArgs e)
        {
            UserName = _clientMainWindow.UserNameTextBox.Text;

            if (!string.IsNullOrEmpty(UserName))
            {
                _clientMainWindow.StatusText.Visibility = Visibility.Visible;
                _clientMainWindow.StatusText.Content = "Connecting to server...";
                ConnectAsync();
            }
        }

        private void SendAllButton_Click(object sender, RoutedEventArgs e)
        {
            HubProxy.Invoke("Send", UserName, _clientMainWindow.MessageTextBox.Text);
            _clientMainWindow.MessageTextBox.Text = string.Empty;
            _clientMainWindow.MessageTextBox.Focus();
        }

        private void Connection_Closed()
        {
            //Show login UI; hide chat UI
            var dispatcher = Application.Current.Dispatcher;

            dispatcher.Invoke(() => _clientMainWindow.ChatPanel.Visibility      = Visibility.Collapsed);
            dispatcher.Invoke(() => _clientMainWindow.SignInPanel.Visibility    = Visibility.Visible);
            dispatcher.Invoke(() => _clientMainWindow.SendAllButton.IsEnabled   = false);
            dispatcher.Invoke(() => _clientMainWindow.StatusText.Content        = "You have been disconnected.");
        }
    }
}
