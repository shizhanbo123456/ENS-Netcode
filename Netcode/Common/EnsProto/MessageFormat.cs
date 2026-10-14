using System;

/*
 * SC表示server->client，CS表示client->server
 * 消息格式中：()=固定内容，[]=可选内容，{}=可变内容
 */


namespace Ens.Format
{
    //repeated: MessageFormatBase(2byte messageLength)0xFE 0xFE
    public static class MessagePointer
    {
        public static byte[] message;
        public static Segment segment;
    }
    //(byte messageHeader)(ushort delivery){body}
    public struct MessageFormatBase
    {
        public byte messageHeader;//body类型由messageHeader决定
        public ushort delivery;
        public static Type[] possibleFollowing = new[]
        {
            typeof(MessageBody_E_SC),

            typeof(MessageBody_C_SC),

            typeof(MessageBody_A_CS),
            typeof(MessageBody_A_SC),

            typeof(MessageBody_F_CS),
            typeof(MessageBody_F_SC),

            typeof(MessageBody_f_CS),
            typeof(MessageBody_f_SC),

            typeof(MessageBody_H_CS),
            typeof(MessageBody_H_SC),

            typeof(MessageBody_D_CS),
            typeof(MessageBody_D_SC),

            typeof(MessageBody_R_SC),

            typeof(MessageBody_Q_CS),
            typeof(MessageBody_Q_SC),

            typeof(MessageBody_M_CS),
            typeof(MessageBody_M_SC),

            typeof(MessageBody_N_CS),
            typeof(MessageBody_N_SC),
        };
    }

    //E 客户端接收 事件
    public struct MessageBody_E_SC
    {
        public byte eventId;//1 OnClientConnect 2 OnClientDisconnect 3 OnServerConnect 4 OnServerDisconnect；3在C事件触发，4在ShutDown触发(超时/D事件/主动调用)
        public short? clientId;
    }

    //C 客户端接收 分配Id
    public struct MessageBody_C_SC
    {
        public int clientId;
    }

    //A 服务器接收 设置房主权限
    public struct MessageBody_A_CS
    {
        public int clientId;
    }
    //A 客户端接收
    public struct MessageBody_A_SC
    {
        public bool authorityActive;
    }

    //F 服务器接收 函数调用
    public struct MessageBody_F_CS
    {
        public short NOMObjectId;
        public byte func;
        public string param;
        public short target;
    }
    //F 客户端接收 函数调用
    public struct MessageBody_F_SC
    {
        public short NOMObjectId;
        public byte func;
        public string param;
    }

    //f 服务器接收 创建物体
    public struct MessageBody_f_CS
    {
        public short sendto;
        public bool spawnMode;//false:用behaviourCount; true:用idStart
        public short collectionId;
        public string param;
        public short? behaviourCount;
        public short? idStart;
        public short target;
    }
    //f 客户端接收 创建物体
    public struct MessageBody_f_SC
    {
        public bool spawnMode;//false:创建; true:检测重建
        public short collectionId;
        public string param;
        public short idStart;
    }

    //H 服务器接收 心跳检测
    public struct MessageBody_H_CS
    {
        public int millisecondInServerMsg;
    }
    //H 客户端接收 心跳检测
    public struct MessageBody_H_SC
    {
        public int serverMillisecond;
        public int delay;
    }

    //D 服务器接收 断开连接(无消息体)
    public struct MessageBody_D_CS
    {
    }
    //D 客户端接收 断开连接(无消息体)
    public struct MessageBody_D_SC
    {
    }

    //R 客户端接收 离开房间重置(无消息体)
    public struct MessageBody_R_SC
    {
    }

    //Q 服务器接收
    public struct MessageBody_Q_CS
    {
        public string header;
        public string content;
    }
    //Q 客户端接收
    public struct MessageBody_Q_SC
    {
        public string header;
        public string content;
    }

    //M 服务器接收
    public struct MessageBody_M_CS
    {
        public int type;
        public string content;
    }
    //M 客户端接收
    public struct MessageBody_M_SC
    {
        public int type;
        public string content;
    }

    //N 服务器接收
    public struct MessageBody_N_CS
    {
        public int type;
        public string content;
    }
    //N 客户端接收
    public struct MessageBody_N_SC
    {
        public int type;
        public string content;
    }
}
