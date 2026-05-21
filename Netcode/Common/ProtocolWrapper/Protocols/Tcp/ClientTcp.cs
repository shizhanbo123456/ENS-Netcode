using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace ProtocolWrapper.Protocols.Tcp
{
    /// <summary>
    /// 需要单独调用Init，需要等待初始化<br></br>
    /// </summary>
    internal class ClientTcp : WrapperTcp
    {
        public new void Init(string ip, int port)
        {
            IP=ip;
            Port=port;
            if (Protocol.mode == ConcurrentType.Multithreading)
            {
                Thread init = new Thread(new ThreadStart(Init));
                init.Start();
            }
            else
            {
                _ = InitAsync();
            }
        }

        private void Init()
        {
            try
            {
                Client = new TcpClient(IP, Port);
            }
            catch
            {
                ShutDown();
                return;
            }

            Init(Client);
            Initialized = true;
            Protocol.OnClientInitialized?.Invoke();

            Thread Recv = new Thread(new ThreadStart(Receive));
            Recv.Start();
        }
        private async Task InitAsync()
        {
            Client = new TcpClient();
            try
            {
                await Client.ConnectAsync(IP, Port);
            }
            catch
            {
                ShutDown();
                return;
            }

            Init(Client);
            Initialized = true;
            Protocol.OnClientInitialized?.Invoke();

            _ = ReceiveAsync();
        }
    }
}
