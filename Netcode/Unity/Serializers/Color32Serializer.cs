using System;
using UnityEngine;

public struct Color32Serializer
{
    public static bool Serialize(Color32 value, byte[] result, ref int indexStart)
    {
        // Color32 = r+g+b+a → 4*1=4字节 (0~255字节值，性能最优)
        if (result.Length - indexStart < 4) return false;

        ByteSerializer.Serialize(value.r, result, ref indexStart);
        ByteSerializer.Serialize(value.g, result, ref indexStart);
        ByteSerializer.Serialize(value.b, result, ref indexStart);
        ByteSerializer.Serialize(value.a, result, ref indexStart);
        return true;
    }

    public static Color32 Deserialize(byte[] data, ref int indexStart, int invalidIndex)
    {
        if (data.Length - indexStart < 4)
        {
            throw new Exception("反序列化失败：剩余数据字节数不足");
        }

        byte r = ByteSerializer.Deserialize(data, ref indexStart, invalidIndex);
        byte g = ByteSerializer.Deserialize(data, ref indexStart, invalidIndex);
        byte b = ByteSerializer.Deserialize(data, ref indexStart, invalidIndex);
        byte a = ByteSerializer.Deserialize(data, ref indexStart, invalidIndex);
        if (indexStart > invalidIndex)
        {
            throw new Exception("下标越界");
        }
        return new Color32(r, g, b, a);
    }
}