using Ens.ProtoFormat;
using System;
using System.Collections.Generic;

public class ClientRoomEventCenter
{
    private static Dictionary<int,Action<string>>_events=new Dictionary<int,Action<string>>();
    public static void Register(int type,Action<string>action)
    {
        if (_events.ContainsKey(type))
        {
            _events[type] += action;
        }
        else
        {
            _events.Add(type, action);
        }
    }
    internal static void TrigEvent(int type,string content)
    {
        if (_events.TryGetValue(type,out var e))
        {
            e.Invoke(content);
        }
        else
        {
            Utils.Debug.LogError($"Room event {type} has no register");
        }
    }
    public static void TrigRpcEvent(Delivery delivery, int header, string content)
    {
        if (EnsInstance.Corr != null && EnsInstance.Corr.Client != null)
        {
            EnsInstance.Corr.Client.Send(delivery, new MessageBody_N_CS { type = header, content = content }, Pkt.csN);
        }
        else
        {
            Utils.Debug.LogError("当前状态不能发送消息");
        }
    }
}
public class ClientRoomManagerEventCenter
{
    private static Dictionary<int, Action<string>> _events = new Dictionary<int, Action<string>>();
    public static void Register(int type, Action<string> action)
    {
        if (_events.ContainsKey(type))
        {
            _events[type] += action;
        }
        else
        {
            _events.Add(type, action);
        }
    }
    internal static void TrigEvent(int type, string content)
    {
        if (_events.TryGetValue(type, out var e))
        {
            e.Invoke(content);
        }
        else
        {
            Utils.Debug.LogWarning($"Room manager event {type} has no register");
        }
    }
    public static void TrigRpcEvent(Delivery delivery,int header,string content)
    {
        if (EnsInstance.Corr != null && EnsInstance.Corr.Client != null)
        {
            EnsInstance.Corr.Client.Send(delivery, new MessageBody_M_CS { type = header, content = content }, Pkt.csM);
        }
        else
        {
            Utils.Debug.LogError("当前状态不能发送消息");
        }
    }
}
