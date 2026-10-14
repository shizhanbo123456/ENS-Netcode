using System;

namespace Ens.ProtoFormat
{
    //A 客户端接收
    public struct MessageBody_A_SC
    {
        public byte messageHeader => Header.Asc;
        public ushort delivery;
        public bool authorityActive;
    }
    public struct MessageBody_A_SCSerializer
    {
        public static bool Serialize(MessageBody_A_SC value, byte[] result, ref int indexStart)
        {
            if (result.Length - indexStart < 3 + 1) return false;
            ByteSerializer.Serialize(value.messageHeader, result, ref indexStart);
            UshortSerializer.Serialize(value.delivery, result, ref indexStart);
            BoolSerializer.Serialize(value.authorityActive, result, ref indexStart);
            return true;
        }

        public static MessageBody_A_SC Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (data.Length - indexStart < 3 + 1) throw new Exception("反序列化失败：剩余数据字节数不足");
            byte msgHeader = ByteSerializer.Deserialize(data, ref indexStart, invalidIndex);
            if (msgHeader != Header.Asc) throw new Exception($"消息类型不匹配：期望{Header.Asc}，实际{msgHeader}");
            ushort delivery = UshortSerializer.Deserialize(data, ref indexStart, invalidIndex);
            bool authorityActive = BoolSerializer.Deserialize(data, ref indexStart, invalidIndex);
            if (indexStart > invalidIndex) throw new Exception("下标越界");
            return new MessageBody_A_SC { delivery = delivery, authorityActive = authorityActive };
        }

        public static int GetLength(MessageBody_A_SC value)
        {
            return 3 + 1;
        }
    }
}
