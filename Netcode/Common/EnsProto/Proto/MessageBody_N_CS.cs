using System;

namespace Ens.ProtoFormat
{
    //N 服务器接收
    public struct MessageBody_N_CS
    {
        public byte messageHeader => Header.Ncs;
        public ushort delivery;
        public int type;
        public string content;
    }
    public struct MessageBody_N_CSSerializer
    {
        public static bool Serialize(MessageBody_N_CS value, byte[] result, ref int indexStart)
        {
            if (result.Length - indexStart < 3 + 4 + StringSerializer.GetLength(value.content ?? string.Empty)) return false;
            ByteSerializer.Serialize(value.messageHeader, result, ref indexStart);
            UshortSerializer.Serialize(value.delivery, result, ref indexStart);
            IntSerializer.Serialize(value.type, result, ref indexStart);
            StringSerializer.Serialize(value.content, result, ref indexStart);
            return true;
        }

        public static MessageBody_N_CS Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (data.Length - indexStart < 3 + 5) throw new Exception("反序列化失败：剩余数据字节数不足");
            byte msgHeader = ByteSerializer.Deserialize(data, ref indexStart, invalidIndex);
            if (msgHeader != Header.Ncs) throw new Exception($"消息类型不匹配：期望{Header.Ncs}，实际{msgHeader}");
            ushort delivery = UshortSerializer.Deserialize(data, ref indexStart, invalidIndex);
            int type = IntSerializer.Deserialize(data, ref indexStart, invalidIndex);
            string content = StringSerializer.Deserialize(data, ref indexStart, invalidIndex);
            if (indexStart > invalidIndex) throw new Exception("下标越界");
            return new MessageBody_N_CS { delivery = delivery, type = type, content = content };
        }
    }
}
