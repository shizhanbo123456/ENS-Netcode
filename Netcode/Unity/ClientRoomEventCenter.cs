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
    public static void TrigEvent(Delivery delivery, int header, string content)
    {
        if (EnsInstance.Corr != null && EnsInstance.Corr.Client != null)
        {
            Writer.instance.t_type = header;
            Writer.instance.t_content = content;
            EnsInstance.Corr.Client.Send(Header.M, delivery, Writer.instance);
        }
        else
        {
            Utils.Debug.LogError("当前状态不能发送消息");
        }
    }
    internal class Writer : MessageWriter
    {
        internal static Writer instance = new();
        internal int t_type;
        internal string t_content;

        public int GetLength()
        {
            return sizeof(int)+StringSerializer.GetLength(t_content);
        }
        public bool Write(SendBuffer b)
        {
            return IntSerializer.Serialize(t_type, b.bytes, ref b.indexStart) &&
                StringSerializer.Serialize(t_content, b.bytes, ref b.indexStart);
        }
        public MessageWriter Clone()
        {
            return new Writer() { t_type = t_type, t_content = t_content };
        }
        public void Dispose()
        {

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
    protected static void TrigEvent(Delivery delivery,int header,string content)
    {
        if (EnsInstance.Corr != null && EnsInstance.Corr.Client != null)
        {
            Writer.instance.t_type = header;
            Writer.instance.t_content = content;
            EnsInstance.Corr.Client.Send(Header.M, delivery, Writer.instance);
        }
        else
        {
            Utils.Debug.LogError("当前状态不能发送消息");
        }
    }
    private class Writer : MessageWriter
    {
        internal static Writer instance = new();
        internal int t_type;
        internal string t_content;

        public int GetLength()
        {
            return sizeof(int) + StringSerializer.GetLength(t_content);
        }
        public bool Write(SendBuffer b)
        {
            return IntSerializer.Serialize(t_type, b.bytes, ref b.indexStart)&&
                StringSerializer.Serialize(t_content,b.bytes,ref b.indexStart);
        }
        public MessageWriter Clone()
        {
            return new Writer() { t_type = t_type,t_content=t_content };
        }
        public void Dispose()
        {

        }
    }
}