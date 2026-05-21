using System.Collections.Generic;
using Utils;
public partial class BytesPool
{
    public static ObjectPool<byte[]> BytePool4 = new ObjectPool<byte[]>(() => new byte[4]);
    public static ObjectPool<byte[]> BytePool8 = new ObjectPool<byte[]>(() => new byte[8]);
    public static ObjectPool<byte[]> BytePool16 = new ObjectPool<byte[]>(() => new byte[16]);
    public static ObjectPool<byte[]> BytePool32 = new ObjectPool<byte[]>(() => new byte[32]);
    public static ObjectPool<byte[]> BytePool64 = new ObjectPool<byte[]>(() => new byte[64]);
    public static ObjectPool<byte[]> BytePool128 = new ObjectPool<byte[]>(() => new byte[128]);
    public static ObjectPool<byte[]> BytePool256 = new ObjectPool<byte[]>(() => new byte[256]);
    public static ObjectPool<byte[]> BytePool512 = new ObjectPool<byte[]>(() => new byte[512]);
    public static ObjectPool<byte[]> BytePool1024 = new ObjectPool<byte[]>(() => new byte[1024]);
    public static SortedDictionary<int, byte[]> BigByteBuffer = new SortedDictionary<int, byte[]>();

    public static byte[] GetBuffer(int length)
    {
        if (length <= 64)
        {
            if (length <= 4) return BytePool4.Get();
            if (length <= 8) return BytePool8.Get();
            if (length <= 16) return BytePool16.Get();
            if (length <= 32) return BytePool32.Get();
            return BytePool64.Get();
        }
        else if (length <= 1024)
        {
            if (length <= 128) return BytePool128.Get();
            if (length <= 256) return BytePool256.Get();
            if (length <= 512) return BytePool512.Get();
            return BytePool1024.Get();
        }
        else
        {
            if (BigByteBuffer.ContainsKey(length))
            {
                var buffer = BigByteBuffer[length];
                BigByteBuffer.Remove(length);
                return buffer;
            }
            else
            {
                return new byte[length];
            }
        }
    }
    public static void ReturnBuffer(byte[] buffer)
    {
        int length = buffer.Length;
        for (int i = 0; i < length; i++) buffer[i] = 0x00;
        if (length < 128)
        {
            if (length >= 64) { BytePool64.Return(buffer); TooManyStoredCheck(BytePool64); return; }
            if (length >= 32) { BytePool32.Return(buffer); TooManyStoredCheck(BytePool32); return; }
            if (length >= 16) { BytePool16.Return(buffer); TooManyStoredCheck(BytePool16); return; }
            if (length >= 8) { BytePool8.Return(buffer); TooManyStoredCheck(BytePool8); return; }
            if (length >= 4) { BytePool4.Return(buffer); TooManyStoredCheck(BytePool4); return; }
            //<4的直接丢弃
        }
        else if (length <= 1024)
        {
            if (length == 1024) { BytePool1024.Return(buffer); TooManyStoredCheck(BytePool1024); return; }
            if (length >= 512) { BytePool512.Return(buffer); TooManyStoredCheck(BytePool512); return; }
            if (length >= 256) { BytePool256.Return(buffer); TooManyStoredCheck(BytePool256); return; }
            if (length >= 128) { BytePool128.Return(buffer); TooManyStoredCheck(BytePool128); return; }
        }
        else
        {
            if (!BigByteBuffer.ContainsKey(length)) BigByteBuffer.Add(length, buffer);
            else
            {
                do
                {
                    length--;
                    if (!BigByteBuffer.ContainsKey(length))
                    {
                        BigByteBuffer.Add(length, buffer);
                        return;
                    }
                }
                while (length > 1024);
                BytePool1024.Return(buffer);
            }
        }
    }
    static partial void TooManyStoredCheck(ObjectPool<byte[]> pool);
}
#if UNITY_EDITOR
public partial class BytesPool
{
    static partial void TooManyStoredCheck(ObjectPool<byte[]> pool)
    {
        if (pool.Count > 50) Debug.LogWarning("存在字节池存入了过多对象");
    }
}
#endif