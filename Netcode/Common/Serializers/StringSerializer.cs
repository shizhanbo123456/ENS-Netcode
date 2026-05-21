using System;
using System.Text;

public struct StringSerializer
{
    /// <summary>
    /// 【性能优化版】序列化字符串（UTF-8编码）
    /// 核心：直接写入预分配数组+编码前预计算长度+零新数组创建
    /// </summary>
    /// <param name="value">待序列化的字符串（支持null/空字符串）</param>
    /// <param name="result">预分配的输出字节缓冲区</param>
    /// <param name="indexStart">起始索引（ref自动偏移）</param>
    /// <returns>缓冲区充足返回true，不足返回false</returns>
    public static bool Serialize(string value, byte[] result, ref int indexStart)
    {
        // 1. 边界防护：缓冲区本身为null/起始索引非法，直接返回空间不足
        if (result == null || indexStart < 0 || indexStart > result.Length)
        {
            return false;
        }

        // 2. 处理null/空字符串，统一按0字节长度处理
        int utf8ByteCount = 0;
        if (!string.IsNullOrEmpty(value))
        {
            //核心优化1：编码前预计算UTF8字节长度，避免编码后再判断（性能关键）
            utf8ByteCount = Encoding.UTF8.GetByteCount(value);
        }

        // 3.核心优化2：编码前完成总空间校验，无需后续回滚/二次判断
        // 总所需字节 = int长度标识(4字节) + 字符串UTF8实际字节数
        const int IntLengthHeader = 4;
        int totalRequiredBytes = IntLengthHeader + utf8ByteCount;
        // 校验剩余缓冲区空间是否充足
        if (result.Length - indexStart < totalRequiredBytes)
        {
            return false;
        }

        // 4. 步骤1：序列化【UTF8字节长度】（大端序，复用原有IntSerializer保证协议统一）
        IntSerializer.Serialize(utf8ByteCount, result, ref indexStart);

        // 5. 步骤2：直接将字符串UTF8编码写入预分配数组（核心优化3：零新数组创建）
        if (utf8ByteCount > 0)
        {
            // 直接写入预分配的result数组，无中间数组、无拷贝，性能极致
            Encoding.UTF8.GetBytes(value, 0, value.Length, result, indexStart);
            indexStart += utf8ByteCount;
        }

        return true;
    }

    /// <summary>
    /// 反序列化字符串（UTF-8编码，兼容优化后的序列化逻辑）
    /// </summary>
    /// <param name="data">输入字节数据</param>
    /// <param name="indexStart">起始索引（ref自动偏移）</param>
    /// <param name="invalidIndex">最大合法索引（越界校验）</param>
    /// <returns>反序列化后的字符串</returns>
    /// <exception cref="ArgumentException">数据不足/格式错误时抛异常</exception>
    public static string Deserialize(byte[] data, ref int indexStart, int invalidIndex)
    {
        // 基础校验：缓冲区null/索引非法/无长度标识的4字节基础空间
        if (data == null || indexStart < 0 || data.Length - indexStart < 4)
        {
            throw new ArgumentException("反序列化string失败：剩余数据不足，无法读取长度标识");
        }

        // 步骤1：反序列化【字符串UTF8字节长度】（大端序）
        int strByteLength = IntSerializer.Deserialize(data, ref indexStart, invalidIndex);

        // 长度合法性校验：负数长度/剩余空间不足/越界最大索引
        if (strByteLength < 0
            || data.Length - indexStart < strByteLength
            || indexStart + strByteLength > invalidIndex)
        {
            string errorMsg = $"反序列化string失败：长度标识({strByteLength}字节)非法，超出剩余缓冲区大小";
            throw new ArgumentException(errorMsg);
        }

        // 步骤2：反序列化字符串内容（空长度直接返回空字符串）
        string result = string.Empty;
        if (strByteLength > 0)
        {
            result = Encoding.UTF8.GetString(data, indexStart, strByteLength);
            indexStart += strByteLength;
        }

        return result;
    }
    public static int GetLength(string msg)=>Encoding.UTF8.GetByteCount(msg)+4;
}