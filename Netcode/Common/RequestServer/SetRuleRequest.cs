using System;
using System.Collections.Generic;

namespace Ens.Request
{
    namespace Server
    {
        internal class SetRule : RequestServer
        {
            protected internal override string Header => "R1";
            protected internal override string HandleRequest(EnsConnection conn, string data)
            {
                if (conn.room == null) return ThrowError(0);
                var info = Format.StringToDictionary(data, t => t, t => (t[0],int.Parse(t.Substring(1,t.Length-1))));
                foreach (var i in info.Keys)
                {
                    if (conn.room.Rule.ContainsKey(i)) conn.room.Rule[i] = info[i];
                    else conn.room.Rule.Add(i, info[i]);
                }
                return "empty";
            }
        }
    }
}