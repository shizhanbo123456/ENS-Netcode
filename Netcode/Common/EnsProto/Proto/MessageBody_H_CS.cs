using System;

namespace Ens.ProtoFormat
{
    //H 服务器接收 心跳检测；body 为客户端时间戳(ms)
    public struct MessageBody_H_CS
    {
        public int clientMillisecond;
    }
    public struct MessageBody_H_CSSerializer
    {
        public static bool Serialize(MessageBody_H_CS value, byte[] result, ref int indexStart)
        {
            if (result.Length - indexStart < 4) return false;
            IntSerializer.Serialize(value.clientMillisecond, result, ref indexStart);
            return true;
        }

        public static MessageBody_H_CS Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (invalidIndex - indexStart < 4) throw new Exception("反序列化失败：剩余数据字节数不足");
            int clientMillisecond = IntSerializer.Deserialize(data, ref indexStart, invalidIndex);
            if (indexStart > invalidIndex) throw new Exception("下标越界");
            return new MessageBody_H_CS { clientMillisecond = clientMillisecond };
        }
    }
}
