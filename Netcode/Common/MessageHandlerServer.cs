using System;
using System.Collections.Generic;
using Utils;

public static class MessageHandlerServer
{
    private static Action<EnsConnection, byte[], Segment> AnyHeader;
    private static Dictionary<byte, Action<EnsConnection, byte[], Segment>> Events = new();
    public static void RegistAny(Action<EnsConnection, byte[], Segment> action)
    {
        AnyHeader += action;
    }
    public static void Regist(byte header, Action<EnsConnection, byte[], Segment> action)
    {
        if (Events.ContainsKey(header))
        {
            Debug.Log("重复注册消息头：" + header);
            return;
        }
        Events.Add(header, action);
    }
    public static void Invoke(EnsConnection conn, byte[] src,Segment segment)
    {
        byte header = MessageReader.Header(src,segment);
        if (!Events.ContainsKey(header))
        {
            Debug.Log("未注册消息头：" + header);
            return;
        }
        AnyHeader?.Invoke(conn, src, segment);
        Events[header].Invoke(conn,src,segment);
    }
}