using System;
using System.Collections.Generic;

namespace Ens.Request
{
    namespace Server
    {
        internal class GetInfo : RequestServer
        {
            protected internal override string Header => "R7";
            protected internal override string HandleRequest(EnsConnection conn, string data)
            {
                List<int> ids = Format.StringToList(data, int.Parse);
                Dictionary<int, string> r = new Dictionary<int, string>();
                foreach (var i in ids)
                {
                    if (EnsRoomManager.Instance.rooms.TryGetValue(i, out var room))
                    {
                        r.Add(i, Format.DictionaryToString(room.Info));
                    }
                }
                return Format.DictionaryToString(r);
            }
        }
    }
}
