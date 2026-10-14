using System;

namespace Ens.ProtoFormat
{
    //A 客户端接收
    public struct MessageBody_A_SC
    {
        public bool authorityActive;
    }
    public struct MessageBody_A_SCSerializer
    {
        public static bool Serialize(MessageBody_A_SC value, byte[] result, ref int indexStart)
        {
            if (result.Length - indexStart < 1) return false;
            BoolSerializer.Serialize(value.authorityActive, result, ref indexStart);
            return true;
        }

        public static MessageBody_A_SC Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (data.Length - indexStart < 1) throw new Exception("反序列化失败：剩余数据字节数不足");
            bool authorityActive = BoolSerializer.Deserialize(data, ref indexStart, invalidIndex);
            if (indexStart > invalidIndex) throw new Exception("下标越界");
            return new MessageBody_A_SC { authorityActive = authorityActive };
        }
    }
}
