using Ens.ProtoFormat;
using System;
using System.Collections.Generic;
using Utils;
using request = Ens.Request;
public class EnsServerEventRegister
{
    private static bool registered;
    public static void RegistDedicateServer()
    {
        if (registered) return;
        registered = true;
        Server_A();
        Server_H();
        Server_D();
        Server_F();
        Server_f();
        Server_Q();
        Server_M();
        Server_N();
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
        //回显客户端时间戳，由客户端自行换算延迟
        MessageHandlerServer.Regist(Header.Hcs,(conn, b, s) =>
        {
            var body = Pkt.csH.FromSegment(b, s);
            conn.Send(Delivery.Unreliable, new MessageBody_H_SC { clientMillisecond = body.clientMillisecond }, Pkt.scH);
        });
    }
    protected static void Server_D()
    {
        MessageHandlerServer.Regist(Header.Dcs,(conn, b, s) =>
        {
            conn.ShutDown();
        });
    }

    protected static void Server_A()
    {
        MessageHandlerServer.Regist(Header.Acs,(conn, b, s) =>
        {
            if (conn.room == null) return;
            conn.room.SetAuthority(Pkt.csA.FromSegment(b, s).clientId);
        });
    }
    protected static void Server_F()
    {
        MessageHandlerServer.Regist(Header.Fcs,(conn, b, s) =>
        {
            if (conn.room == null) return;
            var body = Pkt.csF.FromSegment(b, s);
            SendTo to = SendTo.To(body.target);
            Delivery delivery = DeliverySource.IdToDelivery(body.delivery);
            var outBody = new MessageBody_F_SC { NOMObjectId = body.NOMObjectId, func = body.func, param = body.param };
            if (to == SendTo.Everyone)
            {
                conn.room.Broadcast(delivery, outBody, Pkt.scF);
            }
            else if (to == SendTo.ExcludeSender)
            {
                conn.room.Broadcast(conn.ClientId, delivery, outBody, Pkt.scF);
            }
            else if (to == SendTo.RoomOwner)
            {
                conn.room.PTP(conn.room.CurrentAuthorityAt, delivery, outBody, Pkt.scF);
            }
            else if (to == SendTo.Server)
            {
                InvokeServerRpcLocal(body);
            }
            else
            {
                conn.room.PTP(to.Target, delivery, outBody, Pkt.scF);
            }
        });
    }
    private static void InvokeServerRpcLocal(MessageBody_F_CS body)
    {
        EnsBehaviour obj = EnsNetworkObjectManager.GetObject(body.NOMObjectId);
        if (obj == null)
        {
            if (EnsInstance.DevelopmentDebug) UnityEngine.Debug.LogError("未找到id为" + body.NOMObjectId + "的物体");
            return;
        }
        if (!obj.InvokeFunc(body.func, body.param.data, body.param.segment))
        {
            UnityEngine.Debug.LogError("检测到未注册的函数");
            Utils.Debug.PrintBytes(body.param.data, body.param.segment);
        }
    }
    protected static void Server_f()
    {
        //物体Id同步
        MessageHandlerServer.Regist(Header.fcs,(conn, b, s) =>
        {
            if (conn.room == null) return;
            var body = Pkt.csf.FromSegment(b, s);
            Delivery delivery = DeliverySource.IdToDelivery(body.delivery);

            short idStart = body.idStart ?? 0;
            if (!body.spawnMode)
            {
                //createdID+=behaviourCount,idStart=createdID
                idStart = conn.room.CreatedId;
                conn.room.CreatedId += body.behaviourCount ?? 0;
            }
            var outBody = new MessageBody_f_SC { spawnMode = body.spawnMode, collectionId = body.collectionId, param = body.param, idStart = idStart };
            SendTo to = SendTo.To(body.sendto);
            if (to == SendTo.Everyone)
            {
                conn.room.Broadcast(delivery, outBody, Pkt.scf);
            }
            else if (to == SendTo.ExcludeSender)
            {
                conn.room.Broadcast(conn.ClientId, delivery, outBody, Pkt.scf);
            }
            else if (to == SendTo.RoomOwner)
            {
                conn.room.PTP(conn.room.CurrentAuthorityAt, delivery, outBody, Pkt.scf);
            }
            else if (to == SendTo.Server)
            {
                //只在服务器本地生成
                if (EnsInstance.EnsSpawner != null)
                {
                    if (outBody.spawnMode) EnsInstance.EnsSpawner.RespawnCheckLocal(outBody.collectionId, outBody.param, outBody.idStart);
                    else EnsInstance.EnsSpawner.CreateLocal(outBody.collectionId, outBody.param, outBody.idStart);
                }
            }
            else
            {
                conn.room.PTP(to.Target, delivery, outBody, Pkt.scf);
            }
        });
    }
    protected static void Server_Q()
    {
        MessageHandlerServer.Regist(Header.Qcs,(conn, b, s) =>
        {
            var body = Pkt.csQ.FromSegment(b, s);
            string reply = EnsServerRequest.OnRecvRequest(body.header, body.content, conn);
            if (reply == string.Empty)
            {
                Utils.Debug.LogError($"对{body.header}:{body.content}的响应为空");
                return;
            }
            conn.Send(Delivery.Reliable, new MessageBody_Q_SC { header = body.header, content = reply }, Pkt.scQ);
        });
    }
    protected static void Server_M()
    {
        MessageHandlerServer.Regist(Header.Mcs, (conn,b, s) =>
        {
            var body = Pkt.csM.FromSegment(b, s);
            EnsRoomManager.Instance.RecvEvent(conn, body.type, body.content);
        });
    }
    protected static void Server_N()
    {
        MessageHandlerServer.Regist(Header.Ncs,(conn,b, s) =>
        {
            if (conn.room == null) return;
            var body = Pkt.csN.FromSegment(b, s);
            conn.room.RecvEvent(body.type, body.content);
        });
    }
}
