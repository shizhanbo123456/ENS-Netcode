// 把注册过的远程调用名字缓存进编辑器的会话状态（SessionState），
// 这样退出 Play 模式、甚至域重载之后，Profiler 的详情面板仍能列出清单继续查看。
// SessionState 只活在编辑器进程里，不是文件、不进 Assets；打包后的 Player 没有这一步。
public static class EnsProfilerNameCache
{
    // 详情面板按同样的字面量读这个键，双方没有代码引用关系
    public const string Key = "EnsProfiler.InvokeNames";

    public static void Reset()
    {
#if UNITY_EDITOR
        UnityEditor.SessionState.EraseString(Key);
#endif
    }

    public static void Remember(string name)
    {
#if UNITY_EDITOR
        string all = UnityEditor.SessionState.GetString(Key, string.Empty);

        if (all.Length == 0)
        {
            UnityEditor.SessionState.SetString(Key, name);
            return;
        }

        if (("\n" + all + "\n").Contains("\n" + name + "\n"))
            return;

        UnityEditor.SessionState.SetString(Key, all + "\n" + name);
#endif
    }
}
