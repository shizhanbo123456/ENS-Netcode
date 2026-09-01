using System;

/// <summary>
/// ENCLocalClient和ENCHost一起使用<br></br>
/// 在调用StartHost时由ENCHost创建
/// 函数调用规则与ENCClient一致
/// </summary>
internal class ENCLocalClient : EnsClient
{
    internal CircularQueue<byte[]> ReceivedData;
    private SendBuffer _buffer;
    private bool shutdownStarted;
    public ENCLocalClient()
    {
        _on = true;
        ReceivedData=new CircularQueue<byte[]>(20);
        _buffer = new SendBuffer(OnSend);
        DeliverySource = DeliverySource.Get();
    }
    internal override void Send(byte messageType, Delivery delivery, MessageWriter writer = null)
    {
        if (!_on || _buffer == null || DeliverySource == null) return;
        Send(_buffer, messageType, DeliverySource.DeliveryToId(delivery), writer);
    }
    private void OnSend(byte[] bytes, int length)
    {
        var b = BytesPool.GetBuffer(length);
        Buffer.BlockCopy(bytes, 0, b, 0, length);
        EnsInstance.Corr.Host.ReceivedData.Write(b);
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
                    MessageHandlerClient.Invoke(data, part);
                }
                catch(Exception e)
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
        if (shutdownStarted) return;
        shutdownStarted = true;
        _on = false;
        while (ReceivedData != null && ReceivedData.Read(out var data))
            BytesPool.ReturnBuffer(data);
        _buffer?.Dispose();
        ReceivedData = null;
        _buffer = null;
        if (DeliverySource != null) DeliverySource.Return(DeliverySource);
        DeliverySource = null;
    }
}
