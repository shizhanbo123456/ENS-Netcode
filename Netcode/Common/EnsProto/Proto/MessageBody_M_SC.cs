using System;

namespace Ens.ProtoFormat
{
    //M 客户端接收
    public struct MessageBody_M_SC
    {
        public int type;
        public string content;
    }
    public struct MessageBody_M_SCSerializer
    {
        public static bool Serialize(MessageBody_M_SC value, byte[] result, ref int indexStart)
        {
            if (result.Length - indexStart < 4 + StringSerializer.GetLength(value.content ?? string.Empty)) return false;
            IntSerializer.Serialize(value.type, result, ref indexStart);
            StringSerializer.Serialize(value.content, result, ref indexStart);
            return true;
        }

        public static MessageBody_M_SC Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (data.Length - indexStart < 5) throw new Exception("反序列化失败：剩余数据字节数不足");
            int type = IntSerializer.Deserialize(data, ref indexStart, invalidIndex);
            string content = StringSerializer.Deserialize(data, ref indexStart, invalidIndex);
            if (indexStart > invalidIndex) throw new Exception("下标越界");
            return new MessageBody_M_SC { type = type, content = content };
        }
    }
}
