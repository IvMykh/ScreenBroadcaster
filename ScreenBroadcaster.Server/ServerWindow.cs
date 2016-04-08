using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Owin.Hosting;

namespace ScreenBroadcaster.Server
{
    public partial class ServerWindow 
        : Form
    {
        // Static members.
        private const string    SERVER_URI = "http://localhost:8080";
        
        // Instance members.
        private IDisposable     SignalR { get; set; }

        public ServerWindow()
        {
            InitializeComponent();
        }

        public void WriteToConsole(string message)
        {
            if (richTextBoxConsole.InvokeRequired)
            {
                this.Invoke((Action)(() =>
                    {
                        WriteToConsole(message);
                    }));
                return;
            }
            richTextBoxConsole.AppendText(message + Environment.NewLine);
        }
        
        private void startServerButton_Click(object sender, EventArgs e)
        {
            WriteToConsole("Starting server...");
            startServerButton.Enabled = false;
            Task.Run(() => startServer());
        }

        private void startServer()
        {
            try
            {
                // https...
                SignalR = WebApp.Start(SERVER_URI);
            }
            catch (TargetInvocationException)
            {
                WriteToConsole(
                    string.Format("Server failed to start. A server is already running on {0}", SERVER_URI));

                this.Invoke((Action)(() => startServerButton.Enabled = true));
                return;
            }

            this.Invoke((Action)(() => startServerButton.Enabled = true));
            WriteToConsole(
                   string.Format("Server started at {0}", SERVER_URI));

        }

        private void stopServerButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void ServerWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (SignalR != null)
            {
                SignalR.Dispose();
            }
        }
    }
}
