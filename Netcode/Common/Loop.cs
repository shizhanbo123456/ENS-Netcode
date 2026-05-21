using ProtocolWrapper;
using Utils;

public class Loop
{
    public static void InitCommon()
    {
        Time.Init();
    }
#if UNITY_2017_1_OR_NEWER
    public static void InitClient()
    {

    }
#endif
    public static void InitServer()
    {

    }
    public static void LoopCommon()
    {
        Time.Update();
        FrameCounter.Update();
    }
#if UNITY_2017_1_OR_NEWER
    public static void LoopClient()
    {
        Broadcast.Update();
        EnsClientRequest.Update();
    }
#endif
    public static void LoopServer()
    {

    }
}