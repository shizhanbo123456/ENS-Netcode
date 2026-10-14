using System;

namespace ProtoFormat
{
    //C 客户端接收 分配Id
    public struct MessageBody_C_SC
    {
        public int clientId;
    }
    public struct MessageBody_C_SCSerializer
    {
        public static bool Serialize(MessageBody_C_SC value, byte[] result, ref int indexStart)
        {
            if (result.Length - indexStart < 4) return false;
            IntSerializer.Serialize(value.clientId, result, ref indexStart);
            return true;
        }

        public static MessageBody_C_SC Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (data.Length - indexStart < 4) throw new Exception("反序列化失败：剩余数据字节数不足");
            int clientId = IntSerializer.Deserialize(data, ref indexStart, invalidIndex);
            if (indexStart > invalidIndex) throw new Exception("下标越界");
            return new MessageBody_C_SC { clientId = clientId };
        }
    }
}
