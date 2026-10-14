using System;

namespace ProtoFormat
{
    //E 客户端接收 事件
    public struct MessageBody_E_SC
    {
        public byte eventId;//1 OnClientConnect 2 OnClientDisconnect 3 OnServerConnect 4 OnServerDisconnect；3在C事件触发，4在ShutDown触发(超时/D事件/主动调用)
        public short? clientId;
    }
    public struct MessageBody_E_SCSerializer
    {
        public static bool Serialize(MessageBody_E_SC value, byte[] result, ref int indexStart)
        {
            if (result.Length - indexStart < 1 + (value.clientId.HasValue ? 2 : 0)) return false;
            ByteSerializer.Serialize(value.eventId, result, ref indexStart);
            if (value.clientId.HasValue) ShortSerializer.Serialize(value.clientId.Value, result, ref indexStart);
            return true;
        }

        public static MessageBody_E_SC Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (data.Length - indexStart < 1) throw new Exception("反序列化失败：剩余数据字节数不足");
            byte eventId = ByteSerializer.Deserialize(data, ref indexStart, invalidIndex);
            short? clientId = null;
            if (indexStart < invalidIndex) clientId = ShortSerializer.Deserialize(data, ref indexStart, invalidIndex);
            if (indexStart > invalidIndex) throw new Exception("下标越界");
            return new MessageBody_E_SC { eventId = eventId, clientId = clientId };
        }
    }
}
