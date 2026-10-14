using System;

namespace Ens.ProtoFormat
{
    //E 客户端接收 事件
    public struct MessageBody_E_SC
    {
        public byte messageHeader => Header.Esc;
        public ushort delivery;
        public byte eventId;//1 OnClientConnect 2 OnClientDisconnect
        public short clientId;
    }
    public struct MessageBody_E_SCSerializer
    {
        public static bool Serialize(MessageBody_E_SC value, byte[] result, ref int indexStart)
        {
            if (result.Length - indexStart < 3 + 1 + 2) return false;
            ByteSerializer.Serialize(value.messageHeader, result, ref indexStart);
            UshortSerializer.Serialize(value.delivery, result, ref indexStart);
            ByteSerializer.Serialize(value.eventId, result, ref indexStart);
            ShortSerializer.Serialize(value.clientId, result, ref indexStart);
            return true;
        }

        public static MessageBody_E_SC Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (data.Length - indexStart < 3 + 1 + 2) throw new Exception("反序列化失败：剩余数据字节数不足");
            byte msgHeader = ByteSerializer.Deserialize(data, ref indexStart, invalidIndex);
            if (msgHeader != Header.Esc) throw new Exception($"消息类型不匹配：期望{Header.Esc}，实际{msgHeader}");
            ushort delivery = UshortSerializer.Deserialize(data, ref indexStart, invalidIndex);
            byte eventId = ByteSerializer.Deserialize(data, ref indexStart, invalidIndex);
            short clientId = ShortSerializer.Deserialize(data, ref indexStart, invalidIndex);
            if (indexStart > invalidIndex) throw new Exception("下标越界");
            return new MessageBody_E_SC { delivery = delivery, eventId = eventId, clientId = clientId };
        }
    }
}
