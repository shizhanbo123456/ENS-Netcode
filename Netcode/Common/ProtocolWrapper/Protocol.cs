using ProtocolWrapper.Protocols.Tcp;
using ProtocolWrapper.Protocols.Udp;
using System;
using System.Net;

namespace ProtocolWrapper
{
    public enum ConcurrentType
    {
        Multithreading, Asynchronous
    }
    public enum ProtocolType
    {
        TCP, UDP
    }
    public class Protocol
    {
        /// <summary>
        /// 标识，所有的Listener和Protocol的id都不一样
        /// </summary>
        internal static int id = 0;

        internal static Action<ProtocolBase> OnRecvConnection;
        internal static Action OnClientInitialized;

        public static ConcurrentType mode=ConcurrentType.Multithreading;
        public static ProtocolType defaultProtocol;
        public static Func<string, int, ProtocolBase> GetClientFunc;
        public static Func<IPAddress, int, ListenerBase> GetListenerFunc;


        /// <summary>
        /// 失败会返回null
        /// </summary>
        internal static ProtocolBase GetClient(string ip,int port)
        {
            if(GetClientFunc!=null)
            {
                return GetClientFunc(ip, port);
            }
            switch (defaultProtocol)
            {
                case ProtocolType.TCP: return TransportTcp.GetProtocolBase(ip,port);
                case ProtocolType.UDP: return TransportUdp.GetProtocolBase(ip, port);
            }
            Utils.Debug.LogError("未注册的协议");
            return null;
        }
        /// <summary>
        /// 失败会返回null
        /// </summary>
        internal static ListenerBase GetListener(IPAddress ip,int port)
        {
            if (GetListenerFunc != null)
            {
                return GetListenerFunc(ip,port);
            }
            switch (defaultProtocol)
            {
                case ProtocolType.TCP: return TransportTcp.GetListener(ip, port);
                case ProtocolType.UDP: return TransportUdp.GetListener(ip, port);
            }
            Utils.Debug.LogError("未注册的协议");
            return null;
        }
    }
}
