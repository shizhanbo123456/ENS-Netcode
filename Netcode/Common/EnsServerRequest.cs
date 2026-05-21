using Ens.Request;
using System.Collections.Generic;

public static class EnsServerRequest
{
    private static Dictionary<string,RequestServer> Requests=new Dictionary<string,RequestServer>();
    public static void RegistRequest(RequestServer request)
    {
        if (!Requests.ContainsKey(request.Header)) Requests.Add(request.Header, request);
        else Utils.Debug.LogError("已经注册了事件"+ request.Header);
    }
    internal static string OnRecvRequest(string header,string content,EnsConnection conn)
    {
        if (Requests.ContainsKey(header))
        {
            return Requests[header].HandleRequest(conn, content);
        }
        else
        {
            Utils.Debug.LogError("未注册的请求头：" + header);
            return string.Empty;
        }
    }
}