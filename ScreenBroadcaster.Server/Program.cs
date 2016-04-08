using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ScreenBroadcaster.Server
{
    static class Program
    {
        internal static ServerWindow MainServerWindow { get; set; }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            MainServerWindow = new ServerWindow();
            Application.Run(MainServerWindow);
        }
    }
}
