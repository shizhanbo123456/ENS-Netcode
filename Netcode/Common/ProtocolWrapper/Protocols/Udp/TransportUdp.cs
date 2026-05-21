using System;
using System.Net;

namespace ProtocolWrapper.Protocols.Udp
{
    internal class TransportUdp
    {
        internal static ProtocolBase GetProtocolBase(string ip, int port)
        {
            ClientUdp udp;
            try
            {
                udp = new ClientUdp();
                udp.Init(ip, port);
            }
            catch (Exception e)
            {
                Utils.Debug.LogError(e.ToString());
                return null;
            }
            return udp;
        }
        internal static ListenerBase GetListener(IPAddress ip, int port)
        {
            ListenerUdp udp;
            try
            {
                udp = new ListenerUdp(ip, port);
            }
            catch (Exception e)
            {
                Utils.Debug.LogError(e.ToString());
                return null;
            }
            return udp;
        }
    }
}