using System;

namespace Ens.ProtoFormat
{
    //Q 客户端接收
    public struct MessageBody_Q_SC
    {
        public string header;
        public string content;
    }
    public struct MessageBody_Q_SCSerializer
    {
        public static bool Serialize(MessageBody_Q_SC value, byte[] result, ref int indexStart)
        {
            if (result.Length - indexStart < StringSerializer.GetLength(value.header ?? string.Empty) + StringSerializer.GetLength(value.content ?? string.Empty)) return false;
            StringSerializer.Serialize(value.header, result, ref indexStart);
            StringSerializer.Serialize(value.content, result, ref indexStart);
            return true;
        }

        public static MessageBody_Q_SC Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (data.Length - indexStart < 2) throw new Exception("反序列化失败：剩余数据字节数不足");
            string header = StringSerializer.Deserialize(data, ref indexStart, invalidIndex);
            string content = StringSerializer.Deserialize(data, ref indexStart, invalidIndex);
            if (indexStart > invalidIndex) throw new Exception("下标越界");
            return new MessageBody_Q_SC { header = header, content = content };
        }
    }
}
