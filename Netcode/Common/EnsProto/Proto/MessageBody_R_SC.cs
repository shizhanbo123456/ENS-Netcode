using System;

namespace Ens.ProtoFormat
{
    //R 客户端接收 离开房间重置(无消息体)
    public struct MessageBody_R_SC
    {
        public byte messageHeader => Header.Rsc;
        public ushort delivery;
    }
    public struct MessageBody_R_SCSerializer
    {
        public static bool Serialize(MessageBody_R_SC value, byte[] result, ref int indexStart)
        {
            if (result.Length - indexStart < 3) return false;
            ByteSerializer.Serialize(value.messageHeader, result, ref indexStart);
            UshortSerializer.Serialize(value.delivery, result, ref indexStart);
            return true;
        }

        public static MessageBody_R_SC Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (data.Length - indexStart < 3) throw new Exception("反序列化失败：剩余数据字节数不足");
            byte msgHeader = ByteSerializer.Deserialize(data, ref indexStart, invalidIndex);
            if (msgHeader != Header.Rsc) throw new Exception($"消息类型不匹配：期望{Header.Rsc}，实际{msgHeader}");
            ushort delivery = UshortSerializer.Deserialize(data, ref indexStart, invalidIndex);
            if (indexStart > invalidIndex) throw new Exception("下标越界");
            return new MessageBody_R_SC { delivery = delivery };
        }
    }
}
