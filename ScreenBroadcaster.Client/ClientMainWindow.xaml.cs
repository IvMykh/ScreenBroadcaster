using System;
using System.Windows;
using System.Net.Http;
using Microsoft.AspNet.SignalR.Client;
using System.Windows.Controls;

using ScreenBroadcaster.Client.Controllers;

namespace ScreenBroadcaster.Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class ClientMainWindow 
        : Window
    {
        // Instance fields;
        private ClientController _clientController;

        // Properties;
        public Button       SignInButton 
        {
            get { return signInButton; }
        }
        public Button       SendAllButton 
        {
            get { return sendAllButton; }
        }
        public Button       StartStopGenButton
        {
            get { return startStopGenButton; }
        }
        public StackPanel   ChatPanel 
        { 
            get { return chatPanel; } 
        }
        public StackPanel   SignInPanel 
        { 
            get { return signInPanel; }
        }
        public TextBox      MessageTextBox 
        { 
            get { return messageTextBox; } 
        }
        public RichTextBox  ConsoleRichTextBox 
        { 
            get { return consoleRichTextBox; } 
        }
        public Label        StatusText 
        { 
            get { return statusText; } 
        }
        public Label        NumDisplayLabel
        {
            get { return numDisplayLabel; }
        }

        // Methods.
        public ClientMainWindow()
        {
            InitializeComponent();
        }

        private void Window_Initialized(object sender, EventArgs e)
        {
            _clientController = new ClientController(this);
        }
    }
}
