using System;
using System.Net;

namespace ProtocolWrapper.Protocols.Tcp
{
    internal class TransportTcp
    {
        internal static ProtocolBase GetProtocolBase(string ip, int port)
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
        internal static ListenerBase GetListener(IPAddress ip, int port)
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