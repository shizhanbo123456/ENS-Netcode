using System;

namespace Ens.ProtoFormat
{
    //D 服务器接收 断开连接(无消息体)
    public struct MessageBody_D_CS
    {
    }
    public struct MessageBody_D_CSSerializer
    {
        public static bool Serialize(MessageBody_D_CS value, byte[] result, ref int indexStart)
        {
            return true;
        }

        public static MessageBody_D_CS Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            return new MessageBody_D_CS();
        }
    }
}
