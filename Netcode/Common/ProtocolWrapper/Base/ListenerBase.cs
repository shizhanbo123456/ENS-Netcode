using System;
using System.Collections.Generic;
using System.Net;

namespace ProtocolWrapper
{
    public abstract class ListenerBase:Disposable
    {
        protected IPAddress IP;
        protected int Port;
        public bool Listening=false;
        public bool Cancelled=false;

        public int Id;
        public ListenerBase(IPAddress iP, int port)
        {
            IP = iP;
            Port = port;

            Id=Protocol.id++;
        }

        public abstract void StartListening();
        public abstract void EndListening();
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
