using System;
using System.Collections.Generic;

namespace Ens.Request
{
    namespace Client
    {
        public class GetRule : RequestClient
        {
            protected internal override string Header => "R3";

            public static Action<Dictionary<int,string>> OnRecvReply;
            public static Action OnTimeOut;
            public static Action RoomNotFoundError;

            private static GetRule Instance;
            internal GetRule() : base()
            {
                Instance = this;
            }
            public static void SendRequest(List<int>ids)//提供静态方法用于调用
            {
                if (ids.Count == 0)
                {
                    Utils.Debug.LogError("请求目标为空");
                    return;
                }
                Instance.SendRequest(Format.ListToString(ids));
            }
            protected override void Error(int code, string data)
            {
                RoomNotFoundError?.Invoke();
            }
            protected override void HandleReply(string data)
            {
                var d=Format.StringToDictionary(data, int.Parse,t=>t);
                OnRecvReply?.Invoke(d);
            }
            protected internal override void TimeOut()
            {
                OnTimeOut?.Invoke();
            }
        }
    }
}