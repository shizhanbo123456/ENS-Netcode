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
        private readonly CircularQueue<(byte[] data, IPEndPoint endPoint)> PendingDatagrams =
            new CircularQueue<(byte[] data, IPEndPoint endPoint)>(20);
        private bool receiveLoopStarted;

        public ListenerUdp(IPAddress ip,int port):base(ip,port)
        {
            client = new UdpClient(port);
            Listening = false;
        }
        public override void StartListening()
        {
            Listening = true;
            if (receiveLoopStarted) return;
            receiveLoopStarted = true;
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
                    if (Listening) PendingDatagrams.Write((b, remoteEp));
                }
                catch
                {
                    if (Cancelled) return;
                }
            }
        }
        public async Task RecvAsync()
        {
            while (!Cancelled)
            {
                try
                {
                    var r = await client.ReceiveAsync();
                    if (Cancelled) return;
                    if (Listening) PendingDatagrams.Write((r.Buffer, r.RemoteEndPoint));
                }
                catch
                {
                    if (Cancelled) return;
                }
            }
        }
        public override void Update()
        {
            while (PendingDatagrams.Read(out var datagram))
            {
                if (!Listening || Cancelled) continue;
                if (!Connections.TryGetValue(datagram.endPoint, out var connection))
                {
                    connection = new ConnectionUdp();
                    connection.Init(this, datagram.endPoint);
                    Connections.Add(datagram.endPoint, connection);
                    Protocol.OnRecvConnection?.Invoke(connection);
                }
                if (!connection.Cancelled) connection.RecvBuffer.Write(datagram.data);
            }
        }


        public override void ShutDown()
        {
            if(Listening)EndListening();
            Cancelled = true;
            client?.Close();
            while (PendingDatagrams.Read(out _)) { }
            // ConnectionUdp.ShutDown 会将自身从 Connections 中移除。
            while (Connections.Count > 0)
            {
                Connections.First().Value.ShutDown();
            }
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
