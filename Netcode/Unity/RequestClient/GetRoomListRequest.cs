using System;
using System.Collections.Generic;

namespace Ens.Request
{
    namespace Client
    {
        public class GetRoomList : RequestClient
        {
            protected internal override string Header => "R5";

            public static Action<int,List<int>> OnRecvReply;
            public static Action OnTimeOut;

            private static GetRoomList Instance;
            internal GetRoomList() : base()
            {
                Instance = this;
            }
            public static void SendRequest(int startIndex,int endIndex)//提供静态方法用于调用
            {
                Instance.SendRequest(startIndex+"-"+endIndex);
            }
            protected override void Error(int code, string data)
            {
                
            }
            protected override void HandleReply(string data)
            {
                var List = Format.StringToList(data, int.Parse);
                var count = List[0];
                List.RemoveAt(0);
                OnRecvReply?.Invoke(count,List);
            }
            protected internal override void TimeOut()
            {
                OnTimeOut?.Invoke();
            }
        }
    }
}