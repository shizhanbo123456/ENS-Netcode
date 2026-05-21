using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Utils;

namespace ProtocolWrapper.Protocols.Udp
{
    /// <summary>
    /// 需要调用StartListening/EndListening<br></br>
    /// </summary>
    internal class ListenerUdp : ListenerBase
    {
        public UdpClient client;

        public Dictionary<IPEndPoint,ConnectionUdp>Connections=new Dictionary<IPEndPoint, ConnectionUdp>();

        public ListenerUdp(IPAddress ip,int port):base(ip,port)
        {
            client = new UdpClient(port);
            Listening = false;
        }
        public override void StartListening()
        {
            Listening = true; 
            if (Protocol.mode == ConcurrentType.Multithreading)
            {
                Thread t = new Thread(new ThreadStart(Recv));
                t.Start();
            }
            else
            {
                _ = RecvAsync();
            }
        }
        public override void EndListening()
        {
            Listening = false;
        }


        public void Recv()
        {
            IPEndPoint remoteEp = new IPEndPoint(IPAddress.Any, 0);
            while (!Cancelled)
            {
                try
                {
                    var b = client.Receive(ref remoteEp);
                    if (Cancelled) return;
                    OnRecvData(b, remoteEp);
                }
                catch
                {
                    
                }
            }
        }
        public async Task RecvAsync()
        {
            while (Listening)
            {
                try
                {
                    var r = await client.ReceiveAsync();
                    if (!Listening) return;
                    var b = r.Buffer;
                    OnRecvData(b,r.RemoteEndPoint);
                }
                catch
                {

                }
            }
        }
        private void OnRecvData(byte[] b,IPEndPoint ep)
        {
            if (!Connections.ContainsKey(ep))
            {
                if (!Listening) return;
                var c = new ConnectionUdp();
                c.Init(this, ep);
                Connections.Add(ep, c);
                Protocol.OnRecvConnection?.Invoke(c);
            }
            var conn = Connections[ep];
            if (!conn.Cancelled) conn.RecvBuffer.Write(b);
            else return;
        }


        public override void ShutDown()
        {
            if(Listening)EndListening();
            Cancelled = true;
            foreach (var c in Connections.Keys)Connections[c].ShutDown();
        }


        protected override void ReleaseManagedMenory()
        {
            client.Dispose();
            foreach (var c in Connections.Keys)Connections[c].Dispose();
            Connections.Clear();
        }
        protected override void ReleaseUnmanagedMenory()
        {
            client = null;
            Connections.Clear();
            Connections = null;
        }
    }
}