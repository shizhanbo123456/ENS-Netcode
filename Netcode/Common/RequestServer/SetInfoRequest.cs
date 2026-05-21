using System;
using System.Collections.Generic;

namespace Ens.Request
{
    namespace Server
    {
        internal class SetInfo : RequestServer
        {
            protected internal override string Header => "R6";
            protected internal override string HandleRequest(EnsConnection conn, string data)
            {
                if (conn.room == null) return ThrowError(0);
                var info=Format.StringToDictionary(data, t => t, t => t);
                foreach(var i in info.Keys)
                {
                    if (conn.room.Info.ContainsKey(i))conn.room.Info[i] = info[i];
                    else conn.room.Info.Add(i, info[i]);
                }
                return "empty";
            }
        }
    }
}