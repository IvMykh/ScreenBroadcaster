using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.AspNet.SignalR.Client;
using ScreenBroadcaster.Common;

namespace ScreenBroadcaster.Client.Controllers
{
    public class ClientController
    {
        // Constants.
        private const string    SERVER_URI = "http://localhost:8080/signalr";

        // Instanse members.
        private ClientMainWindow                _clientMainWindow;
        private IntStriker                      _intStriker;

        public User                             User                { get; private set; }
        public Guid?                            BroadcasterID       { get; private set; }
        public ServerToClientCommandsExecutor   CommandsExecutor    { get; private set; }

        public IHubProxy                        CommandsHubProxy    { get; private set; }
        public IHubProxy                        PicturesHubProxy    { get; private set; }
        public HubConnection                    Connection          { get; private set; }



        public ClientController(ClientMainWindow clientMainWindow)
        {
            _clientMainWindow   = clientMainWindow;
            setClientMainWindowEventsHandlers();

            _intStriker     = new IntStriker(500);


            User                = new User();
            BroadcasterID       = null;
            CommandsExecutor    = new ServerToClientCommandsExecutor();
        }

        private void setClientMainWindowEventsHandlers()
        {
            _clientMainWindow.Closing                       += _clientMainWindow_Closing;
            _clientMainWindow.UserNameTextBox.TextChanged   += UserNameTextBox_TextChanged;
            _clientMainWindow.BroadcasterIdTextBox.TextChanged += BroadcasterIdTextBox_TextChanged;

            _clientMainWindow.BroadcastButton.Click += BroadcastButton_Click;
            _clientMainWindow.ReceiveButton.Click += ReceiveButton_Click;
        }

        // Events handlers.
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

        private void Connection_Closed()
        {
            // Show login UI; hide broadcast and receive UI.
            var dispatcher = Application.Current.Dispatcher;

            dispatcher.Invoke(() =>
            {
                _clientMainWindow.BroadcastUI.Visibility = Visibility.Collapsed;
                _clientMainWindow.ReceiveUI.Visibility = Visibility.Collapsed;
            });

            dispatcher.Invoke(() => _clientMainWindow.SignInUI.Visibility = Visibility.Visible);
        }


        private async void BroadcastButton_Click(object sender, RoutedEventArgs e)
        {
            if (isUserNameValid())
            {
                bool isConnected = await ConnectAsync();

                if (isConnected) //Connection.State == ConnectionState.Connected
                {
                    //var param = new CommandParameter();
                    //param["user"]     = User;
                    //
                    //CommandsHubProxy.Invoke(
                    //    "ExecuteCommand", ClientToServerCommand.RegisterNewBroadcaster, param);

                    await CommandsHubProxy.Invoke("ExecuteCommand", ClientToServerCommand.RegisterNewBroadcaster, User);

                    activateSignInUI(false);
                    activateBroadcastUI(true);
                }
            }
        }
        
        private void ReceiveButton_Click(object sender, RoutedEventArgs e)
        {
            if (isUserNameValid() && isBroadcasterIDValid())
            {
                bool isConnected = ConnectAsync().Wait(0);

                if (Connection.State == ConnectionState.Connected)
                {
                    activateSignInUI(false);
                    activateReceiveUI(true);
                }
            }
        }

        private void UserNameTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            User.Name = _clientMainWindow.UserNameTextBox.Text;
        }

