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
    protected Dictionary<int,EnsConnection> ClientConnections =new Dictionary<int, EnsConnection>();
    public int RoomId;
    internal protected short CurrentAuthorityAt = -1;

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
    public EnsRoom(int id)
    {
        RoomId = id;
    }
    public virtual void Join(EnsConnection conn)
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
    protected class BoolWriter : MessageWriter
    {
        public static BoolWriter instance=new();
        public bool target;

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
    public virtual void Exit(EnsConnection conn)
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
    protected class E_EventMessageWriter:MessageWriter
    {
        public static E_EventMessageWriter instance=new();
        public byte b;
        public short connId;

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
    public virtual void SetAuthority(short clientId)
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
    protected void ConnectionSend(EnsConnection conn,byte messageType, Delivery delivery, MessageWriter writer = null)
    {
        conn.Send(messageType, delivery, writer);
    }
    internal protected void Broadcast(byte messageType,Delivery delivery, MessageWriter writer = null)
    {
        foreach (var i in ClientConnections.Values) i.Send(messageType,delivery,writer);
    }
    internal protected void Broadcast(int ignore, byte messageType, Delivery delivery, MessageWriter writer = null)
    {
        foreach (var i in ClientConnections.Values) 
            if (i.ClientId != ignore) 
                i.Send(messageType, delivery, writer);
    }
    internal protected void PTP(short id, byte messageType,  Delivery delivery, MessageWriter writer = null)
    {
        if(ClientConnections.TryGetValue(id, out var conn))
        {
            conn.Send(messageType, delivery, writer);
        }
    }
    public virtual void RecvEvent(int type, string content)
    {

    }
    public virtual void ShutDown()
    {
        Broadcast(Header.R, Delivery.Reliable, null);
        EnsRoomManager.Instance.rooms.Remove(RoomId);
        foreach (var i in ClientConnections.Values) i.room = null;
        ClientConnections.Clear();
        ClientConnections = null;
    }
    public virtual void Update()
    {

    }
    public static void TrigClientEvent(Delivery delivery, int header, string content)
    {
        if (EnsInstance.Corr != null && EnsInstance.Corr.Client != null)
        {
            Writer.instance.t_type = header;
            Writer.instance.t_content = content;
            EnsInstance.Corr.Client.Send(Header.N, delivery, Writer.instance);
        }
        else
        {
            Utils.Debug.LogError("当前状态不能发送消息");
        }
    }
    internal class Writer : MessageWriter
    {
        internal static Writer instance = new();
        internal int t_type;
        internal string t_content;

        public int GetLength()
        {
            return sizeof(int) + StringSerializer.GetLength(t_content);
        }
        public bool Write(SendBuffer b)
        {
            return IntSerializer.Serialize(t_type, b.bytes, ref b.indexStart) &&
                StringSerializer.Serialize(t_content, b.bytes, ref b.indexStart);
        }
        public MessageWriter Clone()
        {
            return new Writer() { t_type = t_type, t_content = t_content };
        }
        public void Dispose()
        {

        }
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