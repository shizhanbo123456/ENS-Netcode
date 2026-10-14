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
            int index = MessageReader.BodyIndexStart(s);
            int invalidIndex = MessageReader.BodyIndexInvalid(s);
            var i=ShortSerializer.Deserialize(b,ref index, invalidIndex);
            EnsInstance.LocalClientId = i;
            EnsInstance.OnServerConnect.Invoke();
        });
    }
    protected static void Client_E()
    {
        //事件
        MessageHandlerClient.Regist(Header.Esc, (b,s) =>
        {
            int index = MessageReader.BodyIndexStart(s);
            int invalidIndex = MessageReader.BodyIndexInvalid(s);
            var e = ByteSerializer.Deserialize(b, ref index, invalidIndex);
            var i = ShortSerializer.Deserialize(b, ref index, invalidIndex);
            if (e == 1) EnsInstance.OnClientEnter?.Invoke(i);
            else if (e == 2) EnsInstance.OnClientExit?.Invoke(i);
            else Debug.LogError("[E]存在错误的事件消息");
        });
    }
    protected static void Client_H()
    {
        MessageHandlerClient.Regist(Header.Hsc,(b,s) =>
        {
            int index = MessageReader.BodyIndexStart(s);
            int invalidIndex = MessageReader.BodyIndexInvalid(s);
            //回包回显的是本机发送时刻，据此换算单程延迟
            int clientMillisecond = IntSerializer.Deserialize(b, ref index, invalidIndex);
            EnsInstance.LocalClientDelay = ((int)(Time.time * 1000) - clientMillisecond) / 2;
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
            int index = MessageReader.BodyIndexStart(s);
            int invalidIndex = MessageReader.BodyIndexInvalid(s);
            EnsInstance.HasAuthority = BoolSerializer.Deserialize(b, ref index, invalidIndex);
            EnsInstance.OnAuthorityChanged?.Invoke();
        });
    }
    protected static void Client_F()
    {
        MessageHandlerClient.Regist(Header.Fsc,(b,s) =>
        {
            int indexStart = MessageReader.BodyIndexStart(s);
            int invalidIndex = MessageReader.BodyIndexInvalid(s);
            short id=ShortSerializer.Deserialize(b, ref indexStart, invalidIndex);
            EnsBehaviour obj = EnsNetworkObjectManager.GetObject(id);
            if (obj == null)
            {
                if (EnsInstance.DevelopmentDebug) Debug.LogError("未找到id为" + id + "的物体");
                return;
            }
            if(!obj.InvokeFunc(b, new Segment(s.StartIndex + MessageReader.BodyOffset+2, s.Length - MessageReader.BodyOffset-2)))//额外排除物体id部分
            {
                Debug.LogError("检测到未注册的函数");
                Utils.Debug.PrintBytes(b,s);
            }
        });
    }
    protected static void Client_f()
    {
        MessageHandlerClient.Regist(Header.fsc,(b, s) =>
        {
            int indexStart = MessageReader.BodyIndexStart(s);
            int invalidIndex = MessageReader.BodyIndexInvalid(s);
            bool t_spawnMode = BoolSerializer.Deserialize(b, ref indexStart, invalidIndex);
            short t_spawnCollectionId = ShortSerializer.Deserialize(b, ref indexStart, invalidIndex);
            string t_spawnParam = StringSerializer.Deserialize(b, ref indexStart, invalidIndex);
            short t_spawnIdStart = ShortSerializer.Deserialize(b, ref indexStart, invalidIndex);
            if(t_spawnMode)EnsInstance.EnsSpawner.RespawnCheckLocal(t_spawnCollectionId,t_spawnParam,t_spawnIdStart);
            else EnsInstance.EnsSpawner.CreateLocal(t_spawnCollectionId,t_spawnParam,t_spawnIdStart);
        });
    }
    protected static void Client_Q()
    {
        MessageHandlerClient.Regist(Header.Qsc,(b,s) =>
        {
            int index = MessageReader.BodyIndexStart(s);
            int invalidIndex = MessageReader.BodyIndexInvalid(s);
            string header = StringSerializer.Deserialize(b, ref index, invalidIndex);
            string cotent = StringSerializer.Deserialize(b, ref index, invalidIndex);
            EnsClientRequest.RecvReply(header, cotent);
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
            int index = MessageReader.BodyIndexStart(s);
            int invalidIndex = MessageReader.BodyIndexInvalid(s);
            int header = IntSerializer.Deserialize(b, ref index, invalidIndex);
            string content = StringSerializer.Deserialize(b, ref index, invalidIndex);
            ClientRoomManagerEventCenter.TrigEvent(header, content);
        });
    }
    protected static void Client_N()
    {
        MessageHandlerClient.Regist(Header.Nsc, (b, s) =>
        {
            int index = MessageReader.BodyIndexStart(s);
            int invalidIndex = MessageReader.BodyIndexInvalid(s);
            int header = IntSerializer.Deserialize(b, ref index, invalidIndex);
            string content = StringSerializer.Deserialize(b, ref index, invalidIndex);
            ClientRoomEventCenter.TrigEvent(header, content);
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
