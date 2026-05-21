using System;

namespace Ens.Request
{
    namespace Client
    {
        public class CreateRoom:RequestClient
        {
            protected internal override string Header => "R0";

            public static Action OnCreateRoom;
            public static Action OnTimeOut;
            public static Action AlreadyInRoomException;

            private static CreateRoom Instance;
            internal CreateRoom():base()
            {
                Instance = this;
            }
            public static void SendRequest()//提供静态方法用于调用
            {
                Instance.SendRequest("empty");
            }
            protected override void Error(int code, string data)
            {
                AlreadyInRoomException?.Invoke();
            }
            protected override void HandleReply(string data)
            {
                EnsInstance.PresentRoomId = int.Parse(data);
                OnCreateRoom?.Invoke();
            }
            protected internal override void TimeOut()
            {
                OnTimeOut?.Invoke();
            }
        }
    }
}