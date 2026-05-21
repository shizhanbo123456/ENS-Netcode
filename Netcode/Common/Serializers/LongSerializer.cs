using System;

public struct LongSerializer
{
    public static bool Serialize(long value, byte[] result, ref int indexStart)
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

    public static long Deserialize(byte[] data, ref int indexStart, int invalidIndex)
    {
        if (data.Length - indexStart < 8)
        {
            throw new Exception("反序列化失败：剩余数据字节数不足");
        }

        long result = (long)data[indexStart] << 56
                      | (long)data[indexStart + 1] << 48
                      | (long)data[indexStart + 2] << 40
                      | (long)data[indexStart + 3] << 32
                      | (long)data[indexStart + 4] << 24
                      | (long)data[indexStart + 5] << 16
                      | (long)data[indexStart + 6] << 8
                      | data[indexStart + 7];
        indexStart += 8;
        if (indexStart > invalidIndex)
        {
            throw new Exception("下标越界");
        }
        return result;
    }
}