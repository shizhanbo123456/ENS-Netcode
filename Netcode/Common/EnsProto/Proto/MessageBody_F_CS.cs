using System;

namespace Ens.ProtoFormat
{
    //F 服务器接收 函数调用
    public struct MessageBody_F_CS
    {
        public short NOMObjectId;
        public byte func;
        public (byte[] data, Segment segment) param;//原始参数字节视图，格式由 func 决定，不做拷贝
        public short target;
    }
    public struct MessageBody_F_CSSerializer
    {
        public static bool Serialize(MessageBody_F_CS value, byte[] result, ref int indexStart)
        {
            int paramLength = value.param.segment.Length;
            if (result.Length - indexStart < 2 + 1 + paramLength + 2) return false;
            ShortSerializer.Serialize(value.NOMObjectId, result, ref indexStart);
            ByteSerializer.Serialize(value.func, result, ref indexStart);
            if (paramLength > 0) Buffer.BlockCopy(value.param.data, value.param.segment.StartIndex, result, indexStart, paramLength);
            indexStart += paramLength;
            ShortSerializer.Serialize(value.target, result, ref indexStart);
            return true;
        }

        public static MessageBody_F_CS Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (data.Length - indexStart < 5) throw new Exception("反序列化失败：剩余数据字节数不足");
            short NOMObjectId = ShortSerializer.Deserialize(data, ref indexStart, invalidIndex);
            byte func = ByteSerializer.Deserialize(data, ref indexStart, invalidIndex);
            int paramLength = invalidIndex - indexStart - 2;
            if (paramLength < 0) throw new Exception("反序列化失败：剩余数据字节数不足");
            var param = (data, segment: new Segment(indexStart, paramLength));
            indexStart += paramLength;
            short target = ShortSerializer.Deserialize(data, ref indexStart, invalidIndex);
            if (indexStart > invalidIndex) throw new Exception("下标越界");
            return new MessageBody_F_CS { NOMObjectId = NOMObjectId, func = func, param = param, target = target };
        }
    }
}
