using System;
using System.Collections.Generic;

namespace Ens.Request
{
    namespace Server
    {
        internal class GetRule : RequestServer
        {
            protected internal override string Header => "R3";
            protected internal override string HandleRequest(EnsConnection conn, string data)
            {
                List<int> ids = Format.StringToList(data,int.Parse);
                Dictionary<int,string>r=new Dictionary<int,string>();
                foreach(var i in ids)
                {
                    if (EnsRoomManager.Instance.rooms.TryGetValue(int.Parse(data), out var room))
                    {
                        r.Add(i, Format.DictionaryToString(room.Rule, valueconverter: t =>t.Item1.ToString()+t.Item2));
                    }
                }
                return Format.DictionaryToString(r);
            }
        }
    }
}