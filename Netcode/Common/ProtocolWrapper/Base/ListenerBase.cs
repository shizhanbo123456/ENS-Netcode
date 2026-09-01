using System;
using System.Collections.Generic;
using System.Net;

namespace ProtocolWrapper
{
    public abstract class ListenerBase:Disposable
    {
        protected IPAddress IP;
        protected int Port;
        public volatile bool Listening=false;
        public volatile bool Cancelled=false;

        public int Id;
        public ListenerBase(IPAddress iP, int port)
        {
            IP = iP;
            Port = port;

            Id=Protocol.id++;
        }

        public abstract void StartListening();
        public abstract void EndListening();
        /// <summary>
        /// 在 ENS 主循环中调用。传输子线程只记录最小接收状态，
        /// 连接创建、集合修改和上层回调在这里完成。
        /// </summary>
        public virtual void Update()
        {

        }
        public virtual void ShutDown()
        {
            Listening = false;
            Cancelled = true;
        }
        protected override void ReleaseManagedMenory()
        {
            IP=null;
            base.ReleaseManagedMenory();
        }
    }
}
