using Ens.ProtoFormat;
using ProtocolWrapper;
using System;
using Utils;

/// <summary>
/// 实例化时启动客户端
/// </summary>
internal class EnsClient:DataTransportBase
{
    private ProtocolBase Client;

    private float heartbeatSendTime;
    private bool shutdownStarted;

    protected bool _on;

    protected EnsClient(){ }
    internal EnsClient(string ip,int port)
    {
        Client = Protocol.GetClient(ip,port);
        if (Client == null) throw new InvalidOperationException("客户端传输创建失败");
        EnsureInitialized();

        _on = true;
    }
    private void EnsureInitialized()
    {
        if (KeyLibrary == null && Client != null && Client.Initialized)
            KeyLibrary = new KeyLibrary(Client.SendBuffer, DeliverySource);
    }
    internal override SendBuffer GetSendBuffer()
    {
        EnsureInitialized();
        if (Client == null || !Client.Initialized || KeyLibrary == null)
        {
            Debug.LogWarning("客户端初始化中");
            return null;
        }
        return Client.SendBuffer;
    }
    //发送心跳包(携带本机时间戳)
    protected void SendHeartbeat()
    {
        hbSendTime = Time.time + EnsInstance.HeartbeatMsgInterval;
        Send(Delivery.Unreliable, new MessageBody_H_CS { clientMillisecond = (int)(Time.time * 1000) }, Pkt.csH);
    }
    internal override void Update()
    {
        if (Client == null) return;
        if (Client.TransportClosed)
        {
            EnsInstance.Corr.ShutDown();
            return;
        }
        EnsureInitialized();
        if (Time.time>hbRecvTime)
        {
            EnsInstance.Corr.ShutDown();
            return;
        }
        if (Time.time>hbSendTime)
        {
            SendHeartbeat();
        }
        if (!Client.Initialized || KeyLibrary == null) return;
        var buffer = Client.ReceiveBuffer;
        while (buffer.Read(out var data)&&_on)
        {
            ExtractData(data);
            foreach (var part in segments)
            {
                try
                {
                    KeyLibrary.OnRecvData(data, part, out bool skip);
                    if (skip) continue;
                    MessageHandlerClient.Invoke(data, part);
                }
                catch (Exception e)
                {
                    Utils.Debug.ErrorCaught(e);
                }
                if (!_on) break;
            }
            segments.Clear();
        }
        if(_on)KeyLibrary.Update();
    }
    internal override void FlushSendBuffer()
    {
        if (Client == null || !Client.Initialized || Client.SendBuffer == null) return;
        Client.SendBuffer.Flush();
    }
    internal override void ShutDown()
    {
        if (shutdownStarted) return;
        shutdownStarted = true;
        _on = false;
        var client = Client;
        try
        {
            if (client != null && client.Initialized && !client.Cancelled)
            {
                DataTransportBase.SendFrame(client.SendBuffer, DeliverySource.DeliveryToId(Delivery.Unreliable), default(MessageBody_D_CS), Pkt.csD);
                client.SendBuffer?.Flush();
            }
        }
        catch (Exception e)
        {
            Utils.Debug.ErrorCaught(e);
        }
        KeyLibrary?.Clear();
        base.ShutDown();
        client?.ShutDown();
        client?.Dispose();
        Client = null;
        KeyLibrary = null;
    }
    internal override ProtocolBase GetProtocolBase()
    {
        return Client;
    }
}
