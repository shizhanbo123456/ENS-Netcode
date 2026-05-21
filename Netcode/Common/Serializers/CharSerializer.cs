using System;

public struct CharSerializer
{
    public static bool Serialize(char value, byte[] result, ref int indexStart)
    {
        if (result.Length - indexStart < 2) return false;
        result[indexStart] = (byte)(value >> 8);
        result[indexStart + 1] = (byte)value;
        indexStart += 2;
        return true;
    }

    public static char Deserialize(byte[] data, ref int indexStart, int invalidIndex)
    {
        if (data.Length - indexStart < 2)
        {
            throw new Exception("反序列化失败：剩余数据字节数不足");
        }

        char result = (char)((data[indexStart] << 8) | data[indexStart + 1]);
        indexStart += 2;
        if (indexStart > invalidIndex)
        {
            throw new Exception("下标越界");
        }
        return result;
    }
}