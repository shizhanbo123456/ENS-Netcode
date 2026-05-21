using System;

namespace Ens.Request
{
    namespace Client
    {
        public class ExitRoom : RequestClient
        {
            protected internal override string Header => "R4";

            public static Action OnRecvReply;
            public static Action OnTimeOut;
            public static Action NotInRoomException;

            private static ExitRoom Instance;
            internal ExitRoom() : base()
            {
                Instance = this;
            }
            public static void SendRequest()//提供静态方法用于调用
            {
                Instance.SendRequest("empty");
            }
            protected override void Error(int code, string data)
            {
                NotInRoomException?.Invoke();
            }
            protected override void HandleReply(string data)
            {
                EnsInstance.PresentRoomId = 0;
                OnRecvReply?.Invoke();
            }
            protected internal override void TimeOut()
            {
                OnTimeOut?.Invoke();
            }
        }
    }
}