using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Profiling;

// ENS 的性能计数器。对外只认字符串：生成的代码调用 CountInvoke("类名 :函数名")，
// 本类不认识任何具体的远程调用函数，所以加函数既不用改这里，也不用改 Profiler 模块。
public static class EnsProfilerCounters
{
    public const string CategoryName = "ENS";
    public const string SentBytesName = "SentBytes";
    public const string ReceivedBytesName = "ReceivedBytes";

    const int k_MaxInvokeCounterCount = 4096;

    static readonly ProfilerCategory k_Category = new ProfilerCategory(CategoryName);
    const ProfilerCounterOptions k_Options = ProfilerCounterOptions.FlushOnEndOfFrame;

    static readonly ProfilerCounterValue<int> s_SentBytes = new ProfilerCounterValue<int>(k_Category, SentBytesName, ProfilerMarkerDataUnit.Bytes, k_Options);
    static readonly ProfilerCounterValue<int> s_ReceivedBytes = new ProfilerCounterValue<int>(k_Category, ReceivedBytesName, ProfilerMarkerDataUnit.Bytes, k_Options);

    static readonly Dictionary<string, int> s_InvokeIndexByName = new Dictionary<string, int>(StringComparer.Ordinal);
    static readonly ProfilerCounterValue<int>[] s_InvokeCounters = new ProfilerCounterValue<int>[k_MaxInvokeCounterCount];

    // 三个对外入口只在编辑器生效，其它构建里编译器会把调用点整个消掉
    [Conditional("UNITY_EDITOR")]
    public static void CountInvoke(string name)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("远程调用的名字不能为空", nameof(name));

        if (!s_InvokeIndexByName.TryGetValue(name, out int index))
            index = RegisterInvokeCounter(name);

        s_InvokeCounters[index].Value += 1;
    }

    // 首次见到某个名字时才建计数器，所以加远程调用函数不需要改这里的任何代码
    static int RegisterInvokeCounter(string name)
    {
        if (s_InvokeIndexByName.Count >= k_MaxInvokeCounterCount)
            throw new InvalidOperationException($"ENS 的远程调用计数器数量超过上限 {k_MaxInvokeCounterCount}，请调大 EnsProfilerCounters.k_MaxInvokeCounterCount");

        int index = s_InvokeIndexByName.Count;
        s_InvokeIndexByName.Add(name, index);
        s_InvokeCounters[index] = new ProfilerCounterValue<int>(k_Category, name, ProfilerMarkerDataUnit.Count, k_Options);
        EnsProfilerNameCache.Remember(name);
        return index;
    }

    [Conditional("UNITY_EDITOR")]
    public static void AddSentBytes(int byteCount)
    {
        s_SentBytes.Value += byteCount;
    }

    [Conditional("UNITY_EDITOR")]
    public static void AddReceivedBytes(int byteCount)
    {
        s_ReceivedBytes.Value += byteCount;
    }

    // FlushOnEndOfFrame 只上报帧内最后一次写入值、不会自动清零，所以每帧开头必须清零，
    // 否则折线是一条单调递增的斜线，看不到"每帧多少次"
    public static void BeginFrame()
    {
        s_SentBytes.Value = 0;
        s_ReceivedBytes.Value = 0;

        for (int i = 0; i < s_InvokeIndexByName.Count; i++)
            s_InvokeCounters[i].Value = 0;
    }
}
