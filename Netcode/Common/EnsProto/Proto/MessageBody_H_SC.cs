using System;

namespace Ens.ProtoFormat
{
    //H 客户端接收 心跳检测
    public struct MessageBody_H_SC
    {
        public int serverMillisecond;
        public int delay;
    }
    public struct MessageBody_H_SCSerializer
    {
        public static bool Serialize(MessageBody_H_SC value, byte[] result, ref int indexStart)
        {
            if (result.Length - indexStart < 4 + 4) return false;
            IntSerializer.Serialize(value.serverMillisecond, result, ref indexStart);
            IntSerializer.Serialize(value.delay, result, ref indexStart);
            return true;
        }

        public static MessageBody_H_SC Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (data.Length - indexStart < 8) throw new Exception("反序列化失败：剩余数据字节数不足");
            int serverMillisecond = IntSerializer.Deserialize(data, ref indexStart, invalidIndex);
            int delay = IntSerializer.Deserialize(data, ref indexStart, invalidIndex);
            if (indexStart > invalidIndex) throw new Exception("下标越界");
            return new MessageBody_H_SC { serverMillisecond = serverMillisecond, delay = delay };
        }
    }
}
