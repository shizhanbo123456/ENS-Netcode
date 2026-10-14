using Ens.ProtoFormat;
using ProtocolWrapper;
using System;
using System.Collections.Generic;

/// <summary>
/// 服务器使用，用于简化和客户端的通信
/// </summary>
public class EnsConnection:DataTransportBase
{
    public short ClientId;
    private ProtocolBase Connection;
    public EnsRoom room;

    private Action<EnsConnection> OnShutDown;

    protected bool _on;
    private bool shutdownStarted;

    protected EnsConnection() { }
    internal EnsConnection(ProtocolBase _base,short index,Action<EnsConnection>onShutDown)
    {
        Connection = _base;
        ClientId = index;
        OnShutDown = onShutDown;
        _on= true;

        KeyLibrary = new KeyLibrary(Connection.SendBuffer, DeliverySource);

        Send(Delivery.Reliable, new MessageBody_C_SC { clientId = ClientId }, Pkt.scC);
    }
    internal override SendBuffer GetSendBuffer()
    {
        if (!_on || KeyLibrary == null) return null;
        return Connection.SendBuffer;
    }
    internal override void Update()
    {
        if (Connection == null) return;
        if (Connection.TransportClosed)
        {
            ShutDown();
            return;
        }
        var buffer=Connection.ReceiveBuffer;
        while (buffer.Read(out var data)&&_on)
        {
            ExtractData(data);
            foreach (var part in segments) 
            {
                try
                {
                    KeyLibrary.OnRecvData(data, part, out bool skip);
                    if (skip) continue;
                    MessageHandlerServer.Invoke(this, data, part);
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
        Connection?.SendBuffer?.Flush();
    }
    internal override void ShutDown()
    {
        if (shutdownStarted) return;
        shutdownStarted = true;
        _on = false;
        var connection = Connection;
        if (room != null)
        {
            EnsRoomManager.Instance.ExitRoom(this, out int _);
        }
        OnShutDown?.Invoke(this);
        try
        {
            if (connection != null && connection.Initialized && !connection.Cancelled)
            {
                // _on 已关闭，直接写帧发送最后一个断联通知。
                DataTransportBase.SendFrame(connection.SendBuffer, DeliverySource.DeliveryToId(Delivery.Unreliable), default(MessageBody_D_SC), Pkt.scD);
                connection.SendBuffer.Flush();
            }
        }
        catch (Exception e)
        {
            Utils.Debug.ErrorCaught(e);
        }
        KeyLibrary?.Clear();
        base.ShutDown();
        connection?.ShutDown();
        connection?.Dispose();
        Connection = null;
        KeyLibrary = null;
        room = null;
    }
    internal override ProtocolBase GetProtocolBase()
    {
        return Connection;
    }
}
