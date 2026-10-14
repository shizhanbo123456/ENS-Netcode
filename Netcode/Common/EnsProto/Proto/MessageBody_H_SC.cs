using System;

namespace Ens.ProtoFormat
{
    //H 客户端接收 心跳回应；回显客户端时间戳(ms)，客户端用(本地时间-该值)/2 算出延迟
    public struct MessageBody_H_SC
    {
        public byte messageHeader => Header.Hsc;
        public ushort delivery;
        public int clientMillisecond;
    }
    public struct MessageBody_H_SCSerializer
    {
        public static bool Serialize(MessageBody_H_SC value, byte[] result, ref int indexStart)
        {
            if (result.Length - indexStart < 3 + 4) return false;
            ByteSerializer.Serialize(value.messageHeader, result, ref indexStart);
            UshortSerializer.Serialize(value.delivery, result, ref indexStart);
            IntSerializer.Serialize(value.clientMillisecond, result, ref indexStart);
            return true;
        }

        public static MessageBody_H_SC Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (data.Length - indexStart < 3 + 4) throw new Exception("反序列化失败：剩余数据字节数不足");
            byte msgHeader = ByteSerializer.Deserialize(data, ref indexStart, invalidIndex);
            if (msgHeader != Header.Hsc) throw new Exception($"消息类型不匹配：期望{Header.Hsc}，实际{msgHeader}");
            ushort delivery = UshortSerializer.Deserialize(data, ref indexStart, invalidIndex);
            int clientMillisecond = IntSerializer.Deserialize(data, ref indexStart, invalidIndex);
            if (indexStart > invalidIndex) throw new Exception("下标越界");
            return new MessageBody_H_SC { delivery = delivery, clientMillisecond = clientMillisecond };
        }
    }
}
