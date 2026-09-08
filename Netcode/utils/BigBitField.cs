using System;

/// <summary>
/// 支持任意位数的位运算数据结构，支持逐位写入，内部缓冲可直接转 byte[] 网络传输。
///
/// 存储布局（小端字节序）：[有效数据 bits 0..Count-1][标识位 1][空数据 0...]
/// 标识位固定位于 bit index = Count，且始终是存储中的最高 1 位：
/// - 写入：数据位写到 Count 处，标识位上移一位（每写 1 位 O(1)，无数据搬移）；
/// - ToBytes()：只导出到标识位所在字节；接收方找载荷最高 1 位即可还原有效位数，
///   无需额外长度字段，带宽开销仅 1 bit；
/// - 空数据 = 无标识位，ToBytes() 为 0 字节，与"数据全 0"（有标识位）天然区分。
/// </summary>
public sealed class BigBitField : IDisposable
{
    private byte[] _buf;  // 小端字节序，_buf[0] 是最低 8 位
    private int _count;   // 有效数据位数；标识位位于 bit index _count

    /// <summary>创建空位域。capacityBits 为初始数据容量（位），写入超出时按倍增扩容。</summary>
    public BigBitField(int capacityBits = 64)
    {
        if (capacityBits < 0) throw new ArgumentOutOfRangeException(nameof(capacityBits));
        _count = 0;
        _buf = new byte[Math.Max(1, (capacityBits + 8) / 8)]; // 容纳 capacityBits 数据 + 1 标识位
    }

    /// <summary>销毁：释放内部存储（托管数组，之后不可再使用本实例）。</summary>
    public void Dispose()
    {
        _buf = Array.Empty<byte>();
        _count = 0;
    }

    /// <summary>有效数据位数（不含标识位）。最新写入的位位于 Count-1（最高位）。</summary>
    public int Count => _count;

    /// <summary>
    /// 在最高位追加 1 位（最新写入的数据位于最高位，有效长度 +1）。
    /// 只改 2 个位（数据位 + 新标识位），无任何数据搬移，O(1) 摊还。
    /// </summary>
    public void WriteBit(bool value)
    {
        EnsureCapacity(_count + 2);
        if (value) _buf[_count >> 3] |= (byte)(1 << (_count & 7));
        else _buf[_count >> 3] &= (byte)~(1 << (_count & 7)); // 该位置原是旧标识位，写 0 时需显式清除
        _buf[(_count + 1) >> 3] |= (byte)(1 << ((_count + 1) & 7)); // 新标识位
        _count++;
    }

    /// <summary>追加 value 的低 bitCount 位（value 的最低位先写入、落在最低处）。</summary>
    public void WriteBits(ulong value, int bitCount)
    {
        if (bitCount < 0 || bitCount > 64) throw new ArgumentOutOfRangeException(nameof(bitCount));
        for (int i = 0; i < bitCount; i++)
            WriteBit((value & (1ul << i)) != 0);
    }

    /// <summary>读取第 index 位有效数据（0 = 最早写入的位）。标识位不可通过此接口访问。</summary>
    public bool this[int index]
    {
        get
        {
            if (index < 0 || index >= _count) throw new ArgumentOutOfRangeException(nameof(index));
            return (_buf[index >> 3] & (1 << (index & 7))) != 0;
        }
    }

    /// <summary>
    /// 导出载荷字节：有效数据 + 标识位，共 Count/8 + 1 字节（比纯数据多至多 1 字节）。
    /// 标识位之上（空数据区）不导出；空数据导出为 0 字节。
    /// </summary>
    public byte[] ToBytes()
    {
        if (_count == 0) return Array.Empty<byte>();
        var bytes = new byte[(_count >> 3) + 1];
        Array.Copy(_buf, bytes, bytes.Length);
        return bytes;
    }

    /// <summary>
    /// 从载荷字节构造（与 ToBytes 配对）。标识位 = 载荷最高 1 位：
    /// 标识位之上恒为 0，故从最高字节向下找第一个非 0 字节即是，O(1)。
    /// 空/全 0 载荷还原为空位域。
    /// </summary>
    public static BigBitField FromBytes(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        int top = bytes.Length - 1;
        while (top >= 0 && bytes[top] == 0) top--;
        if (top < 0) return new BigBitField(); // 无标识位 → 空数据

        int markerIndex = top * 8 + HighestBitOffset(bytes[top]);
        var result = new BigBitField(markerIndex + 16);
        Array.Copy(bytes, result._buf, Math.Min(bytes.Length, result._buf.Length));
        result._count = markerIndex;
        int rem = markerIndex & 7; // 防御：清除标识位之上的残留位
        result._buf[top] &= (byte)((1 << (rem + 1)) - 1);
        return result;
    }

    // ---------- 符号位运算（操作双方的有效数据区，结果自动重建标识位） ----------

    /// <summary>按位与，结果有效长度取两者较小值。</summary>
    public static BigBitField operator &(BigBitField a, BigBitField b)
    {
        int len = Math.Min(a._count, b._count);
        var result = new BigBitField(len + 16);
        Combine(result, a, b, len, (x, y) => x & y);
        return result;
    }

