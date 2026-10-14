using System;

namespace Ens.ProtoFormat
{
    //F 客户端接收 函数调用
    public struct MessageBody_F_SC
    {
        public short NOMObjectId;
        public byte func;
        public string param;
    }
    public struct MessageBody_F_SCSerializer
    {
        public static bool Serialize(MessageBody_F_SC value, byte[] result, ref int indexStart)
        {
            if (result.Length - indexStart < 2 + 1 + StringSerializer.GetLength(value.param ?? string.Empty)) return false;
            ShortSerializer.Serialize(value.NOMObjectId, result, ref indexStart);
            ByteSerializer.Serialize(value.func, result, ref indexStart);
            StringSerializer.Serialize(value.param, result, ref indexStart);
            return true;
        }

        public static MessageBody_F_SC Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (data.Length - indexStart < 4) throw new Exception("反序列化失败：剩余数据字节数不足");
            short NOMObjectId = ShortSerializer.Deserialize(data, ref indexStart, invalidIndex);
            byte func = ByteSerializer.Deserialize(data, ref indexStart, invalidIndex);
            string param = StringSerializer.Deserialize(data, ref indexStart, invalidIndex);
            if (indexStart > invalidIndex) throw new Exception("下标越界");
            return new MessageBody_F_SC { NOMObjectId = NOMObjectId, func = func, param = param };
        }
    }
}
