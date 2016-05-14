using System;
using System.Windows;
using System.Windows.Controls;
using ScreenBroadcaster.Server.Controllers;

namespace ScreenBroadcaster.Server
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class ServerMainWindow
        : Window
    {
        public ServerController ServerController    { get; private set; }

        public Button           StartButton         { get { return startButton; } }
        public Button           StopButton          { get { return stopButton; } }
        public RichTextBox      ConsoleRichTextBox  { get { return consoleRichTextBox; } }

        public ServerMainWindow()
        {
            InitializeComponent();
        }

        private void Window_Initialized(object sender, EventArgs e)
        {
            ServerController = new ServerController(this);
        }
    }
}
