using ProtocolWrapper;
using System;
using System.Collections.Generic;
using System.Net;
using Utils;

public class EnsServer
{
    internal static EnsServer Instance;

    public static Func<EnsRoomManager> RoomManagerFactory;
    public static Func<int,EnsRoom> RoomFactory;

    internal Dictionary<int, EnsConnection> ClientConnections;
    private static CircularQueue<int>ToRemove= new CircularQueue<int>(10);
    private readonly CircularQueue<ProtocolBase> PendingConnections = new CircularQueue<ProtocolBase>(10);
    private readonly Action<ProtocolBase> connectionReceiver;
    internal EnsRoomManager RoomManager;
    internal ListenerBase Listener;

    private short connectionIndex=0;

    public bool On;

    public EnsServer(IPAddress ip, int port)
    {
        Instance = this;

        ClientConnections = new Dictionary<int, EnsConnection>();
        // 自定义传输即使从工作线程报告连接，也只会在这里记录；
        // EnsConnection 和 ClientConnections 始终由主循环创建、修改。
        connectionReceiver = conn =>
        {
            if (conn != null) PendingConnections.Write(conn);
        };
        Protocol.OnRecvConnection = connectionReceiver;

        Listener = Protocol.GetListener(ip, port);
        RoomManager =RoomManagerFactory==null? 
            new EnsRoomManager():
            RoomManagerFactory.Invoke();
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
    private void AcceptPendingConnections()
    {
        while (PendingConnections.Read(out var connection))
        {
            if (!On)
            {
                connection.ShutDown();
                connection.Dispose();
                continue;
            }
            OnRecvConnection(connection);
        }
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
        Listener.Update();
        AcceptPendingConnections();
        RoomManager.Update();
        foreach (var r in RoomManager.rooms) r.Value.Update();

        foreach (var kvp in ClientConnections)
        {
            var i=kvp.Value;
            if (Time.time > i.hbRecvTime)
            {
                ToRemove.Write(kvp.Key);
                continue;
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
    public virtual void ShutDown()
    {
        if (!On) return;
        On = false;
        EndListening();
        Listener.ShutDown();
        Listener.Dispose();
        if (Protocol.OnRecvConnection == connectionReceiver)
            Protocol.OnRecvConnection = null;
        AcceptPendingConnections();
        RoomManager.ShutDown();
        foreach (var i in ClientConnections.Values) i.ShutDown();
        ClientConnections.Clear();
        ClientConnections = null;
        Listener = null;
        RoomManager = null;
        Instance = null;
    }
}
