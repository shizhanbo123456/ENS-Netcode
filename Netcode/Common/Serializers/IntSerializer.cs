using System;

public struct IntSerializer
{
    public static bool Serialize(int value, byte[] result, ref int indexStart)
    {
        if (result.Length - indexStart < 4) return false;
        result[indexStart + 3] = (byte)(value & 0xFF);
        result[indexStart + 2] = (byte)((value >> 8) & 0xFF);
        result[indexStart + 1] = (byte)((value >> 16) & 0xFF);
        result[indexStart] = (byte)((value >> 24) & 0xFF);
        indexStart += 4;
        return true;
    }
    public static int Deserialize(byte[] data, ref int indexStart, int invalidIndex)
    {
        if (data.Length < 4)
        {
            throw new Exception("反序列化失败：剩余数据字节数不足");
        }
        int result = data[indexStart + 3] | (data[indexStart + 2] << 8) | (data[indexStart + 1] << 16) | (data[indexStart] << 24);
        indexStart += 4;
        if (indexStart > invalidIndex)
        {
            throw new Exception("下标越界");
        }
        return result;
    }
}