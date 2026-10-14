using System;

namespace ProtoFormat
{
    //F 服务器接收 函数调用
    public struct MessageBody_F_CS
    {
        public short NOMObjectId;
        public byte func;
        public string param;
        public short target;
    }
    public struct MessageBody_F_CSSerializer
    {
        public static bool Serialize(MessageBody_F_CS value, byte[] result, ref int indexStart)
        {
            if (result.Length - indexStart < 2 + 1 + StringSerializer.GetLength(value.param ?? string.Empty) + 2) return false;
            ShortSerializer.Serialize(value.NOMObjectId, result, ref indexStart);
            ByteSerializer.Serialize(value.func, result, ref indexStart);
            StringSerializer.Serialize(value.param, result, ref indexStart);
            ShortSerializer.Serialize(value.target, result, ref indexStart);
            return true;
        }

        public static MessageBody_F_CS Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (data.Length - indexStart < 6) throw new Exception("反序列化失败：剩余数据字节数不足");
            short NOMObjectId = ShortSerializer.Deserialize(data, ref indexStart, invalidIndex);
            byte func = ByteSerializer.Deserialize(data, ref indexStart, invalidIndex);
            string param = StringSerializer.Deserialize(data, ref indexStart, invalidIndex);
            short target = ShortSerializer.Deserialize(data, ref indexStart, invalidIndex);
            if (indexStart > invalidIndex) throw new Exception("下标越界");
            return new MessageBody_F_CS { NOMObjectId = NOMObjectId, func = func, param = param, target = target };
        }
    }
}
