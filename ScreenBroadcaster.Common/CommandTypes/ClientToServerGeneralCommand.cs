using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenBroadcaster.Common.CommandTypes
{
    public enum ClientToServerGeneralCommand
    {
        // general
        RegisterNewBroadcaster,
        RegisterNewReceiver,
        StopReceiving,
        StopBroadcasting,
        SendMessage,

        SetNewGenerationFreq
    }
}
