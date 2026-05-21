#if UNITY_2017_1_OR_NEWER
using Ens.Request.Client;
#endif
using System;
using System.Collections.Generic;

public class EnsInstance
{
#if UNITY_2017_1_OR_NEWER
    public static EnsCorrespondent Corr;
    public static EnsSpawner EnsSpawner;
#endif

    public static short LocalClientId = -1;
    public static int PresentRoomId = 0;
    public static bool HasAuthority = false;
    public static int LocalClientDelay=20;

    public static bool DevelopmentDebug;

    public static float DisconnectThreshold = 3f;// 上次接收心跳检测时间超过此阈值会认为断开了连接
    public static float HeartbeatMsgInterval = 0.2f;// 发送心跳检测消息的间隔

    public static float ReliableKeyExistTime = 3f;//关键信息发送生效时长
    public static float UnconfirmedKeySendInterval = 0.05f;//未确认的关键信息发送间隔
    public static float ReceiverKeyExistTime = 2f;//对方对关键信息的忽略时长
    public static float StriveKeySendInterval = 0.01f;
    public static int StriveKeyResendCount = 3;

    public static int frameRate;

#if UNITY_2017_1_OR_NEWER
    public static Action OnConnectionRejected;
    public static Action OnServerConnect;
    public static Action OnServerDisconnect;
    public static Action<short> OnClientEnter;//有新用户连接到服务器时触发(新用户自身不调用)
    public static Action<short> OnClientExit;//有用户与服务器断开时调用(断开的用户自身不调用)
    public static Action OnAuthorityChanged;

    public static Action OnCreateRoom { get => CreateRoom.OnCreateRoom; set=>CreateRoom.OnCreateRoom=value; }
    public static Action SetRuleSucceed { get => SetRule.OnRecvReply; set => SetRule.OnRecvReply = value; }
    public static Action OnJoinRoom { get =>JoinRoom.OnRecvReply; set =>JoinRoom.OnRecvReply = value; }
    public static Action<Dictionary<int,string>> OnGetRule { get => GetRule.OnRecvReply; set => GetRule.OnRecvReply = value; }
    public static Action OnExitRoom { get => ExitRoom.OnRecvReply; set => ExitRoom.OnRecvReply = value; }
    public static Action<int, List<int>> OnGetRoomList { get => GetRoomList.OnRecvReply; set => GetRoomList.OnRecvReply = value; }
    public static Action SetInfoSucceed { get => SetInfo.OnRecvReply; set => SetInfo.OnRecvReply = value; }
    public static Action<Dictionary<int, string>> OnGetInfo { get => GetInfo.OnRecvReply; set => GetInfo.OnRecvReply = value; }

    public static Action CreateRoomTimeout { get => CreateRoom.OnTimeOut; set => CreateRoom.OnTimeOut = value; }
    public static Action SetRuleTimeOut { get => SetRule.OnTimeOut; set => SetRule.OnTimeOut = value; }
    public static Action JoinRoomTimeOut { get => JoinRoom.OnTimeOut; set => JoinRoom.OnTimeOut = value; }
    public static Action GetRuleTimeOut { get => GetRule.OnTimeOut; set => GetRule.OnTimeOut = value; }
    public static Action ExitRoomTimeOut { get => ExitRoom.OnTimeOut; set => ExitRoom.OnTimeOut = value; }
    public static Action GetRoomListTimeOut { get => GetRoomList.OnTimeOut; set => GetRoomList.OnTimeOut = value; }
    public static Action SetInfoTimeOut { get => SetInfo.OnTimeOut; set => SetInfo.OnTimeOut = value; }
    public static Action GetInfoTimeOut { get => GetInfo.OnTimeOut; set => GetInfo.OnTimeOut = value; }

    public static Action AlredyInRoomOnCreate { get => CreateRoom.AlreadyInRoomException; set => CreateRoom.AlreadyInRoomException = value; }
    public static Action NotInRoomOnSetRule { get => SetRule.NotInRoomError; set => SetRule.NotInRoomError = value; }
    public static Action RoomNotFoundOnJoin { get => JoinRoom.OnRoomNotFoundError; set => JoinRoom.OnRoomNotFoundError = value; }
    public static Action AlreadyInRoomOnJoin { get => JoinRoom.AlreadyInRoomError; set => JoinRoom.AlreadyInRoomError = value; }
    public static Action RoomNotFoundOnGetRule { get => GetRule.RoomNotFoundError; set => GetRule.RoomNotFoundError = value; }
    public static Action RoomNotFoundOnExit { get => ExitRoom.NotInRoomException; set => ExitRoom.NotInRoomException = value; }
    public static Action NotInRoomOnSetInfo { get => SetInfo.NotInRoomError; set => SetInfo.NotInRoomError = value; }
    public static Action RoomNotFoundOnGetInfo { get => GetInfo.RoomNotFoundError; set => GetInfo.RoomNotFoundError = value; }

    public static void OnTimeOut(Action action)
    {
        CreateRoomTimeout += action;
        SetRuleTimeOut += action;
        JoinRoomTimeOut += action;
        GetRuleTimeOut += action;
        ExitRoomTimeOut += action;
        GetRoomListTimeOut += action;
        SetInfoTimeOut += action;
        GetInfoTimeOut += action;
    }
    public static void OnError(Action action)
    {
        AlredyInRoomOnCreate += action;
        NotInRoomOnSetRule += action;
        RoomNotFoundOnJoin += action;
        AlreadyInRoomOnJoin += action;
        RoomNotFoundOnGetRule += action;
        RoomNotFoundOnExit += action;
        NotInRoomOnSetInfo += action;
        RoomNotFoundOnGetInfo += action;
    }


    //确保断开连接事件只会触发一次(避免多次调用ShutDown)
    //用户不需要注意此参数
    internal static bool RoomExitInvoke = true;
    internal static bool ServerDisconnectInvoke = true;
    internal static bool ClientConnectRejected = false;
#endif
}
