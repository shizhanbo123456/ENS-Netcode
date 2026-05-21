using System;
using UnityEngine;

/// <summary>Unity Vector2Int 二维整型向量 专属序列化器</summary>
public struct Vector2IntSerializer
{
    public static bool Serialize(Vector2Int value, byte[] result, ref int indexStart)
    {
        // Vector2Int = x(int4) + y(int4) → 共8字节
        if (result.Length - indexStart < 8) return false;

        IntSerializer.Serialize(value.x, result, ref indexStart);
        IntSerializer.Serialize(value.y, result, ref indexStart);
        return true;
    }

    public static Vector2Int Deserialize(byte[] data, ref int indexStart, int invalidIndex)
    {
        if (data.Length - indexStart < 8)
        {
            throw new Exception("反序列化失败：剩余数据字节数不足");
        }

        int x = IntSerializer.Deserialize(data, ref indexStart, invalidIndex);
        int y = IntSerializer.Deserialize(data, ref indexStart, invalidIndex);
        if (indexStart > invalidIndex)
        {
            throw new Exception("下标越界");
        }
        return new Vector2Int(x, y);
    }
}