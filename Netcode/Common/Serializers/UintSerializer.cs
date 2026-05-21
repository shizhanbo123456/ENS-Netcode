using System;

public struct UintSerializer
{
    public static bool Serialize(uint value, byte[] result, ref int indexStart)
    {
        if (result.Length - indexStart < 4) return false;
        result[indexStart] = (byte)(value >> 24);
        result[indexStart + 1] = (byte)(value >> 16);
        result[indexStart + 2] = (byte)(value >> 8);
        result[indexStart + 3] = (byte)value;
        indexStart += 4;
        return true;
    }

    public static uint Deserialize(byte[] data, ref int indexStart, int invalidIndex)
    {
        if (data.Length - indexStart < 4)
        {
            throw new Exception("反序列化失败：剩余数据字节数不足");
        }

        uint result = (uint)data[indexStart] << 24
                      | (uint)data[indexStart + 1] << 16
                      | (uint)data[indexStart + 2] << 8
                      | data[indexStart + 3];
        indexStart += 4;
        if (indexStart > invalidIndex)
        {
            throw new Exception("下标越界");
        }
        return result;
    }
}