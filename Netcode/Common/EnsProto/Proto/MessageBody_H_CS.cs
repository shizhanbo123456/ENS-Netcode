using System;

namespace Ens.ProtoFormat
{
    //H 服务器接收 心跳检测；body 为客户端时间戳(ms)
    public struct MessageBody_H_CS
    {
        public byte messageHeader => Header.Hcs;
        public ushort delivery;
        public int clientMillisecond;
    }
    public struct MessageBody_H_CSSerializer
    {
        public static bool Serialize(MessageBody_H_CS value, byte[] result, ref int indexStart)
        {
            if (result.Length - indexStart < 3 + 4) return false;
            ByteSerializer.Serialize(value.messageHeader, result, ref indexStart);
            UshortSerializer.Serialize(value.delivery, result, ref indexStart);
            IntSerializer.Serialize(value.clientMillisecond, result, ref indexStart);
            return true;
        }

        public static MessageBody_H_CS Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (data.Length - indexStart < 3 + 4) throw new Exception("反序列化失败：剩余数据字节数不足");
            byte msgHeader = ByteSerializer.Deserialize(data, ref indexStart, invalidIndex);
            if (msgHeader != Header.Hcs) throw new Exception($"消息类型不匹配：期望{Header.Hcs}，实际{msgHeader}");
            ushort delivery = UshortSerializer.Deserialize(data, ref indexStart, invalidIndex);
            int clientMillisecond = IntSerializer.Deserialize(data, ref indexStart, invalidIndex);
            if (indexStart > invalidIndex) throw new Exception("下标越界");
            return new MessageBody_H_CS { delivery = delivery, clientMillisecond = clientMillisecond };
        }
    }
}
