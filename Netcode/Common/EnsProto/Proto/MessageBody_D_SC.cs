using System;

namespace ProtoFormat
{
    //D 客户端接收 断开连接(无消息体)
    public struct MessageBody_D_SC
    {
    }
    public struct MessageBody_D_SCSerializer
    {
        public static bool Serialize(MessageBody_D_SC value, byte[] result, ref int indexStart)
        {
            return true;
        }

        public static MessageBody_D_SC Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            return new MessageBody_D_SC();
        }
    }
}
