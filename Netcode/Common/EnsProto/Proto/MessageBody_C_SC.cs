using System;

namespace Ens.ProtoFormat
{
    //C 客户端接收 分配Id
    public struct MessageBody_C_SC
    {
        public byte messageHeader => Header.Csc;
        public ushort delivery;
        public short clientId;
    }
    public struct MessageBody_C_SCSerializer
    {
        public static bool Serialize(MessageBody_C_SC value, byte[] result, ref int indexStart)
        {
            if (result.Length - indexStart < 3 + 2) return false;
            ByteSerializer.Serialize(value.messageHeader, result, ref indexStart);
            UshortSerializer.Serialize(value.delivery, result, ref indexStart);
            ShortSerializer.Serialize(value.clientId, result, ref indexStart);
            return true;
        }

        public static MessageBody_C_SC Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (data.Length - indexStart < 3 + 2) throw new Exception("反序列化失败：剩余数据字节数不足");
            byte msgHeader = ByteSerializer.Deserialize(data, ref indexStart, invalidIndex);
            if (msgHeader != Header.Csc) throw new Exception($"消息类型不匹配：期望{Header.Csc}，实际{msgHeader}");
            ushort delivery = UshortSerializer.Deserialize(data, ref indexStart, invalidIndex);
            short clientId = ShortSerializer.Deserialize(data, ref indexStart, invalidIndex);
            if (indexStart > invalidIndex) throw new Exception("下标越界");
            return new MessageBody_C_SC { delivery = delivery, clientId = clientId };
        }

        public static int GetLength(MessageBody_C_SC value)
        {
            return 3 + 2;
        }
    }
}
