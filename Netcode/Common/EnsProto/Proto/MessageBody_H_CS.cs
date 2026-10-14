using System;

namespace Ens.ProtoFormat
{
    //H 服务器接收 心跳检测；空 body 为保活心跳，4 字节为心跳回应(回显服务器时间戳)
    public struct MessageBody_H_CS
    {
        public int? millisecondInServerMsg;
    }
    public struct MessageBody_H_CSSerializer
    {
        public static bool Serialize(MessageBody_H_CS value, byte[] result, ref int indexStart)
        {
            if (!value.millisecondInServerMsg.HasValue) return true;
            if (result.Length - indexStart < 4) return false;
            IntSerializer.Serialize(value.millisecondInServerMsg.Value, result, ref indexStart);
            return true;
        }

        public static MessageBody_H_CS Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            //与 Server_H 一致：不足 4 字节视为保活心跳，无时间戳
            if (invalidIndex - indexStart < 4) return new MessageBody_H_CS { millisecondInServerMsg = null };
            int millisecondInServerMsg = IntSerializer.Deserialize(data, ref indexStart, invalidIndex);
            if (indexStart > invalidIndex) throw new Exception("下标越界");
            return new MessageBody_H_CS { millisecondInServerMsg = millisecondInServerMsg };
        }
    }
}
