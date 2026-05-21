#if UNITY_WEBGL
using ProtocolWrapper.Protocols.WeChatUdp;
#endif
using System;
using System.Net;
using UnityEngine;
#if UNITY_WEBGL
using WeChatWASM;
#endif

namespace Ens.DefaultProtocol
{
    public class ProtocolWxUdp : ProtocolBase
    {
        private void Start()
        {
#if UNITY_WEBGL
            WX.InitSDK(_=>Debug.Log("初始化完成"));
#endif
        }
        public override ProtocolWrapper.ProtocolBase GetProtocolBase(string ip, int port)
        {
#if UNITY_WEBGL
            ClientWeChatUdp client;
            try
            {
                client = new ClientWeChatUdp();
                client.Init(ip, port);
            }
            catch (Exception e)
            {
                Debug.LogError($"[WeChatUdp] 创建客户端失败：{e.ToString()}");
                return null;
            }
            return client;
#else
             throw new System.NotImplementedException();
#endif
        }
        public override ProtocolWrapper.ListenerBase GetListener(IPAddress ip, int port)=>
            throw new NotImplementedException("不支持Udp服务器");
    }
}