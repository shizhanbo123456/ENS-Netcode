namespace Ens.Request
{
    public abstract class RequestServer
    {
        protected internal abstract string Header { get; }
        protected internal abstract string HandleRequest(EnsConnection conn,string data);
        protected static string ThrowError(int code)
        {
            return "error" + code;
        }
        protected static string ThrowError(int code,string data)
        {
            return "error" + code+'#'+data;
        }
    }
}