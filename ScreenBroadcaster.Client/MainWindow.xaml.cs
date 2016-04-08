using System;
using System.Windows;
using System.Net.Http;
using Microsoft.AspNet.SignalR.Client;

namespace ScreenBroadcaster.Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Constants.
        private const string SERVER_URI = "http://localhost:8080/signalr";

        // Instance members;
        public string           UserName    { get; set; }
        public IHubProxy        HubProxy    { get; set; }
        public HubConnection    Connection  { get; set; }

        private IntStriker _picCap;

        private NumberGenerator _numGen = new NumberGenerator();

        public MainWindow()
        {
            InitializeComponent();

            _picCap = new IntStriker(500);
        }

        private void SignInButton_Click(object sender, RoutedEventArgs e)
        {
            UserName = UserNameTextBox.Text;

            if (!string.IsNullOrEmpty(UserName))
            {
                StatusText.Visibility   = Visibility.Visible;
                StatusText.Content      = "Connecting to server...";
                ConnectAsync();
            }
        }

        private void ButtonSendAll_Click(object sender, RoutedEventArgs e)
        {
            HubProxy.Invoke("Send", UserName, TextBoxMessage.Text);
            TextBoxMessage.Text = string.Empty;
            TextBoxMessage.Focus();
        }

        private void ButtonSendMe_Click(object sender, RoutedEventArgs e)
        {
            HubProxy.Invoke("Send", TextBoxMessage.Text);
            TextBoxMessage.Text = string.Empty;
            TextBoxMessage.Focus();
        }

        private async void ConnectAsync()
        {
            Connection = new HubConnection(SERVER_URI);
            Connection.Closed += Connection_Closed;

            HubProxy = Connection.CreateHubProxy("MyHub");

            HubProxy.On<string, string>("addMessage", (name, message) =>
                this.Dispatcher.Invoke(() =>
                    RichTextBoxConsole.AppendText(string.Format("{0}: {1}\r", name, message))
                    )
                );

            try
            {
                await Connection.Start();
            }
            catch (HttpRequestException)
            {
                StatusText.Content = "Unable to connect to server: Start server before connecting clients.";
                return;
            }

            //Show chat UI; hide login UI
            SignInPanel.Visibility  = Visibility.Collapsed;
            ChatPanel.Visibility    = Visibility.Visible;
            ButtonSendAll.IsEnabled = true;
            ButtonSendMe.IsEnabled  = true;
            TextBoxMessage.Focus();
            RichTextBoxConsole.AppendText("Connected to server at " + SERVER_URI + "\r");

        }

        private void Connection_Closed()
        {
            var dispatcher = Application.Current.Dispatcher;
            dispatcher.Invoke(() => ChatPanel.Visibility = Visibility.Collapsed);
            dispatcher.Invoke(() => ButtonSendAll.IsEnabled = false);
            dispatcher.Invoke(() => ButtonSendMe.IsEnabled = false);
            dispatcher.Invoke(() => StatusText.Content = "You have been disconnected.");
            dispatcher.Invoke(() => SignInPanel.Visibility = Visibility.Visible);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (Connection != null)
            {
                Connection.Stop();
                Connection.Dispose();
            }

            if (!_picCap.IsDisposed)
            {
                _picCap.Dispose();
            }
        }

        private void startStopGenButton_Click(object sender, RoutedEventArgs e)
        {
            if (_picCap.IsDisposed)
            {
                _picCap.NewPictureGenerated += 
                    new EventHandler<NewPictureEventArgs>((newSender, newE) => 
                        {
                            Dispatcher.Invoke(() => numDisplayLabel.Content =
                                string.Format("New picture: {0}", newE.NewPicture.ToString()));
                        });

                _picCap.Start();
                startStopGenButton.Content = "Stop Generation";
            }
            else
            {
                _picCap.Stop();
                startStopGenButton.Content = "Start Generation";
            }
        }
    }
}
