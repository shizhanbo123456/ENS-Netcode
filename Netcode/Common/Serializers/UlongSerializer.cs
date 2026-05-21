using System;

public struct UlongSerializer
{
    public static bool Serialize(ulong value, byte[] result, ref int indexStart)
    {
        if (result.Length - indexStart < 8) return false;
        result[indexStart] = (byte)(value >> 56);
        result[indexStart + 1] = (byte)(value >> 48);
        result[indexStart + 2] = (byte)(value >> 40);
        result[indexStart + 3] = (byte)(value >> 32);
        result[indexStart + 4] = (byte)(value >> 24);
        result[indexStart + 5] = (byte)(value >> 16);
        result[indexStart + 6] = (byte)(value >> 8);
        result[indexStart + 7] = (byte)value;
        indexStart += 8;
        return true;
    }

    public static ulong Deserialize(byte[] data, ref int indexStart, int invalidIndex)
    {
        if (data.Length - indexStart < 8)
        {
            throw new Exception("反序列化失败：剩余数据字节数不足");
        }

        ulong result = (ulong)data[indexStart] << 56
                       | (ulong)data[indexStart + 1] << 48
                       | (ulong)data[indexStart + 2] << 40
                       | (ulong)data[indexStart + 3] << 32
                       | (ulong)data[indexStart + 4] << 24
                       | (ulong)data[indexStart + 5] << 16
                       | (ulong)data[indexStart + 6] << 8
                       | data[indexStart + 7];
        indexStart += 8;
        if (indexStart > invalidIndex)
        {
            throw new Exception("下标越界");
        }
        return result;
    }
}