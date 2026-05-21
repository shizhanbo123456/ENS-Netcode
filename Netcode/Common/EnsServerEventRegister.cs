using System;
using System.Collections.Generic;
using Utils;
using request = Ens.Request;
public class EnsServerEventRegister
{
    public static void RegistDedicateServer()
    {
        Server_A();
        Server_H();
        Server_D();
        Server_F();
        Server_f();
        Server_Q();
        Server_Any();
        RegistServerRequests();
    }
    private static void RegistServerRequests()
    {
        EnsServerRequest.RegistRequest(new request.Server.CreateRoom());
        EnsServerRequest.RegistRequest(new request.Server.SetRule());
        EnsServerRequest.RegistRequest(new request.Server.JoinRoom());
        EnsServerRequest.RegistRequest(new request.Server.GetRule());
        EnsServerRequest.RegistRequest(new request.Server.ExitRoom());
        EnsServerRequest.RegistRequest(new request.Server.GetRoomList());
        EnsServerRequest.RegistRequest(new request.Server.SetInfo());
        EnsServerRequest.RegistRequest(new request.Server.GetInfo());
    }
    protected static void Server_Any()
    {
        MessageHandlerServer.RegistAny((conn,b,s) =>
        {
            conn.hbRecvTime=Time.time+EnsInstance.DisconnectThreshold;
        });
    }
    protected static void Server_H()
    {
        MessageHandlerServer.Regist(Header.H,(conn, b, s) =>
        {
            int index = MessageReader.BodyIndexStart(s);
            int invalidIndex=MessageReader.BodyIndexInvalid(s);
            if (invalidIndex - index >= 4)
            {
                var d = IntSerializer.Deserialize(b, ref index, invalidIndex);
                conn.delay = ((int)(Utils.Time.time * 1000) - d) / 2;
            }
        });
    }
    protected static void Server_D()
    {
        MessageHandlerServer.Regist(Header.D,(conn, b, s) =>
        {
            conn.ShutDown();
        });
    }

    protected static void Server_A()
    {
        MessageHandlerServer.Regist(Header.A,(conn, b, s) =>
        {
            if (conn.room == null) return;
            int index = MessageReader.BodyIndexStart(s);
            int invalidIndex = MessageReader.BodyIndexInvalid(s);
            var d = ShortSerializer.Deserialize(b, ref index, invalidIndex);
            conn.room.SetAuthority(d);
        });
    }
    private class F_MessageWriter : MessageWriter
    {
        internal static F_MessageWriter instance = new();
        internal byte[] t_bytes;
        internal Segment t_segment;

        public int GetLength()
        {
            return t_segment.Length - MessageReader.BodyOffset;
        }
        public bool Write(SendBuffer b)
        {
            if (b.bytes.Length - b.indexStart < t_segment.Length + MessageReader.BodyOffset) return false;
            int length = t_segment.Length - MessageReader.BodyOffset;
            Buffer.BlockCopy(t_bytes, t_segment.StartIndex + MessageReader.BodyOffset, b.bytes, b.indexStart, length);
            b.indexStart += length;
            return true;
        }
        public MessageWriter Clone()
        {
            byte[] newBuffer = BytesPool.GetBuffer(t_segment.Length);
            Buffer.BlockCopy(t_bytes,t_segment.StartIndex,newBuffer,0,t_segment.Length);
            return new F_MessageWriter
            {
                t_bytes = newBuffer,
                t_segment = new Segment(0, t_segment.Length)
            };
        }
        public void Dispose()
        {
            BytesPool.ReturnBuffer(t_bytes);
        }
    }
    protected static void Server_F()
    {
        MessageHandlerServer.Regist(Header.F,(conn, b, s) =>
        {
            if (conn.room == null) return;
            byte messageType = MessageReader.Header(b, s);
            SendTo to = SendTo.To(b[s.StartIndex + s.Length - 2], b[s.StartIndex + s.Length - 1]);
            Delivery delivery = DeliverySource.IdToDelivery(MessageReader.Delivery(b,s));
            F_MessageWriter.instance.t_bytes = b;
            F_MessageWriter.instance.t_segment = new Segment(s.StartIndex,s.Length-2);
            if (to == SendTo.Everyone)
            {
                conn.room.Broadcast(messageType,delivery,F_MessageWriter.instance);
            }
            else if (to == SendTo.ExcludeSender)
            {
                conn.room.Broadcast(conn.ClientId,messageType,delivery, F_MessageWriter.instance);
            }
            else if (to == SendTo.RoomOwner)
            {
                conn.room.PTP(conn.room.CurrentAuthorityAt, messageType,delivery, F_MessageWriter.instance);
            }
            else if (to == SendTo.Server) { }
            else
            {
                conn.room.PTP(to.Target, messageType,delivery, F_MessageWriter.instance);
            }
        });
    }
    private class f_MessageWriter : MessageWriter
    {
        internal static f_MessageWriter instance=new();
        internal bool t_spawnMode;
        internal short t_spawnCollectionId;
        internal string t_spawnParam;
        internal short t_spawnIdStart;

