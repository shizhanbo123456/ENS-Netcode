using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Utils;

namespace ProtocolWrapper.Protocols.Udp
{
    internal class ClientUdp : WrapperUdp
    {
        private IPAddress ipAddress;
        public new void Init(string ip,int port)
        {
            ipAddress=IPAddress.Parse(ip);
            Init(new UdpClient(), new IPEndPoint(ipAddress, port));
            Initialized = true;
            Protocol.OnClientInitialized?.Invoke();

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
        public void Recv()
        {
            IPEndPoint remoteEp = new IPEndPoint(IPAddress.Any, 0);
            while (!Cancelled)
            {
                try
                {
                    var b = Client.Receive(ref remoteEp);
                    if (Cancelled)
                    {
                        Debug.LogError("[W]已被取消");
                        return;
                    }
                    ReceiveBuffer.Write(b);
                }
                catch
                {
                    
                }
            }
        }
        public async Task RecvAsync()
        {
            while (!Cancelled)
            {
                try
                {
                    var r = await Client.ReceiveAsync();
                    if (Cancelled)
                    {
                        Debug.LogError("[W]已被取消");
                        return;
                    }
                    ReceiveBuffer.Write(r.Buffer);
                }
                catch
                { 
                    
                }
            }
        }
        protected override void ReleaseManagedMenory()
        {
            ipAddress = null;
            base.ReleaseManagedMenory();
        }
    }
}