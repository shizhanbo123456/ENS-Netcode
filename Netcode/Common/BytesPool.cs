using System.Collections.Generic;
using Utils;
public class BytesPool
{
    private static List<int> validArrayLength = new()
    {
        4,8,16,32,64,128,256,512,1024,
        1400,2048
    };
    private static Dictionary<int, ObjectPool<byte[]>> pools = new Dictionary<int, ObjectPool<byte[]>>();

    static BytesPool()
    {
        //校验分档表是否有序，无序会导致GetBuffer/ReturnBuffer定位到错误的池
        for (int i = 1; i < validArrayLength.Count; i++)
        {
            if (validArrayLength[i] <= validArrayLength[i - 1])
            {
                Debug.LogWarning($"validArrayLength存在无序或重复项：{validArrayLength[i]}，已自动排序修复");
                validArrayLength.Sort();
                break;
            }
        }
        foreach (var length in validArrayLength)
        {
            if (!pools.ContainsKey(length))
            {
                var len= length;
                pools.Add(length, new ObjectPool<byte[]>(() => new byte[len]));
            }
        }
    }

    public static byte[] GetBuffer(int length)
    {
        if(length<validArrayLength[0])return new byte[length];
        if(length>validArrayLength[validArrayLength.Count-1])return new byte[length];
        int lengthIndex = 0;
        while(validArrayLength[lengthIndex] < length)
        {
            lengthIndex++;
        }
        return pools[validArrayLength[lengthIndex]].Get();
    }
    public static void ReturnBuffer(byte[] buffer)
    {
        if (buffer.Length < validArrayLength[0]) return;
        if (buffer.Length > validArrayLength[validArrayLength.Count - 1]) return;
        for (int i = 0; i < buffer.Length; i++) buffer[i] = 0x00;
        int lengthIndex = validArrayLength.Count - 1;
        while (buffer.Length < validArrayLength[lengthIndex])
        {
            lengthIndex--;
        }
        pools[validArrayLength[lengthIndex]].Return(buffer);
        TooManyStoredCheck(pools[validArrayLength[lengthIndex]]);
    }
    private static bool toMangWarningLogged = false;
    private static void TooManyStoredCheck(ObjectPool<byte[]> pool)
    {
        if (!toMangWarningLogged)
        {
            if (pool.Count > 50)
            {
                var t = pool.Get();
                int length = t.Length;
                pool.Return(t);
                Debug.LogWarning($"存在字节池存入了过多对象，首个超限池长度：{length}");
                toMangWarningLogged = true;
            }
        }
    }
}