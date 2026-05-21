using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;

namespace ProtocolWrapper.Protocols.Tcp
{
    internal class ConnectionTcp : WrapperTcp
    {
        public new void Init(TcpClient client)
        {
            base.Init(client);
            Initialized = true;

            if (Protocol.mode == ConcurrentType.Multithreading)
            {
                Thread recv = new Thread(new ThreadStart(Receive));
                recv.Start();
            }
            else
            {
                _ = ReceiveAsync();
            }
        }
    }
}
