using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin.Cors;
using Owin;

namespace ScreenBroadcaster.Server
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // https configuration.

            GlobalHost.Configuration.DefaultMessageBufferSize = 5;

            app.UseCors(CorsOptions.AllowAll);
            app.MapSignalR();
        }
    }
}
