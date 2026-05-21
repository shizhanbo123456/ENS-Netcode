using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Utils;

namespace ProtocolWrapper.Protocols.Udp
{
    internal abstract class WrapperUdp : ProtocolBase
    {
        protected UdpClient Client;
        protected IPEndPoint ep;
        public void Init(UdpClient client,IPEndPoint ep)
        {
            Client= client;
            this.ep= ep;
            base.Init(ep.Address.ToString(),ep.Port);
        }
        

        public override void Send(byte[] bytes, int length)
        {
            if (!Initialized)
            {
                Debug.LogError("[W]未完成初始化");
                return;
            }
            if (Cancelled)
            {
                Debug.LogError("[W]已被取消");
                return;
            }
            try
            {
                //Utils.Debug.Log("发送："+ep.ToString());
                //Utils.Debug.PrintBytes(bytes, new Segment(0, length));
                Client.Send(bytes, length,ep);
            }
            catch (Exception)
            {

            }
        }
        protected override void ReleaseUnmanagedMenory()
        {
            Client?.Dispose();
            base.ReleaseUnmanagedMenory();
        }
        protected override void ReleaseManagedMenory()
        {
            ep = null;
            Client = null;
            base.ReleaseManagedMenory();
        }
    }
}