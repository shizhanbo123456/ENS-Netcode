using System;
using System.Collections.Generic;
using System.Text;

namespace Ens.Request
{
    namespace Server
    {
        internal class JoinRoom : RequestServer
        {
            protected internal override string Header => "R2";
            protected internal override string HandleRequest(EnsConnection conn, string data)
            {
                if (conn.room != null) return ThrowError(1);
                int id = int.Parse(data);
                var b=EnsRoomManager.Instance.JoinRoom(conn,id,out var code);
                if(b)return code.ToString();
                else return ThrowError(code);
            }
        }
    }
}