﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.AspNet.SignalR.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ScreenBroadcaster.Common;
using ScreenBroadcaster.Common.CommandTypes;

// TODO: make server notify clients about its stopping.

namespace ScreenBroadcaster.Client.Controllers
{
    public partial class ClientController
    {
        public partial class GeneralCommandsExecutor { }
        public partial class PictureSender
            : IDisposable { }

        // Constants.
        private const string                    SERVER_URI = "http://localhost:8080/signalr";

        // Instanse members.
        private ClientMainWindow                _clientMainWindow;

        public ClientMainWindow                 MainWindow { get { return _clientMainWindow; } }

        public User                             User                { get; private set; }
        public bool                             IsRegistered        { get; private set; }
        public Guid?                            BroadcasterID       { get; private set; }

        public GeneralCommandsExecutor          GenCommandsExecutor { get; private set; }
        public PictureCommandsExecutor          PicCommandsExecutor { get; private set; }

        public IHubProxy                        CommandsHubProxy    { get; private set; }
        public IHubProxy                        PicturesHubProxy    { get; private set; }
        public HubConnection                    HubConnection       { get; private set; }



        public ClientController(ClientMainWindow clientMainWindow)
        {
            _clientMainWindow = clientMainWindow;
            setClientMainWindowEventsHandlers();

            User                = new User();
            IsRegistered        = false;
            BroadcasterID       = null;
            
            GenCommandsExecutor = new GeneralCommandsExecutor(this);
            PicCommandsExecutor = new PictureCommandsExecutor(this);

            // For convenience - remove later.
            _clientMainWindow.UserNameTextBox.Text = "USER_NAME";
        }

        private void setClientMainWindowEventsHandlers()
        {
            _clientMainWindow.Closing                           += _clientMainWindow_Closing;
            _clientMainWindow.UserNameTextBox.TextChanged       += UserNameTextBox_TextChanged;
            _clientMainWindow.BroadcasterIdTextBox.TextChanged  += BroadcasterIdTextBox_TextChanged;

            _clientMainWindow.BroadcastButton.Click             += BroadcastButton_Click;
            _clientMainWindow.ReceiveButton.Click               += ReceiveButton_Click;
            _clientMainWindow.StopReceivingButton.Click         += StopReceivingButton_Click;
            _clientMainWindow.StopBroadcastingButton.Click      += StopBroadcastingButton_Click;
        }
        

        // Events handlers.
        private void _clientMainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (HubConnection != null)
            {
                HubConnection.Stop();
                HubConnection.Dispose();
            }
        }

        private void Connection_Closed()
        {
            // Show login UI; hide broadcast and receive UI.
            var dispatcher = Application.Current.Dispatcher;

            dispatcher.Invoke(() =>
            {
                activateBroadcastUI(false);
                activateReceiveUI(false);
                activateSignInUI(true);
            });
        }

