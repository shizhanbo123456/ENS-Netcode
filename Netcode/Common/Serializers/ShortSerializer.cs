using System;

public struct ShortSerializer
{
    public static bool Serialize(short value, byte[] result, ref int indexStart)
    {
        if (result.Length - indexStart < 2) return false;
        result[indexStart] = (byte)(value >> 8);
        result[indexStart + 1] = (byte)value;
        indexStart += 2;
        return true;
    }

    public static short Deserialize(byte[] data, ref int indexStart, int invalidIndex)
    {
        if (data.Length - indexStart < 2)
        {
            throw new Exception("反序列化失败：剩余数据字节数不足");
        }

        short result = (short)((data[indexStart] << 8) | data[indexStart + 1]);
        indexStart += 2;
        if (indexStart > invalidIndex)
        {
            throw new Exception("下标越界");
        }
        return result;
    }
}