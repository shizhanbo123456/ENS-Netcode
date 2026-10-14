using System;

namespace Ens.ProtoFormat
{
    //A 服务器接收 设置房主权限
    public struct MessageBody_A_CS
    {
        public byte messageHeader => Header.A;
        public ushort delivery;
        public short clientId;
    }
    public struct MessageBody_A_CSSerializer
    {
        public static bool Serialize(MessageBody_A_CS value, byte[] result, ref int indexStart)
        {
            if (result.Length - indexStart < 3 + 2) return false;
            ByteSerializer.Serialize(value.messageHeader, result, ref indexStart);
            UshortSerializer.Serialize(value.delivery, result, ref indexStart);
            ShortSerializer.Serialize(value.clientId, result, ref indexStart);
            return true;
        }

        public static MessageBody_A_CS Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (data.Length - indexStart < 3 + 2) throw new Exception("反序列化失败：剩余数据字节数不足");
            byte msgHeader = ByteSerializer.Deserialize(data, ref indexStart, invalidIndex);
            if (msgHeader != Header.A) throw new Exception($"消息类型不匹配：期望{Header.A}，实际{msgHeader}");
            ushort delivery = UshortSerializer.Deserialize(data, ref indexStart, invalidIndex);
            short clientId = ShortSerializer.Deserialize(data, ref indexStart, invalidIndex);
            if (indexStart > invalidIndex) throw new Exception("下标越界");
            return new MessageBody_A_CS { delivery = delivery, clientId = clientId };
        }
    }
}
