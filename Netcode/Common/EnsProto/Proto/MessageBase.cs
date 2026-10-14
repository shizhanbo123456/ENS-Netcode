using System;

namespace Ens.ProtoFormat
{

    //(byte messageHeader)(ushort delivery){body}
    public struct MessageBase
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
    public struct MessageBaseSerializer
    {
        public static bool Serialize(MessageBase value, byte[] result, ref int indexStart)
        {
            if (result.Length - indexStart < 1 + 2) return false;
            ByteSerializer.Serialize(value.messageHeader, result, ref indexStart);
            UshortSerializer.Serialize(value.delivery, result, ref indexStart);
            return true;
        }

        public static MessageBase Deserialize(byte[] data, ref int indexStart, int invalidIndex)
        {
            if (data.Length - indexStart < 3) throw new Exception("反序列化失败：剩余数据字节数不足");
            byte messageHeader = ByteSerializer.Deserialize(data, ref indexStart, invalidIndex);
            ushort delivery = UshortSerializer.Deserialize(data, ref indexStart, invalidIndex);
            if (indexStart > invalidIndex) throw new Exception("下标越界");
            return new MessageBase { messageHeader = messageHeader, delivery = delivery };
        }
    }
}