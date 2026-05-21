using System;

public struct BoolSerializer
{
    public static bool Serialize(bool value, byte[] result, ref int indexStart)
    {
        if (result.Length - indexStart < 1) return false;
        result[indexStart] = value ? (byte)1 : (byte)0;
        indexStart += 1;
        return true;
    }

    public static bool Deserialize(byte[] data, ref int indexStart,int invalidIndex)
    {
        if (data.Length - indexStart < 1)
        {
            throw new Exception("反序列化失败：剩余数据字节数不足");
        }
        bool result = data[indexStart] != 0;
        indexStart += 1;
        if(indexStart>invalidIndex)
        {
            throw new Exception("下标越界");
        }
        return result;
    }
}