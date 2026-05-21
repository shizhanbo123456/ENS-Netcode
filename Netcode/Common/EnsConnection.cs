using ProtocolWrapper;
using System;
using System.Collections.Generic;

/// <summary>
/// 服务器使用，用于简化和客户端的通信
/// </summary>
public class EnsConnection:DataTransportBase
{
    internal short ClientId;
    private KeyLibrary KeyLibrary;
    private ProtocolBase Connection;
    internal EnsRoom room;

    private Action<EnsConnection> OnShutDown;

    internal int delay = 20;//20ms

    protected bool _on;

    protected EnsConnection() { }
    internal EnsConnection(ProtocolBase _base,short index,Action<EnsConnection>onShutDown)
    {
        Connection = _base;
        ClientId = index;
        OnShutDown = onShutDown;
        _on= true;

        KeyLibrary = new KeyLibrary(Connection.SendBuffer, DeliverySource);

        ClientIdWriter.instance.currentClientId = ClientId;
        Send(Header.C, Delivery.Reliable, ClientIdWriter.instance);
    }
    private class ClientIdWriter : MessageWriter
    {
        internal static ClientIdWriter instance = new();
        internal short currentClientId;
        public int GetLength()
        {
            return sizeof(short); // 等价 return 2;
        }
        public bool Write(SendBuffer buffer)
        {
            return ShortSerializer.Serialize(currentClientId, buffer.bytes, ref buffer.indexStart);
        }
        public MessageWriter Clone()
        {
            return new ClientIdWriter() { currentClientId=currentClientId};
        }
        public void Dispose()
        {
            
        }
    }
    internal override void Send(byte messageType, Delivery delivery, MessageWriter writer = null)
    {
        KeyLibrary.OnSend(messageType,delivery,writer);
    }
    internal override void Update()
    {
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
        Connection.SendBuffer.Flush();
    }
    internal override void ShutDown()
    {
        if (Connection==null||Connection.Cancelled) return;
        OnShutDown?.Invoke(this);
        Send(Header.D,Delivery.Unreliable);
        Connection.SendBuffer.Flush();

        _on = false;
        base.ShutDown();
        KeyLibrary.Clear();
        Connection.ShutDown();
        Connection?.Dispose();
        Connection = null;
        KeyLibrary = null;
        room = null;
    }
    internal override ProtocolBase GetProtocolBase()
    {
        return Connection;
    }
}
