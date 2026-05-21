using ProtocolWrapper.Protocols.Udp;
using System;
using System.Net;

namespace Ens.DefaultProtocol
{
    public class ProtocolUdp : ProtocolBase
    {
        public override ProtocolWrapper.ProtocolBase GetProtocolBase(string ip, int port)
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
        public override ProtocolWrapper.ListenerBase GetListener(IPAddress ip, int port)
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