    /// <summary>按位或，结果有效长度取两者较大值。</summary>
    public static BigBitField operator |(BigBitField a, BigBitField b)
    {
        int len = Math.Max(a._count, b._count);
        var result = new BigBitField(len + 16);
        Combine(result, a, b, len, (x, y) => x | y);
        return result;
    }

    /// <summary>按位异或，结果有效长度取两者较大值。</summary>
    public static BigBitField operator ^(BigBitField a, BigBitField b)
    {
        int len = Math.Max(a._count, b._count);
        var result = new BigBitField(len + 16);
        Combine(result, a, b, len, (x, y) => x ^ y);
        return result;
    }

    /// <summary>按位取反（在有效数据范围内翻转，长度不变）。</summary>
    public static BigBitField operator ~(BigBitField a)
    {
        int len = a._count;
        var result = new BigBitField(len + 16);
        int dataBytes = (len + 7) >> 3;
        for (int i = 0; i < dataBytes; i++)
            result._buf[i] = (byte)~a.GetByte(i);
        MaskTop(result, len);
        SetMarker(result, len);
        return result;
    }

    /// <summary>左移 n 位（数据整体上移，低位补 0，有效长度 +n）；n 为负等价右移。</summary>
    public static BigBitField operator <<(BigBitField a, int n)
    {
        if (n < 0) return a >> -n;
        int len = a._count;
        if ((long)len + n > int.MaxValue - 8) throw new OverflowException("左移后位长超出 int 范围");
        int newLen = len + n;
        var result = new BigBitField(newLen + 16);
        if (len == 0) return result; // 空数据左移仍为空

        int byteShift = n >> 3, bitShift = n & 7;
        int dataBytes = (len + 7) >> 3, rem = len & 7;
        for (int i = dataBytes - 1; i >= 0; i--)
        {
            int b = a.GetByte(i);
            if (i == dataBytes - 1 && rem != 0) b &= (1 << rem) - 1; // 排除旧标识位
            if (b == 0) continue;
            long dest = i + byteShift;
            result._buf[dest] |= (byte)(b << bitShift);
            if (bitShift > 0 && dest + 1 < result._buf.Length)
                result._buf[dest + 1] |= (byte)(b >> (8 - bitShift));
        }
        SetMarker(result, newLen);
        return result;
    }

    /// <summary>右移 n 位（数据整体下移，高位补 0，有效长度 -n）；n 为负等价左移。</summary>
    public static BigBitField operator >>(BigBitField a, int n)
    {
        if (n < 0) return a << -n;
        int len = a._count;
        if (n >= len) return new BigBitField(); // 全部移出 → 空

        int newLen = len - n;
        var result = new BigBitField(newLen + 16);
        int byteShift = n >> 3, bitShift = n & 7;
        int destBytes = ((newLen + 7) >> 3);
        for (int i = 0; i < destBytes; i++)
        {
            int b = a.GetByte(i + byteShift) >> bitShift;
            if (bitShift > 0)
                b |= a.GetByte(i + byteShift + 1) << (8 - bitShift); // 旧标识位恰好落入新标识位位置，无害
            result._buf[i] = (byte)b;
        }
        MaskTop(result, newLen);
        SetMarker(result, newLen);
        return result;
    }

    // ---------- 内部工具 ----------

    private int GetByte(int byteIndex)
        => (uint)byteIndex < (uint)_buf.Length ? _buf[byteIndex] : 0;

    private void EnsureCapacity(int bits)
    {
        int needed = (bits + 7) >> 3;
        if (needed <= _buf.Length) return;
        Array.Resize(ref _buf, Math.Max(needed, Math.Max(4, _buf.Length * 2)));
    }

    /// <summary>把 len..len+7 范围内的杂散位清零（结果最高数据字节的掩码）。</summary>
    private static void MaskTop(BigBitField f, int len)
    {
        int rem = len & 7;
        if (rem != 0) f._buf[len >> 3] &= (byte)((1 << rem) - 1);
    }

    /// <summary>在 bit index = count 处设置标识位。</summary>
    private static void SetMarker(BigBitField f, int count)
    {
        f._count = count;
        if (count > 0) f._buf[count >> 3] |= (byte)(1 << (count & 7));
    }

    private static void Combine(BigBitField result, BigBitField a, BigBitField b, int len, Func<int, int, int> op)
    {
        int dataBytes = (len + 7) >> 3;
        for (int i = 0; i < dataBytes; i++)
            result._buf[i] = (byte)op(a.GetByte(i), b.GetByte(i));
        MaskTop(result, len);
        SetMarker(result, len);
    }

    /// <summary>字节内最高 1 位的偏移（0..7）；输入非 0。</summary>
    private static int HighestBitOffset(int b)
    {
        for (int i = 7; i >= 0; i--)
            if ((b & (1 << i)) != 0) return i;
        return -1;
    }
}
