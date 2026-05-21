using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Utils;

namespace ProtocolWrapper.Protocols.Udp
{
    internal class ConnectionUdp : WrapperUdp
    {
        public CircularQueue<byte[]> RecvBuffer => ReceiveBuffer;
        private ListenerUdp Listener;//ÒýÓÃ
        protected IPAddress ipAddress;
        public void Init(ListenerUdp listener, IPEndPoint ep)
        {
            Listener = listener;
            ipAddress=ep.Address;
            base.Init(listener.client, ep);
            Initialized = true;
        }


        public override void ShutDown()
        {
            Cancelled = true;
            if(Listener.Connections.ContainsKey(ep))Listener.Connections.Remove(ep);
        }
        protected override void ReleaseManagedMenory()
        {
            Listener = null;
            ipAddress = null;
            base.ReleaseManagedMenory();
        }
    }
}