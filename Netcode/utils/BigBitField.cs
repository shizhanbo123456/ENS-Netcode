using System;

/// <summary>
/// 支持任意位数的位运算数据结构。
/// 底层使用 byte[] 存储（小端字节序：第 i 位位于 _bytes[i >> 3] 的第 (i &amp; 7) 位），
/// 未设置的位一律视为 0（无限高位零扩展）。
///
/// 仅提供：创建、销毁（Dispose）、符号位运算（&amp; | ^ ~ &lt;&lt; &gt;&gt;）、与 byte[] 互转。
/// </summary>
public sealed class BigBitField : IDisposable
{
    private byte[] _bytes;  // 小端字节序，_bytes[0] 是最低 8 位
    private int _bitLength; // 逻辑位长

    /// <summary>创建指定位长的全 0 位域。</summary>
    public BigBitField(int bitLength)
    {
        if (bitLength < 0) throw new ArgumentOutOfRangeException(nameof(bitLength));
        _bitLength = bitLength;
        _bytes = new byte[BytesForLength(bitLength)];
    }

    /// <summary>销毁：释放内部存储（托管数组，之后不可再使用本实例）。</summary>
    public void Dispose()
    {
        _bytes = Array.Empty<byte>();
        _bitLength = 0;
    }

    /// <summary>从字节数组构造（小端：bytes[0] 是最低 8 位，位长 = 字节数 × 8）。</summary>
    public static BigBitField FromBytes(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        if ((long)bytes.Length * 8 > int.MaxValue)
            throw new ArgumentException("bytes 过大，位长超出 int 范围");
        var result = new BigBitField(bytes.Length * 8);
        Array.Copy(bytes, result._bytes, bytes.Length);
        return result;
    }

    /// <summary>导出载荷字节数组副本（正好覆盖全部位，可直接写入流/Socket）。</summary>
    public byte[] ToBytes()
    {
        var bytes = new byte[_bytes.Length];
        Array.Copy(_bytes, bytes, _bytes.Length);
        return bytes;
    }

    // ---------- 符号位运算 ----------

    /// <summary>按位与，结果长度取两者较小值。</summary>
    public static BigBitField operator &(BigBitField a, BigBitField b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        var result = new BigBitField(Math.Min(a._bitLength, b._bitLength));
        for (int i = 0; i < result._bytes.Length; i++)
            result._bytes[i] = (byte)(a.GetByte(i) & b.GetByte(i));
        return result;
    }

    /// <summary>按位或，结果长度取两者较大值。</summary>
    public static BigBitField operator |(BigBitField a, BigBitField b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        var result = new BigBitField(Math.Max(a._bitLength, b._bitLength));
        for (int i = 0; i < result._bytes.Length; i++)
            result._bytes[i] = (byte)(a.GetByte(i) | b.GetByte(i));
        return result;
    }

    /// <summary>按位异或，结果长度取两者较大值。</summary>
    public static BigBitField operator ^(BigBitField a, BigBitField b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        var result = new BigBitField(Math.Max(a._bitLength, b._bitLength));
        for (int i = 0; i < result._bytes.Length; i++)
            result._bytes[i] = (byte)(a.GetByte(i) ^ b.GetByte(i));
        return result;
    }

    /// <summary>按位取反（在自身位长范围内翻转，长度不变）。</summary>
    public static BigBitField operator ~(BigBitField a)
    {
        ArgumentNullException.ThrowIfNull(a);
        var result = new BigBitField(a._bitLength);
        for (int i = 0; i < result._bytes.Length; i++)
            result._bytes[i] = (byte)~a.GetByte(i);
        return result;
    }

    /// <summary>左移 n 位（相当于乘 2^n），结果自动扩容；n 为负等价右移。</summary>
    public static BigBitField operator <<(BigBitField a, int n)
    {
        ArgumentNullException.ThrowIfNull(a);
        if (n < 0) return a >> -n;

        var result = new BigBitField((long)a._bitLength + n > int.MaxValue
            ? throw new OverflowException("左移后位长超出 int 范围")
            : a._bitLength + n);
        if (n == 0 || a.IsZero) { Array.Copy(a._bytes, result._bytes, a._bytes.Length); return result; }

        long byteShift = (long)n >> 3;
        int bitShift = n & 7;
        for (int i = a._bytes.Length - 1; i >= 0; i--)
        {
            int b = a._bytes[i];
            if (b == 0) continue;
            long dest = i + byteShift;
            result._bytes[dest] |= (byte)(b << bitShift);
            if (bitShift > 0 && dest + 1 < result._bytes.Length)
                result._bytes[dest + 1] |= (byte)(b >> (8 - bitShift));
        }
        return result;
    }

    /// <summary>右移 n 位（无符号，高位补 0），逻辑长度相应缩短；n 为负等价左移。</summary>
    public static BigBitField operator >>(BigBitField a, int n)
    {
        ArgumentNullException.ThrowIfNull(a);
        if (n < 0) return a << -n;
        if (n >= a._bitLength) return new BigBitField(0);

        var result = new BigBitField(a._bitLength - n);
        if (n == 0) { Array.Copy(a._bytes, result._bytes, a._bytes.Length); return result; }

        long byteShift = (long)n >> 3;
        int bitShift = n & 7;
        for (int i = 0; i < result._bytes.Length; i++)
        {
            int b = a.GetByte(i + byteShift) >> bitShift;
            if (bitShift > 0)
                b |= a.GetByte(i + byteShift + 1) << (8 - bitShift);
            result._bytes[i] = (byte)b;
        }
        return result;
    }

    // ---------- 内部工具 ----------

    /// <summary>读取第 byteIndex 个字节，超出范围返回 0（无限零扩展）。</summary>
    private int GetByte(long byteIndex)
        => (ulong)byteIndex < (ulong)_bytes.Length ? _bytes[byteIndex] : 0;

    private bool IsZero
    {
        get
        {
            foreach (byte b in _bytes)
                if (b != 0) return false;
            return true;
        }
    }

    private static int BytesForLength(long bitLength)
        => bitLength == 0 ? 1 : (int)((bitLength + 7) / 8);
}
