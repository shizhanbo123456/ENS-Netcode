using System;
using System.Text;

public struct StringSerializer
{
    // 格式：1 字节 UTF-8 长度 + 内容（内容最长 255 字节）
    // 空间不足返回 false（不写、不推进 indexStart）；字符串超长抛 ArgumentException
    public static bool Serialize(string value, byte[] result, ref int indexStart)
    {
        if (result == null || indexStart < 0 || indexStart > result.Length) return false;

        int utf8ByteCount = string.IsNullOrEmpty(value) ? 0 : Encoding.UTF8.GetByteCount(value);
        if (utf8ByteCount > byte.MaxValue)
            throw new ArgumentException($"字符串过长：UTF-8 需 {utf8ByteCount} 字节，上限 255");
        if (result.Length - indexStart < utf8ByteCount + 1) return false;

        ByteSerializer.Serialize((byte)utf8ByteCount, result, ref indexStart);
        if (utf8ByteCount > 0)
        {
            Encoding.UTF8.GetBytes(value, 0, value.Length, result, indexStart);
            indexStart += utf8ByteCount;
        }
        return true;
    }

    // 与 Serialize 配对；读不到长度头或内容不足时抛异常
    public static string Deserialize(byte[] data, ref int indexStart, int invalidIndex)
    {
        if (data == null || indexStart < 0 || data.Length - indexStart < 1)
            throw new ArgumentException("反序列化 string 失败：剩余数据不足，读不到长度头");

        int len = ByteSerializer.Deserialize(data, ref indexStart, invalidIndex);
        if (data.Length - indexStart < len || indexStart + len > invalidIndex)
            throw new ArgumentException($"反序列化 string 失败：长度标识({len} 字节)超出剩余数据");

        if (len == 0) return string.Empty;
        string result = Encoding.UTF8.GetString(data, indexStart, len);
        indexStart += len;
        return result;
    }

    // 序列化后占用的字节数 = 1 字节长度头 + UTF-8 内容
    public static int GetLength(string msg) => Encoding.UTF8.GetByteCount(msg) + 1;
}