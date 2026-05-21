using System;
using System.Collections.Generic;

namespace Ens.Request
{
    namespace Client
    {
        public class SetInfo : RequestClient
        {
            protected internal override string Header => "R6";

            public static Action OnRecvReply;
            public static Action OnTimeOut;
            public static Action NotInRoomError;

            private static SetInfo Instance;
            internal SetInfo() : base()
            {
                Instance = this;
            }
            public static void SendRequest(Dictionary<string,string> pairs)//提供静态方法用于调用
            {
                if (pairs.Count == 0)
                {
                    Utils.Debug.LogError("设置了空的信息");
                    return;
                }
                Instance.SendRequest(Format.DictionaryToString(pairs));
            }
            protected override void Error(int code, string data)
            {
                NotInRoomError?.Invoke();
            }
            protected override void HandleReply(string data)
            {
                OnRecvReply?.Invoke();
            }
            protected internal override void TimeOut()
            {
                OnTimeOut?.Invoke();
            }
        }
    }
}