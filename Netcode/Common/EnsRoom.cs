using System;
using System.Collections.Generic;

public class EnsRoom
{
    public static EnsRoom Instance
    {
        get
        {
            if(EnsRoomManager.Instance == null)return null;
            if(EnsRoomManager.Instance.rooms.TryGetValue(EnsRoomManager.roomIdStart,out var room))return room;
            return null;
        }
    }
    private Dictionary<int,EnsConnection> ClientConnections =new Dictionary<int, EnsConnection>();
    public int RoomId;
    internal short CurrentAuthorityAt = -1;

    public Dictionary<string, (char, int)> Rule = new Dictionary<string, (char, int)>();
    public Dictionary<string,string>Info= new Dictionary<string, string>();

    // >0为游戏过程中制造的物体的Id
    private short createdid = 1;
    internal short CreatedId
    {
        get
        {
            return createdid;
        }
        set
        {
            createdid = value;
            if (createdid >= 30000) createdid -= 29900;
        }
    }

    private EnsRoom() { }
    internal EnsRoom(int id)
    {
        RoomId = id;
    }
    internal void Join(EnsConnection conn)
    {
        ClientConnections.Add(conn.ClientId,conn);
        conn.room = this;
        E_EventMessageWriter.instance.b = 0x01;
        E_EventMessageWriter.instance.connId = conn.ClientId;
        Broadcast(conn.ClientId, Header.E, Delivery.Reliable, E_EventMessageWriter.instance);
        if (CurrentAuthorityAt == -1)
        {
            CurrentAuthorityAt = conn.ClientId;
            BoolWriter.instance.target = true;
            conn.Send(Header.A, Delivery.Reliable, BoolWriter.instance);
        }
        else
        {
            BoolWriter.instance.target = false;
            conn.Send(Header.A, Delivery.Reliable, BoolWriter.instance);
        }
    }
    private class BoolWriter : MessageWriter
    {
        internal static BoolWriter instance=new();
        internal bool target;

        public int GetLength()
        {
            return sizeof(bool);
        }
        public bool Write(SendBuffer b)
        {
            return BoolSerializer.Serialize(target, b.bytes, ref b.indexStart);
        }
        public MessageWriter Clone()
        {
            return new BoolWriter() { target=target};
        }
        public void Dispose()
        {

        }
    }
    internal void Exit(EnsConnection conn)
    {
        ClientConnections.Remove(conn.ClientId);
        conn.room = null;
        if (conn.ClientId == CurrentAuthorityAt)
        {
            ShutDown();
        }
        else
        {
            E_EventMessageWriter.instance.b = 0x02;
            E_EventMessageWriter.instance.connId = conn.ClientId;
            Broadcast(conn.ClientId, Header.E, Delivery.Reliable,E_EventMessageWriter.instance);
        }
    }
    private class E_EventMessageWriter:MessageWriter
    {
        internal static E_EventMessageWriter instance=new();
        internal byte b;
        internal short connId;

        public int GetLength()
        {
            return sizeof(byte) + sizeof(short);
        }
        public bool Write(SendBuffer b)
        {
            return ByteSerializer.Serialize(this.b, b.bytes, ref b.indexStart)
                    && ShortSerializer.Serialize(connId, b.bytes, ref b.indexStart);
        }
        public MessageWriter Clone()
        {
            return new E_EventMessageWriter() { b=b,connId=connId};
        }
        public void Dispose()
        {

        }
    }
    internal void SetAuthority(short clientId)
    {
        if (!ClientConnections.ContainsKey(clientId)) return;
        if (ClientConnections.ContainsKey(CurrentAuthorityAt))
        {
            var conn = ClientConnections[CurrentAuthorityAt];
            BoolWriter.instance.target = false;
            conn.Send(Header.A, Delivery.Reliable, BoolWriter.instance);
        }
        CurrentAuthorityAt= clientId;
        var c = ClientConnections[CurrentAuthorityAt];
        BoolWriter.instance.target = true;
        c.Send(Header.A, Delivery.Reliable, BoolWriter.instance);
    }

    internal void Broadcast(byte messageType,Delivery delivery, MessageWriter writer = null)
    {
        foreach (var i in ClientConnections.Values) i.Send(messageType,delivery,writer);
    }
    internal void Broadcast(int ignore, byte messageType, Delivery delivery, MessageWriter writer = null)
    {
        foreach (var i in ClientConnections.Values) 
            if (i.ClientId != ignore) 
                i.Send(messageType, delivery, writer);
    }
    internal void PTP(short id, byte messageType,  Delivery delivery, MessageWriter writer = null)
    {
        if(ClientConnections.TryGetValue(id, out var conn))
        {
            conn.Send(messageType, delivery, writer);
        }
    }

    internal void ShutDown()
    {
        Broadcast(Header.R, Delivery.Reliable, null);
        EnsRoomManager.Instance.rooms.Remove(RoomId);
        foreach (var i in ClientConnections.Values) i.room = null;
        ClientConnections.Clear();
        ClientConnections = null;
    }
    public override string ToString()
    {
        string t = "[ " + RoomId.ToString() + " : ";
        bool first = true;
        foreach (var i in ClientConnections.Values)
        {
            if (!first) t += ",";
            t += i.ClientId;
            first = false;
        }
        t += "]";
        return t;
    }
}