        private void BroadcasterIdTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            Guid broadcasterID = default(Guid);            
            if (Guid.TryParse(_clientMainWindow.BroadcasterIdTextBox.Text, out broadcasterID))
            {
                BroadcasterID = broadcasterID;
            }
            else
            {
                BroadcasterID = null;
            }
        }

        // Other methods.
        private async Task<bool> ConnectAsync()
        {
            Connection = new HubConnection(SERVER_URI);
            Connection.Closed += Connection_Closed;

            setupCommandsHub();
            //setupPicturesHub();

            try
            {
                await Connection.Start();
            }
            catch (HttpRequestException)
            {
                string text = "Unable to connect to server: Start server before connecting clients.";
                string caption = "Server connection error!";
                MessageBox.Show(text, caption);

                return false;
            }

            //Show chat UI; hide login UI.
            return true;
        }

        private void setupCommandsHub()
        {
            CommandsHubProxy = Connection.CreateHubProxy("CommandsHub");

            CommandsHubProxy.On<ServerToClientCommand, CommandParameter>(
                "ExecuteCommand", (command, param) => CommandsExecutor.ExecuteCommand(command, param));
        }

        private void setupPicturesHub()
        {
            PicturesHubProxy = Connection.CreateHubProxy("PicturesHub");

            // TODO: implement.
        }

        private bool isUserNameValid()
        {
            if (!string.IsNullOrEmpty(User.Name))
            {
                return true;
            }
            else
            {
                string text = "You must specify user name.";
                string caption = "User name error!";
                MessageBox.Show(text, caption);
                
                return false;
            }
        }

        private bool isBroadcasterIDValid()
        {
            if (BroadcasterID.HasValue)
            {
                return true;
            }
            else
            {
                string text = "You must specify broadcaster ID.";
                string caption = "Broadcaster ID error!";
                MessageBox.Show(text, caption);

                return false;
            }
        }

        private void activateSignInUI(bool shouldActivate)
        {
            if (shouldActivate)
            {
                _clientMainWindow.SignInUI.Visibility = Visibility.Visible;
                _clientMainWindow.UserNameTextBox.IsReadOnly = false;
            }
            else
            {
                _clientMainWindow.SignInUI.Visibility = Visibility.Collapsed;
                _clientMainWindow.UserNameTextBox.IsReadOnly = true;
            }
        }

        private void activateBroadcastUI(bool shouldActivate)
        {
            if (shouldActivate)
            {
                _clientMainWindow.BroadcastUI.Visibility = Visibility.Visible;
                _clientMainWindow.UserIDTextBox.Text = User.ID.ToString();
            }
            else
            {
                _clientMainWindow.BroadcastUI.Visibility = Visibility.Collapsed;
                _clientMainWindow.UserIDTextBox.Text = string.Empty;
            }
        }

        private void activateReceiveUI(bool shouldActivate)
        {
            if (shouldActivate)
            {
                _clientMainWindow.ReceiveUI.Visibility = Visibility.Visible;
                _clientMainWindow.UserIDTextBox.Text = User.ID.ToString();
                _clientMainWindow.BroadcasterIDForReceiverTextBox.Text = BroadcasterID.Value.ToString();
            }
            else
            {
                _clientMainWindow.ReceiveUI.Visibility = Visibility.Collapsed;
                _clientMainWindow.UserIDTextBox.Text = string.Empty;
                _clientMainWindow.BroadcasterIDForReceiverTextBox.Text = null;
            }
        }

        public class ServerToClientCommandsExecutor
        {
            private IDictionary<ServerToClientCommand, Action<CommandParameter>> _handlers;

            public ServerToClientCommandsExecutor()
            {
                _handlers = setupHandlers();
            }

            private IDictionary<
                ServerToClientCommand, Action<CommandParameter>> setupHandlers()
            {
                var handlers = new Dictionary<ServerToClientCommand, Action<CommandParameter>>();

                handlers[ServerToClientCommand.ReportSuccessfulBcasterRegistration] =
                    new Action<CommandParameter>((param) =>
                        {
                            string text = "You have been successfully registered as a Broadcaster.";
                            string caption = "Registration succeeded!";
                            MessageBox.Show(text, caption);
                        });

                return handlers;
            }

            public void ExecuteCommand(ServerToClientCommand command, CommandParameter param)
            {
                _handlers[command](param);
            }
        }
    }
}
