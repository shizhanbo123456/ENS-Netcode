using System;

namespace Ens.Request
{
    namespace Server
    {
        internal class CreateRoom:RequestServer
        {
            protected internal override string Header => "R0";
            protected internal override string HandleRequest(EnsConnection conn, string data)
            {
                var b = EnsRoomManager.Instance.CreateRoom(conn, out int code);
                if (b) return code.ToString();
                else return ThrowError(code);
            }
        }
    }
}