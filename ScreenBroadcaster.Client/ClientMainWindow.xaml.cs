﻿using System;
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
        /*Common UI*/
        public Grid CommonUI
        {
            get
            {
                return commonUI;
            }
        }

        public TextBox UserNameTextBox
        {
            get
            {
                return userNameTextBox;
            }
        }

        public TextBox UserIDTextBox
        {
            get
            { return userIDTextBox; }
        }

        /*Sign in UI*/
        public Grid SignInUI
        {
            get
            { return signInUI; }
        }

        public Button BroadcastButton
        {
            get
            { return broadcastButton; }
        }

        public Button ReceiveButton
        {
            get
            { return receiveButton; }
        }

        public TextBox BroadcasterIdTextBox
        {
            get
            { return broadcasterIdTextBox; }
        }

        /*Group Grid*/
        public Grid GroupGrid
        {
            get
            {
                return groupGrid;
            }
        }

        /*Broadcast UI*/
        public Grid BroadcastUI
        {
            get
            { return broadcastUI; }
        }

        public Button StopBroadcastingButton
        {
            get
            { return stopBroadcastButton; }
        }

        public RichTextBox ChatRichTextBox
        {
            get
            {
                return chatRichTextBox;
            }
        }

        public TextBox MessageTextBox
        {
            get
            {
                return messageTextBox;
            }
        }

        public Button SendMessageButton
        {
            get
            {
                return sendMessageButton;
            }
        }

        /*Receive UI*/
        public Grid ReceiveUI
        {
            get
            { return receiveUI; }
        }

        public Button StopReceivingButton
        {
            get
            { return stopReceivingButton; }
        }

        public RichTextBox LogRichTextBox
        {
            get
            {
                return logRichTextBox;
            }
        }

        public TextBox BroadcasterIDForReceiverTextBox
        {
            get
            { return broadcasterIDForReceiverTextBox; }
        }

        public Canvas RemoteScreenDisplay
        {
            get
            { return remoteScreenDisplay; }
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
