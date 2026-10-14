using Ens.ProtoFormat;
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
        Broadcast(conn.ClientId, Delivery.Reliable, new MessageBody_E_SC { eventId = 1, clientId = conn.ClientId }, Pkt.scE);
        if (CurrentAuthorityAt == -1)
        {
            CurrentAuthorityAt = conn.ClientId;
            conn.Send(Delivery.Reliable, new MessageBody_A_SC { authorityActive = true }, Pkt.scA);
        }
        else
        {
            conn.Send(Delivery.Reliable, new MessageBody_A_SC { authorityActive = false }, Pkt.scA);
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
            Broadcast(conn.ClientId, Delivery.Reliable, new MessageBody_E_SC { eventId = 2, clientId = conn.ClientId }, Pkt.scE);
        }
    }
    public virtual void SetAuthority(short clientId)
    {
        if (!ClientConnections.ContainsKey(clientId)) return;
        if (ClientConnections.ContainsKey(CurrentAuthorityAt))
        {
            var conn = ClientConnections[CurrentAuthorityAt];
            conn.Send(Delivery.Reliable, new MessageBody_A_SC { authorityActive = false }, Pkt.scA);
        }
        CurrentAuthorityAt= clientId;
        var c = ClientConnections[CurrentAuthorityAt];
        c.Send(Delivery.Reliable, new MessageBody_A_SC { authorityActive = true }, Pkt.scA);
    }
    internal protected void ConnectionSend<TBody>(EnsConnection conn, Delivery delivery, TBody body, ProtoSender<TBody> proto) where TBody : struct
    {
        conn.Send(delivery, body, proto);
    }
    internal protected void Broadcast<TBody>(Delivery delivery, TBody body, ProtoSender<TBody> proto) where TBody : struct
    {
        foreach (var i in ClientConnections.Values) i.Send(delivery, body, proto);
    }
    internal protected void Broadcast<TBody>(int ignore, Delivery delivery, TBody body, ProtoSender<TBody> proto) where TBody : struct
    {
        foreach (var i in ClientConnections.Values) 
            if (i.ClientId != ignore) 
                i.Send(delivery, body, proto);
    }
    internal protected void PTP<TBody>(short id, Delivery delivery, TBody body, ProtoSender<TBody> proto) where TBody : struct
    {
        if(ClientConnections.TryGetValue(id, out var conn))
        {
            conn.Send(delivery, body, proto);
        }
    }
    public virtual void RecvEvent(int type, string content)
    {

    }
    public virtual void ShutDown()
    {
        Broadcast(Delivery.Reliable, default(MessageBody_R_SC), Pkt.scR);
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
            EnsInstance.Corr.Client.Send(delivery, new MessageBody_N_CS { type = header, content = content }, Pkt.csN);
        }
        else
        {
            Utils.Debug.LogError("当前状态不能发送消息");
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