using System;
using System.Collections.Generic;
using Utils;

public static class MessageHandlerClient
{
    private static Action<byte[], Segment> AnyHeader;
    private static Dictionary<byte,Action<byte[], Segment>>Events=new();
    public static void RegistAny(Action<byte[], Segment> action)
    {
        AnyHeader += action;
    }
    public static void Regist(byte header, Action<byte[],Segment> action)
    {
        if (Events.ContainsKey(header))
        {
            Debug.Log("重复注册消息头：" + header);
            return;
        }
        Events.Add(header,action);
    }
    public static void Invoke(byte[] src,Segment segment)
    {
        byte header = MessageReader.Header(src, segment);
        if (!Events.ContainsKey(header))
        {
            Debug.Log("未注册消息头：" + header);
            return;
        }
        AnyHeader?.Invoke(src,segment);
        Events[header].Invoke(src, segment);
    }
}