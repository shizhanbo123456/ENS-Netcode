using System;

namespace Ens.ProtoFormat
{
    //f 服务器接收 创建物体
    public struct MessageBody_f_CS
    {
        public short sendto;
        public bool spawnMode;//false:用behaviourCount; true:用idStart
        public short collectionId;
        public string param;
        public short? behaviourCount;
        public short? idStart;
    }
    public struct MessageBody_f_CSSerializer
    {
        public static bool Serialize(MessageBody_f_CS value, byte[] result, ref int indexStart)
        {
            if (result.Length - indexStart < 2 + 1 + 2 + StringSerializer.GetLength(value.param ?? string.Empty) + 2) return false;
            ShortSerializer.Serialize(value.sendto, result, ref indexStart);
            BoolSerializer.Serialize(value.spawnMode, result, ref indexStart);
            ShortSerializer.Serialize(value.collectionId, result, ref indexStart);
            StringSerializer.Serialize(value.param, result, ref indexStart);
            ShortSerializer.Serialize(value.spawnMode ? (value.idStart ?? 0) : (value.behaviourCount ?? 0), result, ref indexStart);
            return true;
        }

        public static MessageBody_f_CS Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (data.Length - indexStart < 8) throw new Exception("反序列化失败：剩余数据字节数不足");
            short sendto = ShortSerializer.Deserialize(data, ref indexStart, invalidIndex);
            bool spawnMode = BoolSerializer.Deserialize(data, ref indexStart, invalidIndex);
            short collectionId = ShortSerializer.Deserialize(data, ref indexStart, invalidIndex);
            string param = StringSerializer.Deserialize(data, ref indexStart, invalidIndex);
            short? behaviourCount = null;
            short? idStart = null;
            if (spawnMode) idStart = ShortSerializer.Deserialize(data, ref indexStart, invalidIndex);
            else behaviourCount = ShortSerializer.Deserialize(data, ref indexStart, invalidIndex);
            if (indexStart > invalidIndex) throw new Exception("下标越界");
            return new MessageBody_f_CS { sendto = sendto, spawnMode = spawnMode, collectionId = collectionId, param = param, behaviourCount = behaviourCount, idStart = idStart };
        }
    }
}
