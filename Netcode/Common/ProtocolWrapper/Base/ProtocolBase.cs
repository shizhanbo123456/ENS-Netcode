using System;
using System.Collections.Generic;
using Utils;

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

        public bool Initialized = false;
        public bool Cancelled = false;
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
