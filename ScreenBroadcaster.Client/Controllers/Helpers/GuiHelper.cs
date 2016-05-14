using System;
using System.Windows;

namespace ScreenBroadcaster.Client.Controllers.Helpers
{
    internal enum Ui
    {
        SignInUi,
        BroadcastUi,
        ReceiveUi
    }

    internal class GuiHelper
    {
        protected ClientController ClientController { get; private set; }

        public GuiHelper(ClientController clientController)
        {
            ClientController = clientController;
        }

        private void activateSignInUI(bool shouldActivate)
        {
            if (shouldActivate)
            {
                ClientController.MainWindow.SignInUI.Visibility = Visibility.Visible;
                ClientController.MainWindow.UserNameTextBox.IsReadOnly = false;
            }
            else
            {
                ClientController.MainWindow.SignInUI.Visibility = Visibility.Collapsed;
                ClientController.MainWindow.UserNameTextBox.IsReadOnly = true;
            }
        }
        private void activateBroadcastUI(bool shouldActivate)
        {
            if (shouldActivate)
            {
                ClientController.MainWindow.BroadcastUI.Visibility = Visibility.Visible;
                ClientController.MainWindow.UserIDTextBox.Text = ClientController.User.ID.ToString();

                ClientController.MainWindow.LogRichTextBox.Document.Blocks.Clear();
            }
            else
            {
                ClientController.MainWindow.BroadcastUI.Visibility = Visibility.Collapsed;
                ClientController.MainWindow.UserIDTextBox.Text = string.Empty;
            }

            activateChatUI(shouldActivate);
        }
        private void activateReceiveUI(bool shouldActivate)
        {
            if (shouldActivate)
            {
                ClientController.MainWindow.ReceiveUI.Visibility = Visibility.Visible;
                ClientController.MainWindow.UserIDTextBox.Text = ClientController.User.ID.ToString();
                ClientController.MainWindow.BroadcasterIDForReceiverTextBox.Text = ClientController.BroadcasterID.Value.ToString();
            }
            else
            {
                ClientController.MainWindow.ReceiveUI.Visibility = Visibility.Collapsed;
                ClientController.MainWindow.UserIDTextBox.Text = string.Empty;
                ClientController.MainWindow.BroadcasterIDForReceiverTextBox.Text = null;
            }

            activateChatUI(shouldActivate);
        }
        private void activateChatUI(bool shouldActivate)
        {
            if (shouldActivate)
            {
                ClientController.MainWindow.ChatUI.Visibility = Visibility.Visible;
                ClientController.MainWindow.ChatRichTextBox.Document.Blocks.Clear();
            }
            else
            {
                ClientController.MainWindow.ChatUI.Visibility = Visibility.Collapsed;
            }
        }

        public void ActivateUI(Ui ui)
        {
            switch (ui)
            {
                case Ui.SignInUi:
                    {
                        activateBroadcastUI(false);
                        activateReceiveUI(false);
                        activateSignInUI(true);
                    } break;

                case Ui.BroadcastUi:
                    {
                        activateSignInUI(false);
                        activateReceiveUI(false);
                        activateBroadcastUI(true);
                    } break;

                case Ui.ReceiveUi:
                    {
                        activateSignInUI(false);
                        activateBroadcastUI(false);
                        activateReceiveUI(true);
                    } break;

                default: throw new NotImplementedException();
            }
        }
        public void EnableBcastRecButtons(bool shouldEnable)
        {
            ClientController.MainWindow.BroadcastButton.IsEnabled = shouldEnable;
            ClientController.MainWindow.ReceiveButton.IsEnabled = shouldEnable;
        }
    }
}
