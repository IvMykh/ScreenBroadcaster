using System;
using System.Configuration;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.AspNet.SignalR.Client;
using Newtonsoft.Json.Linq;
using ScreenBroadcaster.Client.Controllers.Helpers;
using ScreenBroadcaster.Client.Properties;
using ScreenBroadcaster.Common;
using ScreenBroadcaster.Common.CommandTypes;
using System.Drawing;

namespace ScreenBroadcaster.Client.Controllers
{
    public partial class ClientController
    {
        internal partial class GeneralCommandsExecutor { }
        internal partial class PictureSender: IDisposable { }

        // Instanse props.
        public ClientMainWindow         MainWindow          { get; private set; }
        internal GuiHelper              MyGuiHelper         { get; private set; }

        public User                     User                { get; private set; }
        public bool                     IsRegistered        { get; private set; }
        public Guid?                    BroadcasterID       { get; private set; }

        internal GeneralCommandsExecutor GenCommandsExecutor { get; private set; }
        internal PictureCommandsExecutor PicCommandsExecutor { get; private set; }

        public HubConnection            HubConnection       { get; private set; }
        public IHubProxy                CommandsHubProxy    { get; private set; }
        public IHubProxy                PicturesHubProxy    { get; private set; }

        public bool                     GetFullImage        { get; set; }
        public Bitmap                   FullImage           { get; set; }


        public ClientController(ClientMainWindow clientMainWindow)
        {
            MainWindow = clientMainWindow;
            MyGuiHelper = new GuiHelper(this);
            setClientMainWindowEventsHandlers();

            User                = new User();
            IsRegistered        = false;
            BroadcasterID       = null;

            GetFullImage        = true;
            FullImage           = new Bitmap((int)SystemParameters.PrimaryScreenWidth,
                                             (int)SystemParameters.PrimaryScreenHeight);

            using (var graphics = Graphics.FromImage(FullImage))
            {
                var sourceUpLeftPoint = new System.Drawing.Point(0, 0);
                var destUpLeftPoint = new System.Drawing.Point(0, 0);
                graphics.CopyFromScreen(sourceUpLeftPoint, destUpLeftPoint, FullImage.Size, CopyPixelOperation.SourceCopy);
            }

            GenCommandsExecutor = new GeneralCommandsExecutor(this);
            PicCommandsExecutor = new PictureCommandsExecutor(this);

#if DEBUG
            MainWindow.UserNameTextBox.Text = Resources.DefaultUserName;
#endif
        }

        private void setClientMainWindowEventsHandlers()
        {
            MainWindow.Closing                          += mainWindow_Closing;
            MainWindow.UserNameTextBox.TextChanged      += UserNameTextBox_TextChanged;
            MainWindow.BroadcasterIdTextBox.TextChanged += BroadcasterIdTextBox_TextChanged;

            MainWindow.BroadcastButton.Click            += BroadcastButton_Click;
            MainWindow.ReceiveButton.Click              += ReceiveButton_Click;
            MainWindow.StopReceivingButton.Click        += StopReceivingButton_Click;
            MainWindow.StopBroadcastingButton.Click     += StopBroadcastingButton_Click;
            MainWindow.SendMessageButton.Click          += ButtonSend_Click;
        }
        
        // Events handlers.
        private void mainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (HubConnection != null)
            {
                disconnectFromServer();
            }
        }

        private void Connection_Closed()
        {
            var dispatcher = Application.Current.Dispatcher;
            dispatcher.Invoke(() =>
                {
                    MyGuiHelper.ActivateUI(Ui.SignInUi);
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
                    MyGuiHelper.EnableBcastRecButtons(false);
                    shouldRegister = await ConnectAsync();
                    MyGuiHelper.EnableBcastRecButtons(true);
                }
                
                if (shouldRegister)
                {
                    var param = new JObject();
                    param["ID"] = User.ID;
                    param["Name"] = User.Name;

                    await executeSafe(ClientToServerGeneralCommand.RegisterNewBroadcaster, param);

                    if (IsRegistered)
                    {
                        MyGuiHelper.ActivateUI(Ui.BroadcastUi);
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
                    MyGuiHelper.EnableBcastRecButtons(false);
                    shouldRegister = await ConnectAsync();
                    MyGuiHelper.EnableBcastRecButtons(true);
                }

                if (shouldRegister)
                {
                    var clientParam = new JObject();
                    clientParam["ID"] = User.ID;
                    clientParam["Name"] = User.Name;
                    clientParam["BroadcasterID"] = BroadcasterID;

                    await executeSafe(ClientToServerGeneralCommand.RegisterNewReceiver, clientParam);

                    if (IsRegistered)
                    {
                        MyGuiHelper.ActivateUI(Ui.ReceiveUi);
                    }
                    else
                    {
                        disconnectFromServer();
                    }
                }
            }
        }

        private async void StopBroadcastingButton_Click(object sender, RoutedEventArgs e)
        {
            var clientParam = new JObject();
            clientParam["BroadcasterID"] = User.ID;

            GenCommandsExecutor.StopBroadcast();
            await executeSafe(ClientToServerGeneralCommand.StopBroadcasting, clientParam);
            disconnectFromServer();
        }
        private async void StopReceivingButton_Click(object sender, RoutedEventArgs e)
        {
            var clientParam = new JObject();
            clientParam["ID"] = User.ID;
            clientParam["BroadcasterID"] = BroadcasterID;

            await executeSafe(ClientToServerGeneralCommand.StopReceiving, clientParam);
            disconnectFromServer();
        }
        
        private async void ButtonSend_Click(object sender, RoutedEventArgs e)
        {
            var clientParam = new JObject();
            clientParam["ID"] = User.ID;
            clientParam["Name"] = User.Name;
            clientParam["Message"] = MainWindow.MessageTextBox.Text;
            clientParam["BroadcasterID"] = (BroadcasterID == null) ? Guid.Empty : BroadcasterID;


            await executeSafe(ClientToServerGeneralCommand.SendMessage, clientParam);

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
            string serverUri = ConfigurationManager.AppSettings["ServerUri"];

            HubConnection = new HubConnection(serverUri);
            HubConnection.Closed += Connection_Closed;

            setupCommandsHub();
            setupPicturesHub();

            return await executeSafe(async () => { await HubConnection.Start(); });
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

        private async Task<bool> executeSafe(ClientToServerGeneralCommand command, JObject param)
        {
            return await executeSafe(async () => 
                {
                    await CommandsHubProxy.Invoke("ExecuteCommand", command, param);
                }
            );
        }
        private async Task<bool> executeSafe(Func<Task> action)
        {
            try
            {
                await action();
            }
            catch (HttpRequestException)
            {
                disconnectFromServer();

                MsgReporter.Instance.ReportError(
                    Resources.HttpRequestExcMsg, Resources.ServerConnErrorCaption);

                return false;
            }
            catch (InvalidOperationException)
            {
                disconnectFromServer();

                MsgReporter.Instance.ReportError(
                    Resources.OperationFailedMsg, Resources.ServerConnErrorCaption);

                return false;
            }

            return true;
        }

        private void disconnectFromServer()
        {
            if (HubConnection != null)
            {
                HubConnection.Dispose();
                HubConnection = null;
            }
        }
    }
}
