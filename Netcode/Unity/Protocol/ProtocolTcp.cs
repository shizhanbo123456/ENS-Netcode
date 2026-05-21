using ProtocolWrapper.Protocols.Tcp;
using System;
using System.Net;

namespace Ens.DefaultProtocol
{
    public class ProtocolTcp : ProtocolBase
    {
        public override ProtocolWrapper.ProtocolBase GetProtocolBase(string ip, int port)
        {
            ClientTcp tcp = null;
            try
            {
                tcp = new ClientTcp();
                tcp.Init(ip, port);
            }
            catch (Exception e)
            {
                Utils.Debug.LogError(e.ToString());
                return null;
            }
            return tcp;
        }
        public override ProtocolWrapper.ListenerBase GetListener(IPAddress ip, int port)
        {
            ListenerTcp tcp;
            try
            {
                tcp = new ListenerTcp(ip, port);
            }
            catch (Exception e)
            {
                Utils.Debug.LogError(e.ToString());
                return null;
            }
            return tcp;
        }
    }
}