using System;

namespace Ens.ProtoFormat
{
    //A 服务器接收 设置房主权限
    public struct MessageBody_A_CS
    {
        public short clientId;
    }
    public struct MessageBody_A_CSSerializer
    {
        public static bool Serialize(MessageBody_A_CS value, byte[] result, ref int indexStart)
        {
            if (result.Length - indexStart < 2) return false;
            ShortSerializer.Serialize(value.clientId, result, ref indexStart);
            return true;
        }

        public static MessageBody_A_CS Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (data.Length - indexStart < 2) throw new Exception("反序列化失败：剩余数据字节数不足");
            short clientId = ShortSerializer.Deserialize(data, ref indexStart, invalidIndex);
            if (indexStart > invalidIndex) throw new Exception("下标越界");
            return new MessageBody_A_CS { clientId = clientId };
        }
    }
}
