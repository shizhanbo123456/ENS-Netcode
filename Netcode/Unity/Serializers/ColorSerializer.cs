using System;
using UnityEngine;

public struct ColorSerializer
{
    public static bool Serialize(Color value, byte[] result, ref int indexStart)
    {
        // Color = r+g+b+a → 4*8=32字节 (0~1浮点值)
        if (result.Length - indexStart < 32) return false;

        FloatSerializer.Serialize(value.r, result, ref indexStart);
        FloatSerializer.Serialize(value.g, result, ref indexStart);
        FloatSerializer.Serialize(value.b, result, ref indexStart);
        FloatSerializer.Serialize(value.a, result, ref indexStart);
        return true;
    }

    public static Color Deserialize(byte[] data, ref int indexStart, int invalidIndex)
    {
        if (data.Length - indexStart < 16)
        {
            throw new Exception("反序列化失败：剩余数据字节数不足");
        }

        float r = FloatSerializer.Deserialize(data, ref indexStart, invalidIndex);
        float g = FloatSerializer.Deserialize(data, ref indexStart, invalidIndex);
        float b = FloatSerializer.Deserialize(data, ref indexStart, invalidIndex);
        float a = FloatSerializer.Deserialize(data, ref indexStart, invalidIndex);
        if (indexStart > invalidIndex)
        {
            throw new Exception("下标越界");
        }
        return new Color(r, g, b, a);
    }
}