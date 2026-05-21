using System;
using UnityEngine;

/// <summary>Unity Vector3Int 三维整型向量 专属序列化器</summary>
public struct Vector3IntSerializer
{
    public static bool Serialize(Vector3Int value, byte[] result, ref int indexStart)
    {
        // Vector3Int = x+y+z → 3*4=12字节
        if (result.Length - indexStart < 12) return false;

        IntSerializer.Serialize(value.x, result, ref indexStart);
        IntSerializer.Serialize(value.y, result, ref indexStart);
        IntSerializer.Serialize(value.z, result, ref indexStart);
        return true;
    }

    public static Vector3Int Deserialize(byte[] data, ref int indexStart, int invalidIndex)
    {
        if (data.Length - indexStart < 12)
        {
            throw new Exception("反序列化失败：剩余数据字节数不足");
        }

        int x = IntSerializer.Deserialize(data, ref indexStart, invalidIndex);
        int y = IntSerializer.Deserialize(data, ref indexStart, invalidIndex);
        int z = IntSerializer.Deserialize(data, ref indexStart, invalidIndex);
        if (indexStart > invalidIndex)
        {
            throw new Exception("下标越界");
        }
        return new Vector3Int(x, y, z);
    }
}