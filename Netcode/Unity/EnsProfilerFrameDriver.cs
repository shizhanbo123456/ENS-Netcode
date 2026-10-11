using UnityEngine;

// 每帧最前面把逐帧计数器清零。必须早于本帧任何收发，所以执行顺序给到极小。
// 用协程/PlayerLoop 注入都更绕，所以直接用一个自动挂载的常驻物体。
[DefaultExecutionOrder(-10000)]
public class EnsProfilerFrameDriver : MonoBehaviour
{
    void Update()
    {
        EnsProfilerCounters.BeginFrame();
    }

    // 进 Play 时自动挂一个，并清掉上一轮留下来的名字清单，让本轮重新累积。
    // 只有编辑器需要这个物体（统计相关的调用见 EnsProfilerCounters 上的 [Conditional]）
#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        EnsProfilerNameCache.Reset();

        var driverObject = new GameObject("EnsProfilerFrameDriver");
        UnityEngine.Object.DontDestroyOnLoad(driverObject);
        driverObject.AddComponent<EnsProfilerFrameDriver>();
    }
#endif
}
