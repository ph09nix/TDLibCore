using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TDLibCore.enums
{
    public enum AuhtorizationState
    {
        Ready,
        WaitingForVerificationCode,
        WaitingForVerificationPassword,
        LoggingOut,
        Closing,
        Closed,
        BackgroundActions,
        Failed,
        InvalidData
    }

    public enum ConnectionState
    {
        Connected,
        ConnectingToProxy,
        Updating,
        Connecting,
        WaitingForNetwork,
        InvalidData
    }

    public enum Response
    {
        Success,
        Failed,
        TimedOut,
        Processing
    }

    public enum DebugLevel
    {
        Full,
        Normal,
        LogOnly,
        None
    }
}