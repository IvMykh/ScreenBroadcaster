using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.AspNet.SignalR.Client;
using Newtonsoft.Json.Linq;
using ScreenBroadcaster.Client.Properties;
using ScreenBroadcaster.Common;
using ScreenBroadcaster.Common.CommandTypes;

// TODO: make server notify clients about its stopping.

namespace ScreenBroadcaster.Client.Controllers
{
    public partial class ClientController
        //: IDisposable
    {
        public partial class GeneralCommandsExecutor { }
        public partial class PictureSender
            : IDisposable { }

        // Constants.
        private const string            SERVER_URI = "http://localhost:8080/signalr";

        // Instanse props.
        public ClientMainWindow         MainWindow          { get; private set; }

        public User                     User                { get; private set; }
        public bool                     IsRegistered        { get; private set; }
        public Guid?                    BroadcasterID       { get; private set; }

        public GeneralCommandsExecutor  GenCommandsExecutor { get; private set; }
        public PictureCommandsExecutor  PicCommandsExecutor { get; private set; }

        public HubConnection            HubConnection       { get; private set; }
        public IHubProxy                CommandsHubProxy    { get; private set; }
        public IHubProxy                PicturesHubProxy    { get; private set; }

        //public bool                     Disposed { get; private set; }

        public ClientController(ClientMainWindow clientMainWindow)
        {
            MainWindow = clientMainWindow;
            setClientMainWindowEventsHandlers();

            User                = new User();
            IsRegistered        = false;
            BroadcasterID       = null;
            
            GenCommandsExecutor = new GeneralCommandsExecutor(this);
            PicCommandsExecutor = new PictureCommandsExecutor(this);

            //Disposed            = false;

#if DEBUG
            MainWindow.UserNameTextBox.Text = Resources.DefaultUserName;
#endif
        }

        //~ClientController()
        //{
        //    cleanUp(false);
        //}
        //
        //public void Dispose()
        //{
        //    cleanUp(true);
        //    GC.SuppressFinalize(this);
        //}
        //
        //private void cleanUp(bool disposing)
        //{
        //    if (!Disposed)
        //    {
        //        if (disposing)
        //        {
        //            HubConnection.Dispose();
        //        }
        //        HubConnection = null;
        //    }
        //
        //    Disposed = true;
        //}

        private void setClientMainWindowEventsHandlers()
        {
            MainWindow.Closing                          += mainWindow_Closing;
            MainWindow.UserNameTextBox.TextChanged      += UserNameTextBox_TextChanged;
            MainWindow.BroadcasterIdTextBox.TextChanged += BroadcasterIdTextBox_TextChanged;

            MainWindow.BroadcastButton.Click        += BroadcastButton_Click;
            MainWindow.ReceiveButton.Click          += ReceiveButton_Click;
            MainWindow.StopReceivingButton.Click    += StopReceivingButton_Click;
            MainWindow.StopBroadcastingButton.Click += StopBroadcastingButton_Click;
            MainWindow.SendMessageButton.Click      += ButtonSend_Click;
        }
        
        // Events handlers.
        private void mainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (HubConnection != null)
            {
                HubConnection.Stop();
            }
        }

        private void Connection_Closed()
        {
            var dispatcher = Application.Current.Dispatcher;
            dispatcher.Invoke(() =>
                {
                    activateBroadcastUI(false);
                    activateReceiveUI(false);
                    activateSignInUI(true);
                }
            );
        }

        private async void BroadcastButton_Click(object sender, RoutedEventArgs e)
        {
            if (isUserNameValid())
            {
                User.ID = Guid.NewGuid();

                bool shouldRegister = false;
                if (HubConnection == null)
                {
                    enableBcastRecButtons(false);
                    shouldRegister = await ConnectAsync();
                    enableBcastRecButtons(true);
                }
                
                if (shouldRegister)
                {
                    var param = new JObject();
                    param["ID"] = User.ID;
                    param["Name"] = User.Name;

                    try
                    {
                        await CommandsHubProxy.Invoke(
                            "ExecuteCommand", ClientToServerGeneralCommand.RegisterNewBroadcaster, param);
                    }
                    catch (InvalidOperationException)
                    {
                        HubConnection.Stop();
                        HubConnection = null;

                        MsgReporter.Instance.ReportError(
                            Resources.OperationFailedMsg, Resources.ServerConnErrorCaption);
                    }

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
                    enableBcastRecButtons(false);
                    shouldRegister = await ConnectAsync();
                    enableBcastRecButtons(true);
                }

                if (shouldRegister)
                {
                    var clientParam = new JObject();
                    clientParam["ID"] = User.ID;
                    clientParam["Name"] = User.Name;
                    clientParam["BroadcasterID"] = BroadcasterID;

                    try
                    {
                        await CommandsHubProxy.Invoke(
                            "ExecuteCommand", ClientToServerGeneralCommand.RegisterNewReceiver, clientParam);
                    }
                    catch (InvalidOperationException)
                    {
                        HubConnection.Stop();
                        HubConnection = null;

                        MsgReporter.Instance.ReportError(
                            Resources.OperationFailedMsg, Resources.ServerConnErrorCaption);
                    }

                    if (IsRegistered)
                    {
                        activateSignInUI(false);
                        activateReceiveUI(true);
                    }
                    else
                    {
                        HubConnection.Dispose();
                        HubConnection = null;
                    }
                }
            }
        }

