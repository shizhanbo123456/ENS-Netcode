using System;
using UnityEngine;

public struct Vector2Serializer
{
    public static bool Serialize(Vector2 value, byte[] result, ref int indexStart)
    {
        // Vector2 = x(double 8字节) + y(double 8字节) → 共16字节
        if (result.Length - indexStart < 16) return false;

        FloatSerializer.Serialize(value.x, result, ref indexStart);
        FloatSerializer.Serialize(value.y, result, ref indexStart);
        return true;
    }

    public static Vector2 Deserialize(byte[] data, ref int indexStart, int invalidIndex)
    {
        if (data.Length - indexStart < 8)
        {
            throw new Exception("反序列化失败：剩余数据字节数不足");
        }

        float x = FloatSerializer.Deserialize(data, ref indexStart, invalidIndex);
        float y = FloatSerializer.Deserialize(data, ref indexStart, invalidIndex);
        if (indexStart > invalidIndex)
        {
            throw new Exception("下标越界");
        }
        return new Vector2(x, y);
    }
}