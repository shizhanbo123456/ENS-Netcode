using System;
using System.Collections.Generic;
using Utils;

internal class KeyLibrary
{
    //A发送，B收到消息后返回的校验消息中，Header不变，DeliveryId不变，body省略

    //Unreliable:发送一次，不等待回应
    //Strive:发送指定数量次，不等待回应，接收方会忽略因重发重复的消息
    //Reliable:发送多次，直至收到回复或超时，接收方会忽略因重发重复的消息
    //OrderWise:发送多次，直至收到回复或超时，接收方会忽略因重发重复的消息。只会发送储存的未确认的最早记录的此类消息，收到回复或超时后才会发送下一个

    public enum KeyState
    {
        TobeConfirmed, ConfirmedExisting, End
    }
    private class StriveKey
    {
        private static ObjectPool<StriveKey> Pool = new(() => new());

        public float NextSendTime;
        public int SendCountLeft;

        public byte messageType;
        public ushort delivery;
        public MessageWriter writer;

        private StriveKey() { }
        public static StriveKey Get(byte messageType, ushort delivery, MessageWriter writer)
        {
            var k = Pool.Get();
            k.NextSendTime = EnsInstance.StriveKeySendInterval+Time.time;
            k.SendCountLeft = EnsInstance.StriveKeyResendCount;

            k.messageType = messageType;
            k.delivery = delivery;
            k.writer = writer;

            return k;
        }
        public static void Return(StriveKey key)
        {
            Pool.Return(key);
        }
    }
    private class ReliableKey
    {
        private static ObjectPool<ReliableKey> Pool = new(() => new());

        public KeyState State;
        public float ToConfirmIntervalLeft;
        public float ConfirmedExistingSource;

        public byte messageType;
        public ushort delivery;
        public MessageWriter writer;

        protected ReliableKey() { }
        public static ReliableKey Get(byte messageType,  ushort delivery, MessageWriter writer)
        {
            var k = Pool.Get();
            k.State = KeyState.TobeConfirmed;
            k.ToConfirmIntervalLeft  = Time.time + EnsInstance.UnconfirmedKeySendInterval;
            k.ConfirmedExistingSource = EnsInstance.ReliableKeyExistTime + Time.time;

            k.messageType = messageType;
            k.delivery = delivery;
            k.writer = writer;

            return k;
        }
        public static void Return(ReliableKey key)
        {
            Pool.Return(key);
        }
    }
    private class OrderwiseKey
    {
        private static ObjectPool<OrderwiseKey> Pool = new(() => new());

        public int OrderwiseIndex;

        public KeyState State;
        public float ToConfirmIntervalLeft;
        public float ConfirmedExistingSource;

        public byte messageType;
        public ushort delivery;
        public MessageWriter writer;
        protected OrderwiseKey():base() { }
        public static OrderwiseKey Get(int orderwiseIndex,byte messageType, ushort delivery, MessageWriter writer)
        {
            var k = Pool.Get();
            k.OrderwiseIndex= orderwiseIndex;

            k.State = KeyState.TobeConfirmed;
            k.ToConfirmIntervalLeft = Time.time + EnsInstance.UnconfirmedKeySendInterval;
            k.ConfirmedExistingSource = EnsInstance.ReliableKeyExistTime + Time.time;

            k.messageType = messageType;
            k.delivery = delivery;
            k.writer = writer;

            return k;
        }
        public static void Return(OrderwiseKey key)
        {
            Pool.Return(key);
        }
    }
    private Dictionary<ushort, StriveKey> StriveKeys = new();
    private Dictionary<ushort,ReliableKey> ReliableKeys = new();
    private Dictionary<ushort,OrderwiseKey> OrderedKeys = new();
    private Dictionary<ushort, float> RespKeys = new();//value=EndTime
    private int OrderedIndexSource = 0;

    private readonly SendBuffer buffer;
    private readonly DeliverySource deliverySource;

    internal KeyLibrary(SendBuffer buffer,DeliverySource deliverySource)
    {
        this.buffer= buffer;
        this.deliverySource = deliverySource;
    }

