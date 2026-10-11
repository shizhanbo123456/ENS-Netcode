using System;
using Unity.Profiling;
using Unity.Profiling.Editor;

[Serializable]
[ProfilerModuleMetadata("ENS 网络统计")]
public class EnsProfilerModule : ProfilerModule
{
    // 折线只放每帧收发字节数。这两条是本模块自己的固定计数器，用字符串指定，与业务代码无关；
    // 每个远程调用函数的次数是动态名字，放不进折线，交给详情面板。
    static readonly ProfilerCounterDescriptor[] k_ChartCounters =
    {
        new ProfilerCounterDescriptor("SentBytes", new ProfilerCategory("ENS")),
        new ProfilerCounterDescriptor("ReceivedBytes", new ProfilerCategory("ENS")),
    };

    public EnsProfilerModule() : base(k_ChartCounters)
    {
    }

    public override ProfilerModuleViewController CreateDetailsViewController()
    {
        return new EnsProfilerDetailsViewController(ProfilerWindow);
    }
}
