using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 用于在服务器端也启动一个客户端<br></br>
/// 函数调用规则与ENCConnection一致
/// </summary>
internal class EnsHost : EnsConnection
{
    internal CircularQueue<byte[]> ReceivedData;
    private ENCLocalClient _client;
    private SendBuffer _buffer;

    internal static void Create(out EnsHost host,out ENCLocalClient client)
    {
        if (EnsInstance.Corr.Client != null)
        {
            Debug.LogError("[E]客户端已经启动");
            host = null;
            client = null;
            return;
        }
        client=new ENCLocalClient();
        EnsInstance.Corr.Client = client;
        host = new EnsHost(client);
        EnsInstance.Corr.Host = host;
    }
    internal EnsHost(ENCLocalClient client)
    {
        _client = client;
        ReceivedData=new CircularQueue<byte[]>(20);
        _buffer=new SendBuffer(OnSend);
        DeliverySource = DeliverySource.Get();
        ClientId = 0;
        EnsInstance.LocalClientId = ClientId;
        _on = true;
    }
    internal override void Send(byte messageType, Delivery delivery, MessageWriter writer = null)
    {
        Send(_buffer, messageType,DeliverySource.DeliveryToId(delivery), writer);
    }
    private void OnSend(byte[] bytes,int length)
    {
        var b=BytesPool.GetBuffer(length);
        Buffer.BlockCopy(bytes,0,b,0, length);
        _client.ReceivedData.Write(b);
    }
    internal override void Update()
    {
        var buffer = ReceivedData;
        while (buffer.Read(out var data) && _on)
        {
            ExtractData(data);
            foreach (var part in segments)
            {
                try
                {
                    MessageHandlerServer.Invoke(this, data, part);
                }
                catch (Exception e)
                {
                    Utils.Debug.ErrorCaught(e);
                }
                if (!_on) break;
            }
            segments.Clear();
            BytesPool.ReturnBuffer(data);
        }
    }
    internal override void FlushSendBuffer()
    {
        _buffer.Flush();
    }
    internal override void ShutDown()
    {
        _on = false;
        ReceivedData= null;
        _client = null;
        _buffer = null;
        DeliverySource.Return(DeliverySource);
        DeliverySource = null;
    }
}