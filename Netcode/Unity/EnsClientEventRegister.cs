using Ens.ProtoFormat;
using System;
using Utils;
using request = Ens.Request;
public class EnsClientEventRegister
{
    private static bool registered;
    public static void RegistUnity()
    {
        if (registered) return;
        registered = true;
        EnsServerEventRegister.RegistDedicateServer();

        Client_C();
        Client_E();

        Client_A();
        Client_H();
        Client_D();
        Client_F();
        Client_f();
        Client_Q();
        Client_R();
        Client_M();
        Client_N();
        Client_Any();
        RegistClientRequests();

        ForceInvokeOnce_Server();
        ForceInvokeOnce_Room();
    }
    private static void RegistClientRequests()
    {
        EnsClientRequest.RegistRequest(new request.Client.CreateRoom());
        EnsClientRequest.RegistRequest(new request.Client.SetRule());
        EnsClientRequest.RegistRequest(new request.Client.JoinRoom());
        EnsClientRequest.RegistRequest(new request.Client.GetRule());
        EnsClientRequest.RegistRequest(new request.Client.ExitRoom());
        EnsClientRequest.RegistRequest(new request.Client.GetRoomList());
        EnsClientRequest.RegistRequest(new request.Client.SetInfo());
        EnsClientRequest.RegistRequest(new request.Client.GetInfo());
    }
    protected static void Client_Any()
    {
        MessageHandlerClient.RegistAny((datb,s) =>
        {
            EnsInstance.Corr.Client.hbRecvTime=Time.time+EnsInstance.DisconnectThreshold;
        });
    }
    protected static void Client_C()
    {
        //成功连接
        MessageHandlerClient.Regist(Header.Csc,(b,s) =>
        {
            EnsInstance.LocalClientId = Pkt.scC.FromSegment(b, s).clientId;
            EnsInstance.OnServerConnect.Invoke();
        });
    }
    protected static void Client_E()
    {
        //事件
        MessageHandlerClient.Regist(Header.Esc, (b,s) =>
        {
            var body = Pkt.scE.FromSegment(b, s);
            if (body.eventId == 1) EnsInstance.OnClientEnter?.Invoke(body.clientId);
            else if (body.eventId == 2) EnsInstance.OnClientExit?.Invoke(body.clientId);
            else Debug.LogError("[E]存在错误的事件消息");
        });
    }
    protected static void Client_H()
    {
        MessageHandlerClient.Regist(Header.Hsc,(b,s) =>
        {
            //回包回显的是本机发送时刻，据此换算单程延迟
            var body = Pkt.scH.FromSegment(b, s);
            EnsInstance.LocalClientDelay = ((int)(Time.time * 1000) - body.clientMillisecond) / 2;
        });
    }
    protected static void Client_D()
    {
        MessageHandlerClient.Regist(Header.Dsc,(b,s) =>
        {
            EnsInstance.Corr.ShutDown();
        });
    }
    protected static void Client_A()
    {
        MessageHandlerClient.Regist(Header.Asc,(b,s) =>
        {
            EnsInstance.HasAuthority = Pkt.scA.FromSegment(b, s).authorityActive;
            EnsInstance.OnAuthorityChanged?.Invoke();
        });
    }
    protected static void Client_F()
    {
        MessageHandlerClient.Regist(Header.Fsc,(b,s) =>
        {
            var body = Pkt.scF.FromSegment(b, s);
            EnsBehaviour obj = EnsNetworkObjectManager.GetObject(body.NOMObjectId);
            if (obj == null)
            {
                if (EnsInstance.DevelopmentDebug) Debug.LogError("未找到id为" + body.NOMObjectId + "的物体");
                return;
            }
            if(!obj.InvokeFunc(body.func, body.param.data, body.param.segment))
            {
                Debug.LogError("检测到未注册的函数");
                Utils.Debug.PrintBytes(body.param.data, body.param.segment);
            }
        });
    }
    protected static void Client_f()
    {
        MessageHandlerClient.Regist(Header.fsc,(b, s) =>
        {
            var body = Pkt.scf.FromSegment(b, s);
            if(body.spawnMode)EnsInstance.EnsSpawner.RespawnCheckLocal(body.collectionId, body.param, body.idStart);
            else EnsInstance.EnsSpawner.CreateLocal(body.collectionId, body.param, body.idStart);
        });
    }
    protected static void Client_Q()
    {
        MessageHandlerClient.Regist(Header.Qsc,(b,s) =>
        {
            var body = Pkt.scQ.FromSegment(b, s);
            EnsClientRequest.RecvReply(body.header, body.content);
        });
    }
    protected static void Client_R()
    {
        MessageHandlerClient.Regist(Header.Rsc, (b,s) =>
        {
            Ens.Request.Client.ExitRoom.OnRecvReply?.Invoke();
        });
    }
    protected static void Client_M()
    {
        MessageHandlerClient.Regist(Header.Msc, (b, s) =>
        {
            var body = Pkt.scM.FromSegment(b, s);
            ClientRoomManagerEventCenter.TrigEvent(body.type, body.content);
        });
    }
    protected static void Client_N()
    {
        MessageHandlerClient.Regist(Header.Nsc, (b, s) =>
        {
            var body = Pkt.scN.FromSegment(b, s);
            ClientRoomEventCenter.TrigEvent(body.type, body.content);
        });
    }

    protected static void ForceInvokeOnce_Server()
    {
        EnsInstance.OnServerConnect += () =>
        {
            EnsInstance.ServerDisconnectInvoke = false;
            EnsInstance.ClientConnectRejected = false;
        };
        EnsInstance.OnServerDisconnect += () =>
        {
            EnsInstance.ServerDisconnectInvoke = true;
        };
    }
    protected static void ForceInvokeOnce_Room()
    {
        EnsInstance.OnCreateRoom += () =>
        {
            EnsInstance.RoomExitInvoke = false;
        };
        EnsInstance.OnJoinRoom += () =>
        {
            EnsInstance.RoomExitInvoke = false;
        };
        EnsInstance.OnExitRoom += () =>
        {
            EnsInstance.RoomExitInvoke = true;
        };
    }
}
