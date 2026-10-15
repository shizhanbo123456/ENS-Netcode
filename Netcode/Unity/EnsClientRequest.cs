using Ens.ProtoFormat;
using Ens.Request;
using System;
using System.Collections.Generic;
using Utils;

public static class EnsClientRequest
{
    private static Dictionary<string, RequestClient> Requests=new Dictionary<string, RequestClient>();

    private static Dictionary<string,float>ActiveRequestHeader = new Dictionary<string, float>();

    public static void RegistRequest(RequestClient request)
    {
        string key = request.Header;
        if (Requests.ContainsKey(key))
        {
            Debug.LogError("ÖØ¸´×¢²áÊÂ¼þ");
        }
        else
        {
            Requests.Add(key, request);
        }
    }
    internal static bool SendRequest(string header,string content)
    {
        if (ActiveRequestHeader.ContainsKey(header)) return false;
        if (EnsInstance.Corr == null) return false;
        if (EnsInstance.Corr.Client == null) return false;
        EnsInstance.Corr.Client.Send(Delivery.Reliable, new MessageBody_Q_CS { header = header, content = content }, Pkt.csQ);
        ActiveRequestHeader.Add(header, Time.time + EnsInstance.ReliableKeyExistTime + 1);
        return true;
    }
    internal static void RecvReply(string header,string content)
    {
        ActiveRequestHeader.Remove(header);
        Requests[header].RecvReply(content);
    }
    internal static void Update()
    {
        List<string>timeExceedKeys=new List<string>();
        foreach(var pair in ActiveRequestHeader)
        {
            if (Time.time>pair.Value)
            {
                timeExceedKeys.Add(pair.Key);
            }
        }
        foreach(var i in timeExceedKeys)
        {
            ActiveRequestHeader.Remove(i);
            Requests[i].TimeOut();
        }
    }
}