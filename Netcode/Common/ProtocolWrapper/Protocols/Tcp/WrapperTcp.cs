using Utils;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace ProtocolWrapper.Protocols.Tcp
{
    internal abstract class WrapperTcp : ProtocolBase
    {
        protected TcpClient Client;
        protected NetworkStream Stream;

        /// <summary>
        /// 需要client已经初始化
        /// </summary>
        protected void Init(TcpClient client)
        {
            Client = client;
            Stream = client.GetStream();
            Init((Client.Client.RemoteEndPoint as IPEndPoint).Address.ToString(), (Client.Client.RemoteEndPoint as IPEndPoint).Port);
        }
        protected virtual void Receive()
        {
            var buffer = BytesPool.GetBuffer(512);
            while (!Cancelled)
            {
                try
                {
                    int bytesRead = Stream.Read(buffer, 0, buffer.Length);
                    OnRecvData(buffer, bytesRead);
                }
                catch
                {
                    
                }
            }
            BytesPool.ReturnBuffer(buffer);
        }

        protected async Task ReceiveAsync()
        {
            var buffer = BytesPool.GetBuffer(512);
            while (!Cancelled)
            {
                try
                {
                    int bytesRead = await Stream.ReadAsync(buffer, 0, buffer.Length);
                    OnRecvData(buffer, bytesRead);
                }
                catch
                {
                    
                }
            }
            BytesPool.ReturnBuffer(buffer);
        }
        private void OnRecvData(byte[] buffer,int bytesRead)
        {
            if (bytesRead == 0 || ReceiveBuffer.Full()) return;
            if (Cancelled) return;
            byte[] data = new byte[bytesRead];
            Buffer.BlockCopy(buffer,0, data, 0, bytesRead);
            ReceiveBuffer.Write(data);
        }

        public override void Send(byte[] bytes,int length)
        {
            if (!Initialized)
            {
                Debug.LogError("[W]WrapperTcp未完成初始化");
                return;
            }
            if (Cancelled)
            {
                Debug.LogError("[W]WrapperTcp已被取消");
                return;
            }

            try
            {
                Stream.Write(bytes, 0, length);
            }
            catch (Exception)
            {
                
            }
        }
        public override void ShutDown()
        {
            base.ShutDown();
            Stream?.Close();
            Client?.Close();
        }
        protected override void ReleaseUnmanagedMenory()
        {
            Stream?.Dispose();
            Client?.Dispose();
            base.ReleaseUnmanagedMenory();
        }
        protected override void ReleaseManagedMenory()
        {
            Stream = null;
            Client = null;
            base.ReleaseManagedMenory();
        }
    }
}
