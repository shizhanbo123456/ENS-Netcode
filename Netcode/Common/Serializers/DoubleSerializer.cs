using System;

public struct DoubleSerializer
{
    public static bool Serialize(double value, byte[] result, ref int indexStart)
    {
        // 校验剩余空间，严谨可靠
        if (result == null || indexStart < 0 || result.Length - indexStart < 8)
            return false;

        ulong num = (ulong)BitConverter.DoubleToInt64Bits(value);

        result[indexStart] = (byte)(num >> 56);
        result[indexStart + 1] = (byte)(num >> 48);
        result[indexStart + 2] = (byte)(num >> 40);
        result[indexStart + 3] = (byte)(num >> 32);
        result[indexStart + 4] = (byte)(num >> 24);
        result[indexStart + 5] = (byte)(num >> 16);
        result[indexStart + 6] = (byte)(num >> 8);
        result[indexStart + 7] = (byte)num;

        indexStart += 8;
        return true;
    }

    // 反序列化：零分配、零反转、零拷贝，纯位运算解析大端序
    public static double Deserialize(byte[] data, ref int indexStart, int invalidIndex)
    {
        if (data == null || indexStart < 0 || data.Length - indexStart < 8)
        {
            throw new Exception("反序列化失败：剩余数据字节数不足");
        }

        ulong num = (ulong)data[indexStart] << 56
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

        return BitConverter.Int64BitsToDouble((long)num);
    }
}