using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace ProtocolWrapper.Protocols.Tcp
{
    /// <summary>
    /// 需要调用StartListening/EndListening<br></br>
    /// </summary>
    internal class ListenerTcp : ListenerBase
    {
        private TcpListener Listener;
        // Accept 线程只记录已接受的 socket；ConnectionTcp 的创建与上层回调在主线程完成。
        private readonly CircularQueue<TcpClient> PendingClients = new CircularQueue<TcpClient>(10);

        public ListenerTcp(IPAddress ip,int port) : base(ip, port)
        {
            Listener = new TcpListener(ip, Port);
            Listening = false;
        }
        public override void StartListening()
        {
            if (Listening) throw new Exception("[W]Listener已经启动");
            Listener.Start();
            Listening = true;
            if (Protocol.mode == ConcurrentType.Multithreading)
            {
                Thread AcceptClientsThread = new Thread(new ThreadStart(AcceptClients));
                AcceptClientsThread.Start();
            }
            else
            {
                _ = AcceptClientsAsync();
            }
        }
        public override void EndListening()
        {
            if (!Listening) throw new Exception("[W]Listener已经关闭");
            Listening = false;
            Listener.Stop();
        }
        protected virtual void AcceptClients()
        {
            while (Listening)
            {
                try
                {
                    TcpClient Client = Listener.AcceptTcpClient();//------------------------------会导致线程阻塞
                    PendingClients.Write(Client);
                }
                catch
                {
                    
                }
            }
        }
        private async Task AcceptClientsAsync()
        {
            while (Listening)
            {
                try
                {
                    TcpClient Client = await Listener.AcceptTcpClientAsync(); // 异步接受客户端连接  
                    PendingClients.Write(Client);
                }
                catch
                {
                    
                }
            }
        }
        public override void Update()
        {
            while (PendingClients.Read(out var client))
            {
                if (!Listening || Cancelled)
                {
                    client.Close();
                    client.Dispose();
                    continue;
                }
                var connection = new ConnectionTcp();
                connection.Init(client);
                Protocol.OnRecvConnection?.Invoke(connection);
            }
        }

        public override void ShutDown()
        {
            if (Listening) EndListening();
            Cancelled = true;
        }
        protected override void ReleaseManagedMenory()
        {
            while (PendingClients.Read(out var client))
            {
                client.Close();
                client.Dispose();
            }
            base.ReleaseManagedMenory();
        }
        protected override void ReleaseUnmanagedMenory()
        {
            Listener = null;
        }
    }
}
