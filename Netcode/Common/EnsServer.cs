using ProtocolWrapper;
using System;
using System.Collections.Generic;
using System.Net;
using Utils;

public class EnsServer
{
    internal static EnsServer Instance;

    internal Dictionary<int, EnsConnection> ClientConnections;
    private static CircularQueue<int>ToRemove= new CircularQueue<int>(10);
    internal EnsRoomManager RoomManager;
    internal ListenerBase Listener;

    private short connectionIndex=0;

    public bool On;

    public EnsServer(IPAddress ip, int port)
    {
        Instance = this;

        ClientConnections = new Dictionary<int, EnsConnection>();
        Protocol.OnRecvConnection = conn => OnRecvConnection(conn);

        Listener = Protocol.GetListener(ip, port);
        RoomManager = new EnsRoomManager();
        On = true;
    }
    internal virtual void OnRecvConnection(ProtocolBase conn)
    {
        do
        {
            connectionIndex++;
            if (connectionIndex == short.MaxValue)
            {
                connectionIndex = 0;
            }
        } 
        while (ClientConnections.ContainsKey(connectionIndex));
        short index = connectionIndex;
        EnsConnection connection = new EnsConnection(conn, index,OnConnectionShutDown);
        ClientConnections.Add(index, connection);
    }
    internal void OnConnectionShutDown(EnsConnection conn)
    {
        if (!On) return;
        if (conn.room != null) conn.room.Exit(conn);
        ToRemove.Write(conn.ClientId);
    }
    public void StartListening()
    {
        if (!Listener.Listening) Listener.StartListening();
    }
    public void EndListening()
    {
        if (Listener.Listening) Listener.EndListening();
    }
    internal void Update()
    {
        foreach (var kvp in ClientConnections)
        {
            var i=kvp.Value;
            if (Time.time > i.hbRecvTime)
            {
                ToRemove.Write(kvp.Key);
                continue;
            }
            if (Time.time > i.hbSendTime)
            {
                HeartBeatMessageWriter.instance.delay=i.delay;
                i.Send(Header.H,  Delivery.Unreliable, HeartBeatMessageWriter.instance);
                i.hbSendTime = Time.time + EnsInstance.HeartbeatMsgInterval;
            }
            i.Update();
        }
        if (ToRemove.Count > 0)
        {
            while(ToRemove.Read(out var connectionIndex))
            {
                if (!ClientConnections.ContainsKey(connectionIndex)) continue;
                ClientConnections[connectionIndex].ShutDown();
                ClientConnections.Remove(connectionIndex);
            }
        }
    }
    internal void FlushSendBuffer()
    {
        foreach (var i in ClientConnections.Values) i.FlushSendBuffer();
    }
    private class HeartBeatMessageWriter : MessageWriter
    {
        internal static HeartBeatMessageWriter instance=new();
        internal int delay;

        public int GetLength()
        {
            return sizeof(int) * 2;
        }
        public bool Write(SendBuffer buffer)
        {
            return IntSerializer.Serialize((int)(Utils.Time.time * 1000), buffer.bytes, ref buffer.indexStart)
                && IntSerializer.Serialize(delay, buffer.bytes, ref buffer.indexStart);
        }
        public MessageWriter Clone()
        {
            return new HeartBeatMessageWriter() { delay=delay};
        }
        public void Dispose()
        {

        }
    }
    public virtual void ShutDown()
    {
        if (!On) return;
        On = false;
        EndListening();
        Listener.ShutDown();
        Listener.Dispose();
        RoomManager.ShutDown();
        foreach (var i in ClientConnections.Values) i.ShutDown();
        ClientConnections.Clear();
        ClientConnections = null;
        Listener = null;
        RoomManager = null;
        Instance = null;
    }
}
