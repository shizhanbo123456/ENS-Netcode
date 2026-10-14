using System;

namespace Ens.ProtoFormat
{
    //f 客户端接收 创建物体
    public struct MessageBody_f_SC
    {
        public bool spawnMode;//false:创建; true:检测重建
        public short collectionId;
        public string param;
        public short idStart;
    }
    public struct MessageBody_f_SCSerializer
    {
        public static bool Serialize(MessageBody_f_SC value, byte[] result, ref int indexStart)
        {
            if (result.Length - indexStart < 1 + 2 + StringSerializer.GetLength(value.param ?? string.Empty) + 2) return false;
            BoolSerializer.Serialize(value.spawnMode, result, ref indexStart);
            ShortSerializer.Serialize(value.collectionId, result, ref indexStart);
            StringSerializer.Serialize(value.param, result, ref indexStart);
            ShortSerializer.Serialize(value.idStart, result, ref indexStart);
            return true;
        }

        public static MessageBody_f_SC Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (data.Length - indexStart < 6) throw new Exception("反序列化失败：剩余数据字节数不足");
            bool spawnMode = BoolSerializer.Deserialize(data, ref indexStart, invalidIndex);
            short collectionId = ShortSerializer.Deserialize(data, ref indexStart, invalidIndex);
            string param = StringSerializer.Deserialize(data, ref indexStart, invalidIndex);
            short idStart = ShortSerializer.Deserialize(data, ref indexStart, invalidIndex);
            if (indexStart > invalidIndex) throw new Exception("下标越界");
            return new MessageBody_f_SC { spawnMode = spawnMode, collectionId = collectionId, param = param, idStart = idStart };
        }
    }
}
