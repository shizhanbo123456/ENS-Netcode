using System;
using System.Collections.Generic;
using Utils;
using System.Threading;

namespace ProtocolWrapper
{
    public abstract class ProtocolBase : Disposable
    {
        public string IP;
        public int Port;
        internal SendBuffer SendBuffer;
        /// <summary>
        /// 取出后无需放回字节数组池，均为临时数组
        /// </summary>
        public CircularQueue<byte[]> ReceiveBuffer;

        public volatile bool Initialized = false;
        public volatile bool Cancelled = false;
        private int transportClosed;
        /// <summary>
        /// 由传输线程记录，由 ENS 主线程读取并执行上层断联清理。
        /// UDP 等无法直接检测远端关闭的传输可保持默认值。
        /// </summary>
        public bool TransportClosed => Volatile.Read(ref transportClosed) != 0;
        public bool On
        {
            get
            {
                return Initialized && !Cancelled;
            }
        }

        public int Id;

        /// <summary>
        /// Fill data only
        /// </summary>
        protected void Init(string ip, int port)
        {
            SendBuffer = new SendBuffer(Send);
            ReceiveBuffer = new CircularQueue<byte[]>(20);
            IP = ip;
            Port = port;

            Id = Protocol.id++;
        }


        //直接对外暴露SendBuffer方便直接写入，省去复制
        public abstract void Send(byte[] bytes, int length);

        public virtual void ShutDown()
        {
            Cancelled = true;
        }

        protected void MarkTransportClosed()
        {
            Volatile.Write(ref transportClosed, 1);
        }



        public static void BytesToLength(byte left, byte right, out int value)
        {
            value = left * 200 + right;
        }
        public static void LengthToBytes(int value, out byte left, out byte right)
        {
            left = (byte)(value / 200);
            right = (byte)(value % 200);
        }

        protected override void ReleaseUnmanagedMenory()
        {
            SendBuffer?.Dispose();
            base.ReleaseUnmanagedMenory();
        }
        protected override void ReleaseManagedMenory()
        {
            SendBuffer = null;
            ReceiveBuffer = null;
            base.ReleaseManagedMenory();
        }
    }
}