    /// <summary>
    /// Add后自动发送，无需再发送
    /// </summary>
    internal void OnSend(byte messageType, Delivery delivery, MessageWriter writer = null)
    {
        var d = deliverySource.DeliveryToId(delivery);
        DataTransportBase.Send(buffer, messageType, d, writer);
        if (delivery == Delivery.Unreliable)
        {
        }
        else if (delivery == Delivery.Strive)
        {
            StriveKeys.Add(d, StriveKey.Get(messageType, d, writer.Clone()));
        }
        else if(delivery== Delivery.Reliable)
        {
            ReliableKeys.Add(d, ReliableKey.Get(messageType, d, writer.Clone()));
        }
        else
        {
            OrderedKeys.Add(d, OrderwiseKey.Get(OrderedIndexSource++, messageType, d, writer.Clone()));
        }
    }
    private static readonly HashSet<ushort>t_ToRemove=new();
    internal void Update()
    {
        if (RespKeys.Count > 0)
        {
            foreach (var kvp in RespKeys) if (kvp.Value < Time.time) t_ToRemove.Add(kvp.Key);
            foreach (var i in t_ToRemove) RespKeys.Remove(i, out var k);
            t_ToRemove.Clear();
        }
        if (StriveKeys.Count > 0)
        {
            foreach (var kvp in StriveKeys)
            {
                if (Time.time > kvp.Value.NextSendTime)
                {
                    DataTransportBase.Send(buffer, kvp.Value.messageType, kvp.Value.delivery, kvp.Value.writer);
                    kvp.Value.NextSendTime = Time.time + EnsInstance.StriveKeySendInterval;
                    kvp.Value.SendCountLeft-=1;
                    if (kvp.Value.SendCountLeft == 0)
                    {
                        t_ToRemove.Add(kvp.Key);
                        kvp.Value.writer.Dispose();
                    }
                }
            }
            foreach (var i in t_ToRemove)
            {
                StriveKeys.Remove(i,out var k);
                StriveKey.Return(k);
            }
            t_ToRemove.Clear();
        }
        if (ReliableKeys.Count > 0)
        {
            foreach (var kvp in ReliableKeys)
            {
                var k = kvp.Value;
                if (k.State == KeyState.TobeConfirmed)
                {
                    if (Time.time > k.ConfirmedExistingSource)
                    {
                        k.State = KeyState.End;
                        Utils.Debug.LogError($"信息未得到确认：type={k.messageType},delivery={k.delivery}");
                    }

                    if (Time.time > k.ToConfirmIntervalLeft)
                    {
                        k.ToConfirmIntervalLeft = Time.time + EnsInstance.UnconfirmedKeySendInterval;

                        DataTransportBase.Send(buffer, k.messageType, k.delivery, k.writer);
                    }
                }
                else if (k.State == KeyState.ConfirmedExisting)
                {
                    if (Time.time > k.ConfirmedExistingSource) k.State = KeyState.End;
                }
                if (kvp.Value.State == KeyState.End)
                {
                    t_ToRemove.Add(kvp.Key);
                    kvp.Value.writer.Dispose();
                }
            }
            foreach (var i in t_ToRemove)
            {
                ReliableKeys.Remove(i,out var k);
                ReliableKey.Return(k);
            }
            t_ToRemove.Clear();
        }
        if (OrderedKeys.Count > 0)
        {
            ushort index = ushort.MaxValue;
            foreach (var kvp in OrderedKeys)
            {
                if (kvp.Value.State == KeyState.ConfirmedExisting)
                {
                    if (Time.time > kvp.Value.ConfirmedExistingSource) 
                        kvp.Value.State = KeyState.End;
                }
                else if (kvp.Value.State == KeyState.TobeConfirmed)
                {
                    if (index==ushort.MaxValue||kvp.Value.OrderwiseIndex < OrderedKeys[index].OrderwiseIndex) index = kvp.Key;
                }
            }
            if(index!=ushort.MaxValue)
            {
                var k = OrderedKeys[index];
                if (Time.time > k.ConfirmedExistingSource)
                {
                    k.State = KeyState.End;
                    Utils.Debug.LogError($"信息未得到确认：type={k.messageType},delivery={k.delivery}");
                }

                if (Time.time > k.ToConfirmIntervalLeft)
                {
                    k.ToConfirmIntervalLeft = Time.time + EnsInstance.UnconfirmedKeySendInterval;

                    DataTransportBase.Send(buffer, k.messageType, k.delivery, k.writer);
                }
            }

            foreach (var kvp in OrderedKeys)
                if (kvp.Value.State == KeyState.End)
                {
                    t_ToRemove.Add(kvp.Key);
                    kvp.Value.writer.Dispose();
                }
            foreach (var i in t_ToRemove)
            {
                OrderedKeys.Remove(i,out var k);
                OrderwiseKey.Return(k);
            }
            t_ToRemove.Clear();
            if (OrderedKeys.Count == 0) OrderedIndexSource = 0;
        }
    }
    internal void Clear()
    {
        foreach (var v in StriveKeys.Values) { v.writer.Dispose(); StriveKey.Return(v); }
        foreach (var v in ReliableKeys.Values) { v.writer.Dispose(); ReliableKey.Return(v); }
        foreach (var v in OrderedKeys.Values) { v.writer.Dispose(); OrderwiseKey.Return(v); }

        StriveKeys.Clear();
        ReliableKeys.Clear();
        OrderedKeys.Clear();
        RespKeys.Clear();
    }
    /// <summary>
    /// 处理所有关键消息
    /// </summary>
    internal void OnRecvData(byte[] src,Segment segment, out bool skip)
    {
        int startIndex = segment.StartIndex+1;
        ushort deliveryKey = UshortSerializer.Deserialize(src, ref startIndex, startIndex + 2);
        Delivery deliveryType = DeliverySource.IdToDelivery(deliveryKey);
        if (deliveryType==Delivery.Unreliable)
        {
            skip = false;
        }
        ///////////////////////////////////////////////////////////////A收到B对A的回应
        else if (DeliverySource.IsResponse(deliveryKey))
        {
            skip= true;
            deliveryKey=DeliverySource.ResponseToMessage(deliveryKey);
            if (ReliableKeys.ContainsKey(deliveryKey))
            {
                if (ReliableKeys[deliveryKey].State == KeyState.TobeConfirmed)
                {
                    ReliableKeys[deliveryKey].State = KeyState.ConfirmedExisting;
                }
            }
            else if (OrderedKeys.ContainsKey(deliveryKey))
            {
                if (OrderedKeys[deliveryKey].State == KeyState.TobeConfirmed)
                {
                    OrderedKeys[deliveryKey].State = KeyState.ConfirmedExisting;
                }
            }
        }
        //非本地//////////////////////////////////////////////////////////////////B收到A的关键消息
        else
        {
            if (!RespKeys.ContainsKey(deliveryKey))
            {
                skip = false;
                RespKeys.Add(deliveryKey, EnsInstance.ReceiverKeyExistTime + Time.time);
            }
            else
            {
                skip = true;
            }
            if(deliveryType != Delivery.Strive)
                DataTransportBase.Send(buffer, src[segment.StartIndex], DeliverySource.MessageToResponse(deliveryKey));
        }
    }
}