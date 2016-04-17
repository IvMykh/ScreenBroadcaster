using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenBroadcaster.Common.CommandTypes
{
    public enum ServerToClientGeneralCommand
    {
        ReportSuccessfulRegistration,
        ReportFailedRegistration,
        NotifyReceiverStateChange,
        NotifyStopReceiving,
        NotifyStopBroadcasting,
        ForceStopReceiving
    }
}
