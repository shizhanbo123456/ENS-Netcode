using System.Net;
using System.Threading;

public class DedicateServerProgram
{
    public EnsServer server;

    public float DisconnectThreshold = 3f;
    public float HeartbeatMsgInterval = 0.2f;
    public ProtocolWrapper.ProtocolType ProtocolType = ProtocolWrapper.ProtocolType.UDP;
    public bool PrintRoomData = true;

    //为空则视为IPAddress.Any
    public string IP;
    public int port = 44433;

    public void Start()
    {
        EnsInstance.DisconnectThreshold = DisconnectThreshold;
        EnsInstance.HeartbeatMsgInterval = HeartbeatMsgInterval;
        ProtocolWrapper.Protocol.defaultProtocol = ProtocolType;
        IPAddress ip = string.IsNullOrEmpty(IP) ? IPAddress.Any : IPAddress.Parse(IP);
        EnsRoomManager.PrintRoomData = PrintRoomData;

        EnsServerEventRegister.RegistDedicateServer();

        global::Loop.InitServer();
        global::Loop.InitCommon();

        server=new EnsServer(ip,port);
        server.StartListening();
    }
    public void Loop()
    {
        global::Loop.LoopCommon();
        global::Loop.LoopServer();

        server.Update();
        server.FlushSendBuffer();

        Thread.Sleep(1);
    }
}