using System;

namespace Ens.ProtoFormat
{
    //H 服务器接收 心跳检测
    public struct MessageBody_H_CS
    {
        public int millisecondInServerMsg;
    }
    public struct MessageBody_H_CSSerializer
    {
        public static bool Serialize(MessageBody_H_CS value, byte[] result, ref int indexStart)
        {
            if (result.Length - indexStart < 4) return false;
            IntSerializer.Serialize(value.millisecondInServerMsg, result, ref indexStart);
            return true;
        }

        public static MessageBody_H_CS Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (data.Length - indexStart < 4) throw new Exception("反序列化失败：剩余数据字节数不足");
            int millisecondInServerMsg = IntSerializer.Deserialize(data, ref indexStart, invalidIndex);
            if (indexStart > invalidIndex) throw new Exception("下标越界");
            return new MessageBody_H_CS { millisecondInServerMsg = millisecondInServerMsg };
        }
    }
}
