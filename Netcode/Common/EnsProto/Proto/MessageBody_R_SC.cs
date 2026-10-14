using System;

namespace ProtoFormat
{
    //R 客户端接收 离开房间重置(无消息体)
    public struct MessageBody_R_SC
    {
    }
    public struct MessageBody_R_SCSerializer
    {
        public static bool Serialize(MessageBody_R_SC value, byte[] result, ref int indexStart)
        {
            return true;
        }

        public static MessageBody_R_SC Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            return new MessageBody_R_SC();
        }
    }
}