        public int GetLength()
        {
            return 1 + 2 + StringSerializer.GetLength(t_spawnParam) + 2;
        }
        public bool Write(SendBuffer b)
        {
            return BoolSerializer.Serialize(t_spawnMode, b.bytes, ref b.indexStart)
                && ShortSerializer.Serialize(t_spawnCollectionId, b.bytes, ref b.indexStart)
                && StringSerializer.Serialize(t_spawnParam, b.bytes, ref b.indexStart)
                && ShortSerializer.Serialize(t_spawnIdStart, b.bytes, ref b.indexStart);
        }
        public MessageWriter Clone()
        {
            return new f_MessageWriter() { t_spawnMode=t_spawnMode,t_spawnCollectionId=t_spawnCollectionId,t_spawnParam=t_spawnParam,t_spawnIdStart=t_spawnIdStart};
        }
        public void Dispose()
        {

        }
    }
    protected static void Server_f()
    {
        //物体Id同步
        MessageHandlerServer.Regist(Header.f,(conn, b, s) =>
        {
            if (conn.room == null) return;
            byte messageType = MessageReader.Header(b, s);
            Delivery delivery = DeliverySource.IdToDelivery(MessageReader.Delivery(b,s));

            int indexStart = MessageReader.BodyIndexStart(s);
            int invalidIndex = MessageReader.BodyIndexInvalid(s);
            SendTo to =SendTo.To(ShortSerializer.Deserialize(b, ref indexStart, invalidIndex));
            f_MessageWriter.instance.t_spawnMode = BoolSerializer.Deserialize(b, ref indexStart, invalidIndex);
            f_MessageWriter.instance.t_spawnCollectionId =ShortSerializer.Deserialize(b, ref indexStart, invalidIndex);
            f_MessageWriter.instance.t_spawnParam = StringSerializer.Deserialize(b,ref indexStart, invalidIndex);
            f_MessageWriter.instance.t_spawnIdStart =ShortSerializer.Deserialize(b,ref indexStart, invalidIndex);
            if (!f_MessageWriter.instance.t_spawnMode)
            {
                //createdID+=spawnIdStart,spawnIdStart=createdID
                short t = conn.room.CreatedId;
                conn.room.CreatedId += f_MessageWriter.instance.t_spawnIdStart;
                f_MessageWriter.instance.t_spawnIdStart = t;
            }
            if (to == SendTo.Everyone)
            {
                conn.room.Broadcast(messageType, delivery, f_MessageWriter.instance);
            }
            else if (to == SendTo.ExcludeSender)
            {
                conn.room.Broadcast(conn.ClientId, messageType, delivery, f_MessageWriter.instance);
            }
            else if (to == SendTo.RoomOwner)
            {
                conn.room.PTP(conn.room.CurrentAuthorityAt, messageType, delivery, f_MessageWriter.instance);
            }
            else if (to == SendTo.Server) { }
            else
            {
                conn.room.PTP(to.Target, messageType,delivery, f_MessageWriter.instance);
            }
        });
    }
    private class Q_MessageWriter : MessageWriter
    {
        internal static Q_MessageWriter instance = new();
        internal string t_header;
        internal string t_content;

        public int GetLength()
        {
            return StringSerializer.GetLength(t_header) + StringSerializer.GetLength(t_content);
        }
        public bool Write(SendBuffer buffer)
        {
            return StringSerializer.Serialize(t_header, buffer.bytes, ref buffer.indexStart)
                    && StringSerializer.Serialize(t_content, buffer.bytes, ref buffer.indexStart);
        }
        public MessageWriter Clone()
        {
            return new Q_MessageWriter() { t_header=t_header,t_content=t_content};
        }
        public void Dispose()
        {

        }
    }
    protected static void Server_Q()
    {
        MessageHandlerServer.Regist(Header.Q,(conn, b, s) =>
        {
            int index = MessageReader.BodyIndexStart(s);
            int invalidIndex = MessageReader.BodyIndexInvalid(s);
            Q_MessageWriter.instance.t_header = StringSerializer.Deserialize(b, ref index, invalidIndex);
            Q_MessageWriter.instance.t_content = StringSerializer.Deserialize(b, ref index, invalidIndex);
            string reply = EnsServerRequest.OnRecvRequest(Q_MessageWriter.instance.t_header, Q_MessageWriter.instance.t_content, conn);
            if (reply == string.Empty)
            {
                Utils.Debug.LogError($"对{Q_MessageWriter.instance.t_header}:{Q_MessageWriter.instance.t_content}的响应为空");
                return;
            }
            Q_MessageWriter.instance.t_content = reply;
            conn.Send(Header.Q, Delivery.Reliable, Q_MessageWriter.instance);
        });
    }
}