        private async void StopBroadcastingButton_Click(object sender, RoutedEventArgs e)
        {
            var clientParam = new JObject();
            clientParam["BroadcasterID"] = User.ID;

            try
            {
                GenCommandsExecutor.StopBroadcast();

                await CommandsHubProxy.Invoke(
                    "ExecuteCommand", ClientToServerGeneralCommand.StopBroadcasting, clientParam);
            }
            catch (HttpRequestException)
            {
                reportHttpRequestException();
            }
            catch (InvalidOperationException)
            {
                MsgReporter.Instance.ReportError(
                    Resources.ServerStoppedMsg, Resources.ServerConnErrorCaption);
            }

            HubConnection.Dispose();
            HubConnection = null;
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
                MsgReporter.Instance.ReportError(
                    Resources.ServerStoppedMsg, Resources.ServerConnErrorCaption);
            }

            HubConnection.Dispose();
            HubConnection = null;
        }
        
        private async void ButtonSend_Click(object sender, RoutedEventArgs e)
        {
            var clientParam = new JObject();
            clientParam["ID"] = User.ID;
            clientParam["Name"] = User.Name;
            clientParam["Message"] = MainWindow.MessageTextBox.Text;
            clientParam["BroadcasterID"] = (BroadcasterID == null) ? Guid.Empty : BroadcasterID;

            await CommandsHubProxy.Invoke(
                "ExecuteCommand", ClientToServerGeneralCommand.SendMessage, clientParam);

            MainWindow.MessageTextBox.Text = String.Empty;
            MainWindow.MessageTextBox.Focus();
        }

        private void UserNameTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            User.Name = MainWindow.UserNameTextBox.Text;
        }
        private void BroadcasterIdTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            Guid broadcasterID = default(Guid);
            if (Guid.TryParse(MainWindow.BroadcasterIdTextBox.Text, out broadcasterID))
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
                HubConnection = null;
                reportHttpRequestException();
                return false;
            }

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

            PicturesHubProxy.On<ServerToClientPictureCommand, JObject>(
                "ExecuteCommand", (command, serverParam) => PicCommandsExecutor.ExecuteCommand(command, serverParam));
        }

        private bool isUserNameValid()
        {
            if (!string.IsNullOrEmpty(User.Name))
            {
                return true;
            }
            else
            {
                MsgReporter.Instance.ReportError(
                    Resources.UserNameErrorMsg, Resources.UserNameErrorCaption);
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
                MsgReporter.Instance.ReportError(
                    Resources.BcasterIdErrorMsg, Resources.BcasterIdErrorCaption);
                return false;
            }
        }

        private void reportHttpRequestException()
        {
            MsgReporter.Instance.ReportError(
                Resources.HttpRequestExcMsg, Resources.ServerConnErrorCaption);
        }

        private void activateSignInUI(bool shouldActivate)
        {
            if (shouldActivate)
            {
                MainWindow.SignInUI.Visibility = Visibility.Visible;
                MainWindow.UserNameTextBox.IsReadOnly = false;
            }
            else
            {
                MainWindow.SignInUI.Visibility = Visibility.Collapsed;
                MainWindow.UserNameTextBox.IsReadOnly = true;
            }
        }
        private void activateBroadcastUI(bool shouldActivate)
        {
            if (shouldActivate)
            {
                MainWindow.BroadcastUI.Visibility = Visibility.Visible;
                MainWindow.UserIDTextBox.Text = User.ID.ToString();

                MainWindow.LogRichTextBox.Document.Blocks.Clear();
            }
            else
            {
                MainWindow.BroadcastUI.Visibility = Visibility.Collapsed;
                MainWindow.UserIDTextBox.Text = string.Empty;
            }

            activateChatUI(shouldActivate);
        }
        private void activateReceiveUI(bool shouldActivate)
        {
            if (shouldActivate)
            {
                MainWindow.ReceiveUI.Visibility = Visibility.Visible;
                MainWindow.UserIDTextBox.Text = User.ID.ToString();
                MainWindow.BroadcasterIDForReceiverTextBox.Text = BroadcasterID.Value.ToString();
            }
            else
            {
                MainWindow.ReceiveUI.Visibility = Visibility.Collapsed;
                MainWindow.UserIDTextBox.Text = string.Empty;
                MainWindow.BroadcasterIDForReceiverTextBox.Text = null;
            }

            activateChatUI(shouldActivate);
        }
        private void activateChatUI(bool shouldActivate)
        {
            if (shouldActivate)
            {
                MainWindow.ChatUI.Visibility = Visibility.Visible;
                MainWindow.ChatRichTextBox.Document.Blocks.Clear();
            }
            else
            {
                MainWindow.ChatUI.Visibility = Visibility.Collapsed;
            }
        }
        
        private void enableBcastRecButtons(bool shouldEnable)
        {
            MainWindow.BroadcastButton.IsEnabled = shouldEnable;
            MainWindow.ReceiveButton.IsEnabled = shouldEnable;
        }
    }
}
