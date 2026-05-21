namespace Ens.Request
{
    public abstract class RequestClient
    {
        protected internal abstract string Header { get; }
        protected void SendRequest(string content)
        {
            EnsClientRequest.SendRequest(Header, content);
        }
        internal void RecvReply(string data)
        {
            if (Error(data, out int errorCode,out var content))
            {
                Error(errorCode,content);
            }
            else
            {
                HandleReply(data);
            }
        }
        protected abstract void Error(int code,string data);
        protected abstract void HandleReply(string data);
        protected internal abstract void TimeOut();
        protected static bool Error(string data, out int errorCode,out string content)
        {
            if (data.StartsWith("error"))
            {
                var s=data.Substring(5, data.Length - 5);
                if (s.Contains('#'))
                {
                    var ss = s.Split('#');
                    errorCode = int.Parse(ss[0]);
                    content = ss[1];
                }
                else
                {
                    errorCode = int.Parse(s);
                    content = string.Empty;
                }
                return true;
            }
            errorCode = -1;
            content = string.Empty;
            return false;
        }
    }
}