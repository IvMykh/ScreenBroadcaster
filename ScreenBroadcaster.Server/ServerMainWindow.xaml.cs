using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Owin.Hosting;
using ScreenBroadcaster.Server.Controllers;

namespace ScreenBroadcaster.Server
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class ServerMainWindow
        : Window
    {
        // Instance fields;
        private ServerController _serverController;

        // Properties.
        public ServerController ServerController
        {
            get
            {
                return _serverController;
            }
        }

        public Button StartButton
        {
            get
            {
                return startButton;
            }
        }

        public Button StopButton
        {
            get
            {
                return stopButton;
            }
        }

        public RichTextBox ConsoleRichTextBox
        {
            get
            {
                return consoleRichTextBox;
            }
        }

        // Methods.
        public ServerMainWindow()
        {
            InitializeComponent();
        }

        private void Window_Initialized(object sender, EventArgs e)
        {
            _serverController = new ServerController(this);
        }
    }
}
