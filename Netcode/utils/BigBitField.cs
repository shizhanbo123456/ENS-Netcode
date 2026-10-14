using System;

// 任意位数的位域。小端存储：[有效数据 Count 位][标识位 1][空数据 0...]，标识位固定在 bit index = Count，
// 故载荷自描述：接收方找最高 1 位即还原位数，无需长度字段，带宽开销仅 1 bit；空数据无标识位，导出 0 字节。
// 缓冲来自 BytesPool，Dispose 后 _buf 为 null。
public sealed class BigBitField : IDisposable
{
    private byte[] _buf;  // 小端字节序，_buf[0] 是最低 8 位
    private int _count;   // 有效数据位数；标识位位于 bit index _count
    public int Count => _count;// 有效数据位数（不含标识位）

    // capacityBits 为初始数据容量（位），写入超出时倍增扩容
    public BigBitField(int capacityBits = 64)
    {
        if (capacityBits < 0) throw new ArgumentOutOfRangeException(nameof(capacityBits));
        _count = 0;
        // 池内数组已清零，长度按档位可能大于请求值
        _buf = BytesPool.GetBuffer(Math.Max(1, (capacityBits + 8) / 8));
    }

    // 归还缓冲并置 _buf = null（null 即已销毁），重复调用安全
    public void Dispose()
    {
        if (_buf == null) return;
        BytesPool.ReturnBuffer(_buf);
        _buf = null;
        _count = 0;
    }

    // 在最高位追加 1 位：只改数据位和新标识位，无数据搬移，O(1)
    public void WriteBit(bool value)
    {
        EnsureCapacity(_count + 2);
        if (value) _buf[_count >> 3] |= (byte)(1 << (_count & 7));
        else _buf[_count >> 3] &= (byte)~(1 << (_count & 7)); // 该位置原是旧标识位，写 0 时需显式清除
        _buf[(_count + 1) >> 3] |= (byte)(1 << ((_count + 1) & 7)); // 新标识位
        _count++;
    }

    // 追加 value 的低 bitCount 位（最低位先写、落在最低处）
    public void WriteBits(ulong value, int bitCount)
    {
        if (bitCount < 0 || bitCount > 64) throw new ArgumentOutOfRangeException(nameof(bitCount));
        for (int i = 0; i < bitCount; i++)
            WriteBit((value & (1ul << i)) != 0);
    }

    // 读第 index 位有效数据（0 = 最早写入的位）
    public bool this[int index]
    {
        get
        {
            if (index < 0 || index >= _count) throw new ArgumentOutOfRangeException(nameof(index));
            return (_buf[index >> 3] & (1 << (index & 7))) != 0;
        }
    }

    // 导出载荷 = 有效数据 + 标识位，共 Count/8 + 1 字节，空位域返回 Array.Empty。
    // 返回的是池缓冲，长度按档位可能大于载荷：须 BytesPool.ReturnBuffer 归还，载荷长度以 Count 推算而非数组 Length。
    public byte[] ToBytes()
    {
        if (_buf == null) throw new ObjectDisposedException(nameof(BigBitField)); // 已销毁与空位域都是 Count==0，必须靠 _buf 区分
        if (_count == 0) return Array.Empty<byte>();
        int len = (_count >> 3) + 1;
        var bytes = BytesPool.GetBuffer(len);
        Array.Copy(_buf, bytes, len);
        return bytes;
    }

    // 从载荷还原（与 ToBytes 配对）：标识位 = 最高 1 位，往上找第一个非 0 字节即可；全 0 载荷还原为空位域
    public static BigBitField FromBytes(byte[] bytes)
    {
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

    // 按位与，取较短长度
    public static BigBitField operator &(BigBitField a, BigBitField b)
    {
        int len = Math.Min(a._count, b._count);
        var result = new BigBitField(len + 16);
        Combine(result, a, b, len, (x, y) => x & y);
        return result;
    }

    // 按位或，取较长长度
    public static BigBitField operator |(BigBitField a, BigBitField b)
    {
        int len = Math.Max(a._count, b._count);
        var result = new BigBitField(len + 16);
        Combine(result, a, b, len, (x, y) => x | y);
        return result;
    }

    // 按位异或，取较长长度
    public static BigBitField operator ^(BigBitField a, BigBitField b)
    {
        int len = Math.Max(a._count, b._count);
        var result = new BigBitField(len + 16);
        Combine(result, a, b, len, (x, y) => x ^ y);
        return result;
    }

    // 按位取反，长度不变
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

    // 左移 n 位（低位补 0，长度 +n）；n 为负等价右移
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

    // 右移 n 位（高位补 0，长度 -n）；n 为负等价左移
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
        int newSize = Math.Max(needed, Math.Max(4, _buf.Length * 2));
        // 扩容走池：取新缓冲 → 拷贝 → 归还旧的
        var grown = BytesPool.GetBuffer(newSize);
        Array.Copy(_buf, grown, _buf.Length);
        BytesPool.ReturnBuffer(_buf);
        _buf = grown;
    }

    // 清除 len..len+7 的杂散位（结果最高数据字节的掩码）
    private static void MaskTop(BigBitField f, int len)
    {
        int rem = len & 7;
        if (rem != 0) f._buf[len >> 3] &= (byte)((1 << rem) - 1);
    }

    // 在 bit index = count 处设标识位
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

    // 字节内最高 1 位的偏移（0..7），输入非 0
    private static int HighestBitOffset(int b)
    {
        for (int i = 7; i >= 0; i--)
            if ((b & (1 << i)) != 0) return i;
        return -1;
    }
}
