using System.Net;
using UnityEngine;

namespace Ens.DefaultProtocol
{
    public abstract class ProtocolBase : MonoBehaviour
    {
        public abstract ProtocolWrapper.ProtocolBase GetProtocolBase(string ip, int port);
        public abstract ProtocolWrapper.ListenerBase GetListener(IPAddress ip, int port);
    }
}