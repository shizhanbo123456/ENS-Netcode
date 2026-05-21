using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ens.Request
{
    namespace Server
    {
        internal class GetRoomList : RequestServer
        {
            protected internal override string Header => "R5";
            protected internal override string HandleRequest(EnsConnection conn, string data)
            {
                List<int>r=new List<int>();
                var rooms = EnsRoomManager.Instance.rooms.Keys.ToList();
                r.Add(rooms.Count);
                var s=data.Split('-');
                int start = int.Parse(s[0]);
                start=Math.Max(start,0);
                int end=int.Parse(s[1]);
                end=Math.Min(end,rooms.Count-1);
                for (int i = start; i <= end; i++) r.Add(rooms[i]);
                return Format.ListToString(r);
            }
        }
    }
}