using System;
using System.Collections.Generic;
using System.Text;

namespace Ens.Request
{
    namespace Client
    {
        public class JoinRoom : RequestClient
        {
            protected internal override string Header => "R2";

            public static Action OnRecvReply;
            public static Action OnTimeOut;
            public static Action OnRoomNotFoundError;
            public static Action AlreadyInRoomError;

            private static JoinRoom Instance;
            internal JoinRoom() : base()
            {
                Instance = this;
            }
            public static void SendRequest(int id)//提供静态方法用于调用
            {
                Instance.SendRequest(id.ToString());
            }
            protected override void Error(int code, string data)
            {
                if (code == 0) OnRoomNotFoundError?.Invoke();
                else AlreadyInRoomError?.Invoke();
            }
            protected override void HandleReply(string data)
            {
                EnsInstance.PresentRoomId=int.Parse(data);
                OnRecvReply?.Invoke();
            }
            protected internal override void TimeOut()
            {
                OnTimeOut?.Invoke();
            }
        }
    }
}