        private async void BroadcastButton_Click(object sender, RoutedEventArgs e)
        {
            if (isUserNameValid())
            {
                User.ID = Guid.NewGuid();

                bool shouldRegister = false;
                if (HubConnection == null)
                {
                    shouldRegister = await ConnectAsync();
                }
                
                if (shouldRegister)
                {
                    var param = new JObject();
                    param["ID"] = User.ID;
                    param["Name"] = User.Name;

                    await CommandsHubProxy.Invoke(
                        "ExecuteCommand", ClientToServerGeneralCommand.RegisterNewBroadcaster, param);

                    if (IsRegistered)
                    {
                        activateSignInUI(false);
                        activateBroadcastUI(true);
                    }
                }
            }
        }
        private async void ReceiveButton_Click(object sender, RoutedEventArgs e)
        {
            if (isUserNameValid() && isBroadcasterIDValid())
            {
                User.ID = Guid.NewGuid();

                bool shouldRegister = false;
                if (HubConnection == null)
                {
                    shouldRegister = await ConnectAsync();
                }

                if (shouldRegister)
                {
                    var clientParam = new JObject();
                    clientParam["ID"] = User.ID;
                    clientParam["Name"] = User.Name;
                    clientParam["BroadcasterID"] = BroadcasterID;

                    await CommandsHubProxy.Invoke(
                        "ExecuteCommand", ClientToServerGeneralCommand.RegisterNewReceiver, clientParam);

                    if (IsRegistered)
                    {
                        activateSignInUI(false);
                        activateReceiveUI(true);
                    }
                }
            }
        }
        private async void StopReceivingButton_Click(object sender, RoutedEventArgs e)
        {
            var clientParam = new JObject();
            clientParam["ID"] = User.ID;
            clientParam["BroadcasterID"] = BroadcasterID;


            try
            {
                await CommandsHubProxy.Invoke(
                    "ExecuteCommand", ClientToServerGeneralCommand.StopReceiving, clientParam);
            }
            catch (HttpRequestException)
            {
                reportHttpRequestException();
            }
            catch (InvalidOperationException)
            {
                string text = "Unable to execute the command. Possibly, the server was stopped.";
                string caption = "Server connection error!";
                MessageBox.Show(text, caption);
            }

            HubConnection.Stop();
            HubConnection.Dispose();
        }
        private async void StopBroadcastingButton_Click(object sender, RoutedEventArgs e)
        {
            var clientParam = new JObject();
            clientParam["BroadcasterID"] = User.ID;

            try
            {
                await CommandsHubProxy.Invoke(
                    "ExecuteCommand", ClientToServerGeneralCommand.StopBroadcasting, clientParam);
            }
            catch (HttpRequestException)
            {
                reportHttpRequestException();
            }
            catch (InvalidOperationException)
            {
                string text = "Unable to execute the command. Possibly, the server was stopped.";
                string caption = "Server connection error!";
                MessageBox.Show(text, caption);
            }

            HubConnection.Stop();
            HubConnection.Dispose();
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
            HubConnection = new HubConnection(SERVER_URI);
            HubConnection.Closed += Connection_Closed;

            setupCommandsHub();
            setupPicturesHub();

            try
            {
                await HubConnection.Start();
            }
            catch (HttpRequestException)
            {
                reportHttpRequestException();
                return false;
            }

            //Show chat UI; hide login UI.
            return true;
        }

        private void setupCommandsHub()
        {
            CommandsHubProxy = HubConnection.CreateHubProxy("CommandsHub");

            CommandsHubProxy.On<ServerToClientGeneralCommand, JObject>(
                "ExecuteCommand", (command, serverParam) => GenCommandsExecutor.ExecuteCommand(command, serverParam));
        }
        private void setupPicturesHub()
        {
            PicturesHubProxy = HubConnection.CreateHubProxy("PicturesHub");

            var context = SynchronizationContext.Current;

            PicturesHubProxy.On<ServerToClientPictureCommand, JObject>(
                "ExecuteCommand", (command, serverParam) => context.Post(delegate { PicCommandsExecutor.ExecuteCommand(command, serverParam); }, null));
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

        private void reportHttpRequestException()
        {
            string text = "Unable to connect to server: Start server before connecting clients.";
            string caption = "Server connection error!";
            MessageBox.Show(text, caption);
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
                
                _clientMainWindow.LogRichTextBox.Document.Blocks.Clear();
                _clientMainWindow.ChatRichTextBox.Document.Blocks.Clear();
            }
            else
            {
                _clientMainWindow.BroadcastUI.Visibility = Visibility.Collapsed;
                _clientMainWindow.UserIDTextBox.Text = string.Empty;
            }

            activateChatUI(shouldActivate);
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

            activateChatUI(shouldActivate);
        }
        private void activateChatUI(bool shouldActivate)
        {
            if (shouldActivate)
            {
                _clientMainWindow.ChatUI.Visibility = Visibility.Visible;
            }
            else
            {
                _clientMainWindow.ChatUI.Visibility = Visibility.Collapsed;
            }
        }
    }
}
