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
                    OnRecvConnection(Client);
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
                    OnRecvConnection(Client);
                }
                catch
                {
                    
                }
            }
        }
        private void OnRecvConnection(TcpClient Client)
        {
            if (!Listening)
            {
                Client.Close();
                Client.Dispose();
                return;
            }
            var Connection = new ConnectionTcp();
            Connection.Init(Client);
            Protocol.OnRecvConnection?.Invoke(Connection);
        }

        public override void ShutDown()
        {
            Listening = false;
            Cancelled = true;
        }
        protected override void ReleaseUnmanagedMenory()
        {
            Listener = null;
        }
    }
}
