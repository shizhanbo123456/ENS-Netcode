using System;

namespace Ens.ProtoFormat
{
    //D 服务器接收 断开连接(无消息体)
    public struct MessageBody_D_CS
    {
        public byte messageHeader => Header.Dcs;
        public ushort delivery;
    }
    public struct MessageBody_D_CSSerializer
    {
        public static bool Serialize(MessageBody_D_CS value, byte[] result, ref int indexStart)
        {
            if (result.Length - indexStart < 3) return false;
            ByteSerializer.Serialize(value.messageHeader, result, ref indexStart);
            UshortSerializer.Serialize(value.delivery, result, ref indexStart);
            return true;
        }

        public static MessageBody_D_CS Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (data.Length - indexStart < 3) throw new Exception("反序列化失败：剩余数据字节数不足");
            byte msgHeader = ByteSerializer.Deserialize(data, ref indexStart, invalidIndex);
            if (msgHeader != Header.Dcs) throw new Exception($"消息类型不匹配：期望{Header.Dcs}，实际{msgHeader}");
            ushort delivery = UshortSerializer.Deserialize(data, ref indexStart, invalidIndex);
            if (indexStart > invalidIndex) throw new Exception("下标越界");
            return new MessageBody_D_CS { delivery = delivery };
        }

        public static int GetLength(MessageBody_D_CS value)
        {
            return 3;
        }
    }
}
