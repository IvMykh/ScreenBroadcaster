using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.AspNet.SignalR;
using Newtonsoft.Json.Linq;
using ScreenBroadcaster.Server.Properties;

namespace ScreenBroadcaster.Server.Hubs
{
    public abstract class AbstrHub<CommandType>
        : Hub 
        where CommandType : struct, IConvertible
    {
        // Static members.
        protected static HubData Data { get; private set; }

        static AbstrHub()
        {
            Data = HubData.Instance;
        }

        // Instance members.
        private IDictionary<CommandType, Action<JObject>> _handlers;

        public AbstrHub()
        {
            _handlers = setupHandlers();
        }

        protected abstract IDictionary<CommandType, Action<JObject>> setupHandlers();
        
        public void ExecuteCommand(CommandType command, JObject argument)
        {
            _handlers[command](argument);
        }

        public override Task OnConnected()
        {
            //Use Application.Current.Dispatcher to access UI thread from outside the MainWindow class
            Application.Current.Dispatcher.Invoke(() =>
                {
                    var window = Application.Current.MainWindow as ServerMainWindow;
                    window.ServerController.WriteToConsole(
                        string.Format(Resources.ClientConnectedMsgFormat, Context.ConnectionId));
                }
            );

            return base.OnConnected();
        }
        public override Task OnDisconnected()
        {
            //Use Application.Current.Dispatcher to access UI thread from outside the MainWindow class
            Application.Current.Dispatcher.Invoke(() =>
                {
                    var window = Application.Current.MainWindow as ServerMainWindow;
                    window.ServerController.WriteToConsole(
                        string.Format(Resources.ClientDisconnectedMsgFormat, Context.ConnectionId));
                }
            );

            return base.OnDisconnected();
        }
    }
}
