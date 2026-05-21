using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using UnityEngine;

public class EnsCorrespondent :MonoBehaviour
{
    [Tooltip("The ip of the server which you gonna connect")]
    public string IP = "127.0.0.1";
    [Tooltip("The port of connections, for both server and clients")]
    public int Port = 65432;
    public enum NetworkMode
    {
        None, Client, Host
    }
    [Tooltip("Which character the current game is at")]
    public NetworkMode networkMode;

    [Tooltip("In which way the client would receive data multithreading or asynchronous")]
    public ProtocolWrapper.ConcurrentType recvMode;
    [Tooltip("Which protocol would be used, make sure it uses the same protocol as the server")]
    public ProtocolWrapper.ProtocolType defaultProtocol;

    [Space]
    [Tooltip("How long the local key will exist, it includes sending and ignoring response")]
    public float KeyExistTime = 5f;//关键信息忽略时长
    [Tooltip("The interval of unconfirmed keys to send")]
    public float KeySendInterval = 0.2f;//未确认的关键信息发送时长
    [Tooltip("How long will you ignore key messages after confirmed")]
    public float RKeyExistTime = 5f;//返回的关键信息忽略时长

    [Tooltip("The interval of strive keys to send")]
    public float StriveKeySendInterval = 0.01f;
    [Tooltip("How many times to resend strive keys")]
    public int StriveKeyResendCount = 3;

    [Space]
    public bool DevelopmentDebug = true;

    [Space]
    /// <summary>
    /// 上次接收心跳检测时间超过此阈值会认为断开了连接
    /// </summary>
    [Tooltip("How long will a connection be reset since the last message received")]
    public float DisconnectThreshold = 3f;
    /// <summary>
    /// 发送心跳检测消息的间隔
    /// </summary>
    [Tooltip("The interval to send heartbeat message")]
    public float HeartbeatMsgInterval = 0.2f;


    internal EnsServer Server;
    internal EnsClient Client;
    internal EnsHost Host;

    protected virtual void OnValidate()
    {
        if (DisconnectThreshold <= HeartbeatMsgInterval) DisconnectThreshold = HeartbeatMsgInterval + 0.1f;
    }

    private void Awake()
    {
        EnsInstance.Corr = this;

        if(TryGetComponent<Ens.DefaultProtocol.ProtocolBase>(out var component))
        {
            Debug.Log($"Use transport:{component.GetType()}");
            ProtocolWrapper.Protocol.GetClientFunc = component.GetProtocolBase;
            ProtocolWrapper.Protocol.GetListenerFunc=component.GetListener;
        }
        else
        {
            Debug.Log($"Use default transport:{defaultProtocol}");
        }

        EnsInstance.DevelopmentDebug = DevelopmentDebug;

        EnsInstance.ReliableKeyExistTime = KeyExistTime;
        EnsInstance.UnconfirmedKeySendInterval = KeySendInterval;
        EnsInstance.ReceiverKeyExistTime = RKeyExistTime;

        EnsInstance.DisconnectThreshold = DisconnectThreshold;
        EnsInstance.HeartbeatMsgInterval = HeartbeatMsgInterval;
        EnsInstance.StriveKeySendInterval = StriveKeySendInterval;
        EnsInstance.StriveKeyResendCount = StriveKeyResendCount;

        ProtocolWrapper.Protocol.mode = recvMode;
        ProtocolWrapper.Protocol.defaultProtocol = defaultProtocol;

        EnsClientEventRegister.RegistUnity();

        Loop.InitClient();
        Loop.InitCommon();
    }
    protected void UpdateServerAndClient()//Clear send buffer and handle recv buffer
    {
        if (networkMode == NetworkMode.Host)
        {
            Server.Update();
            Server.FlushSendBuffer();
        }
        if (networkMode == NetworkMode.Host || networkMode == NetworkMode.Client)
        {
            Client.Update();
            Client?.FlushSendBuffer();
        }
    }
    private void Update()
    {
        if (networkMode != NetworkMode.None)
        {
            foreach (var p in EnsNetworkObjectManager.GetPriority().ToArray())//创建副本避免因修改产生错误
            {
                EnsNetworkObjectManager.Update(p);
                Client.FlushSendBuffer();
            }
            UpdateServerAndClient();
        }
        Loop.LoopCommon();
        Loop.LoopClient();
    }
    protected virtual void FixedUpdate()
    {
        if (networkMode != NetworkMode.None)
        {
            foreach (var p in EnsNetworkObjectManager.GetFixedPriority().ToArray())//创建副本避免因修改产生错误
            {
                EnsNetworkObjectManager.FixedUpdate(p);
                Client.FlushSendBuffer();
            }
        }
    }
    public void StartHost()
    {
        if (networkMode != NetworkMode.None)
        {
            Debug.LogWarning("已启动，关闭后才可调用");
            return;
        }
        if (!IPAddress.TryParse(IP, out _) || Port < 0 || Port > 65535)
        {
            Debug.Log("输入的IP或端口有误");
            return;
        }

        networkMode = NetworkMode.Host;
        EnsHost.Create(out var host, out var client);
        Server = new EnsServer(IPAddress.Any,Port);
        Server.ClientConnections.Add(host.ClientId,host);
        EnsInstance.OnServerConnect.Invoke();
    }
    public void StartClient()
    {
        if (networkMode != NetworkMode.None)
        {
            Debug.LogWarning("已启动，关闭后才可调用");
            return;
        }
        if (!IPAddress.TryParse(IP, out _) || Port < 0 || Port > 65535)
        {
            Debug.Log("输入的IP或端口有误");
            return;
        }

        try
        {
            EnsInstance.ClientConnectRejected = true;
            networkMode = NetworkMode.Client;
            Client = new EnsClient(IP, Port);
        }
        catch (Exception e)
        {
            Debug.LogError("客户端启动失败，IP=" + IP + " Port=" + Port + " Log:" + e.ToString());
        }
    }
    public void SetServerListening(bool listening)
    {
        if (Server == null)
        {
            Debug.LogError("未启动服务器");
            return;
        }
        if(listening)Server.StartListening();
        else Server.EndListening();
    }

    public virtual void ShutDown()
    {
        try
        {
            if (networkMode == NetworkMode.Client)
            {
                if (Client != null)
                {
                    Client.ShutDown();
                }
            }
            else if (networkMode == NetworkMode.Host)
            {
                if (Server != null)//关闭Server->关闭Host
                {
                    Server.ShutDown();
                }
                if (Client != null)
                {
                    Client.ShutDown();
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
        finally
        {
            Server = null;
            Client = null;
            Server = null;
        }
        networkMode = NetworkMode.None;
        if (EnsInstance.ClientConnectRejected)
        {
            EnsInstance.OnConnectionRejected?.Invoke();
            EnsInstance.ClientConnectRejected = false;
        }
        if (!EnsInstance.RoomExitInvoke)
        {
            EnsInstance.OnExitRoom.Invoke();
        }
        if (!EnsInstance.ServerDisconnectInvoke)
        {
            EnsInstance.LocalClientId = -1;
            EnsInstance.OnServerDisconnect?.Invoke();
        }
        EnsInstance.HasAuthority = false;
        EnsInstance.PresentRoomId = 0;
    }

    private void OnApplicationQuit()
    {
        ShutDown();
    }
}
