using System;

namespace Ens.ProtoFormat
{
    //F 客户端接收 函数调用
    public struct MessageBody_F_SC
    {
        public short NOMObjectId;
        public byte func;
        public (byte[] data, Segment segment) param;//原始参数字节视图，格式由 func 决定，不做拷贝
    }
    public struct MessageBody_F_SCSerializer
    {
        public static bool Serialize(MessageBody_F_SC value, byte[] result, ref int indexStart)
        {
            int paramLength = value.param.segment.Length;
            if (result.Length - indexStart < 2 + 1 + paramLength) return false;
            ShortSerializer.Serialize(value.NOMObjectId, result, ref indexStart);
            ByteSerializer.Serialize(value.func, result, ref indexStart);
            if (paramLength > 0) Buffer.BlockCopy(value.param.data, value.param.segment.StartIndex, result, indexStart, paramLength);
            indexStart += paramLength;
            return true;
        }

        public static MessageBody_F_SC Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (data.Length - indexStart < 3) throw new Exception("反序列化失败：剩余数据字节数不足");
            short NOMObjectId = ShortSerializer.Deserialize(data, ref indexStart, invalidIndex);
            byte func = ByteSerializer.Deserialize(data, ref indexStart, invalidIndex);
            int paramLength = invalidIndex - indexStart;
            if (paramLength < 0) throw new Exception("反序列化失败：剩余数据字节数不足");
            var param = (data, segment: new Segment(indexStart, paramLength));
            indexStart += paramLength;
            if (indexStart > invalidIndex) throw new Exception("下标越界");
            return new MessageBody_F_SC { NOMObjectId = NOMObjectId, func = func, param = param };
        }
    }
}
