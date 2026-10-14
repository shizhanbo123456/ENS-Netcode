using System;

// 任意位数的位域。内部小端只存有效数据（Count 位），标识位不占内部缓冲，仅由 ToBytes() 在导出时
// 补在最高有效位之后、用于标识有效范围：载荷长 Count/8 + 1 字节，接收方（FromBytes）找最高 1 位即还原位数；
// 空数据无标识位、导出 0 字节。缓冲来自 BytesPool，Dispose 后 _buf 为 null。
public sealed class BigBitField : IDisposable
{
    private byte[] _buf;  // 小端字节序，_buf[0] 是最低 8 位；≥ _count 的位恒为 0
    private int _count;   // 有效数据位数
    public int Count => _count;// 有效数据位数（不含标识位）

    // capacityBits 为初始数据容量（位），写入超出时倍增扩容
    public BigBitField(int capacityBits = 64)
    {
        if (capacityBits < 0) throw new ArgumentOutOfRangeException(nameof(capacityBits));
        _count = 0;
        // 池内数组已清零，长度按档位可能大于请求值
        _buf = BytesPool.GetBuffer(Math.Max(1, (capacityBits + 7) / 8)); // 只按数据位算，不含标识位
    }

    // 归还缓冲并置 _buf = null（null 即已销毁），重复调用安全
    public void Dispose()
    {
        if (_buf == null) return;
        BytesPool.ReturnBuffer(_buf);
        _buf = null;
        _count = 0;
    }

    // 在最高位追加 1 位：只改数据位，无数据搬移，O(1)
    public void WriteBit(bool value)
    {
        EnsureCapacity(_count + 1);
        if (value) _buf[_count >> 3] |= (byte)(1 << (_count & 7));
        else _buf[_count >> 3] &= (byte)~(1 << (_count & 7)); // 该位可能残留旧数据，写 0 时显式清除
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
    // 标识位在导出时才补上（内部不存它）；返回的是池缓冲、长度按档位可能大于载荷：须 BytesPool.ReturnBuffer 归还，
    // payloadBytes 回传真正有效的字节数（= Count/8 + 1），长度一律以它为准，不要用返回数组的 Length。
    public byte[] ToBytes(out int payloadBytes)
    {
        if (_buf == null) throw new ObjectDisposedException(nameof(BigBitField)); // 已销毁与空位域都是 Count==0，必须靠 _buf 区分
        if (_count == 0) { payloadBytes = 0; return Array.Empty<byte>(); }
        int len = (_count >> 3) + 1;                       // 载荷 = 数据字节 + 标识位所在字节
        var bytes = BytesPool.GetBuffer(len);
        Array.Copy(_buf, 0, bytes, 0, Math.Min(_buf.Length, len));
        bytes[_count >> 3] |= (byte)(1 << (_count & 7));    // 补标识位
        payloadBytes = len;
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
        int rem = markerIndex & 7;
        result._buf[top] &= (byte)((1 << rem) - 1); // 连标识位本身一起清掉，内部只留数据
        return result;
    }

    // ---------- 位运算（作用于双方的有效数据区） ----------

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
        result._count = len;
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
        int dataBytes = (len + 7) >> 3;
        for (int i = dataBytes - 1; i >= 0; i--)
        {
            int b = a.GetByte(i); // 内部 ≥ len 的位恒为 0，无需再屏蔽
            if (b == 0) continue;
            long dest = i + byteShift;
            result._buf[dest] |= (byte)(b << bitShift);
            if (bitShift > 0 && dest + 1 < result._buf.Length)
                result._buf[dest + 1] |= (byte)(b >> (8 - bitShift));
        }
        result._count = newLen;
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
                b |= a.GetByte(i + byteShift + 1) << (8 - bitShift); // 超出 Count 的位恒为 0
            result._buf[i] = (byte)b;
        }
        MaskTop(result, newLen);
        result._count = newLen;
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

    // 清除 ≥ len 的位（结果最高数据字节的掩码）
    private static void MaskTop(BigBitField f, int len)
    {
        int rem = len & 7;
        if (rem != 0) f._buf[len >> 3] &= (byte)((1 << rem) - 1);
    }

    private static void Combine(BigBitField result, BigBitField a, BigBitField b, int len, Func<int, int, int> op)
    {
        int dataBytes = (len + 7) >> 3;
        for (int i = 0; i < dataBytes; i++)
            result._buf[i] = (byte)op(a.GetByte(i), b.GetByte(i));
        MaskTop(result, len);
        result._count = len;
    }

    // 字节内最高 1 位的偏移（0..7），输入非 0
    private static int HighestBitOffset(int b)
    {
        for (int i = 7; i >= 0; i--)
            if ((b & (1 << i)) != 0) return i;
        return -1;
    }
}
