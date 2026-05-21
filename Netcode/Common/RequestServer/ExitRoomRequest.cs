using System;

namespace Ens.Request
{
    namespace Server
    {
        internal class ExitRoom : RequestServer
        {
            protected internal override string Header => "R4";
            protected internal override string HandleRequest(EnsConnection conn, string data)
            {
                var b=EnsRoomManager.Instance.ExitRoom(conn, out int code);
                if(b)return "empty";
                return ThrowError(code);
            }
        }
    }
}