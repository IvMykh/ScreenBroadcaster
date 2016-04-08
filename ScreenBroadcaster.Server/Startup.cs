using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin.Cors;
using Owin;

namespace ScreenBroadcaster.Server
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // http configuration.

            app.UseCors(CorsOptions.AllowAll);
            app.MapSignalR();
        }
    }
}
