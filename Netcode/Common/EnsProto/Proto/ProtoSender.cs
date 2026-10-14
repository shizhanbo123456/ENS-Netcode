using System;

namespace Ens.ProtoFormat
{
    //把 body 写进缓冲区，空间不足返回 false
    public delegate bool ProtoBodySerialize<TBody>(TBody value, byte[] result, ref int indexStart);
    //从缓冲区读出 body
    public delegate TBody ProtoBodyDeserialize<TBody>(byte[] data, ref int indexStart, int invalidIndex);
    //把本次分配的 delivery id 写进 body
    public delegate TBody ProtoBodySetDelivery<TBody>(TBody value, ushort delivery);

    /// <summary>
    /// 每个包类型一个实例，绑定该类型 Serializer 的静态方法（整个工程唯一的绑定点）
    /// </summary>
    internal sealed class ProtoSender<TBody>
    {
        internal readonly Func<TBody, int> GetLength;
        internal readonly ProtoBodySerialize<TBody> Write;
        internal readonly ProtoBodyDeserialize<TBody> Read;
        internal readonly ProtoBodySetDelivery<TBody> SetDelivery;

        internal ProtoSender(Func<TBody, int> getLength, ProtoBodySerialize<TBody> write,
            ProtoBodyDeserialize<TBody> read, ProtoBodySetDelivery<TBody> setDelivery)
        {
            GetLength = getLength;
            Write = write;
            Read = read;
            SetDelivery = setDelivery;
        }

        //从整包里解析 body：帧头由结构体自带，所以从段首读到段尾
        internal TBody FromSegment(byte[] data, Segment segment)
        {
            int index = segment.StartIndex;
            return Read(data, ref index, segment.StartIndex + segment.Length);
        }
    }

    /// <summary>
    /// 所有包的绑定表，增删协议只改这里
    /// </summary>
    internal static class Pkt
    {
        internal static readonly ProtoSender<MessageBody_A_CS> csA = new(
            MessageBody_A_CSSerializer.GetLength,
            MessageBody_A_CSSerializer.Serialize,
            MessageBody_A_CSSerializer.Deserialize,
            (v, d) => { v.delivery = d; return v; });

        internal static readonly ProtoSender<MessageBody_A_SC> scA = new(
            MessageBody_A_SCSerializer.GetLength,
            MessageBody_A_SCSerializer.Serialize,
            MessageBody_A_SCSerializer.Deserialize,
            (v, d) => { v.delivery = d; return v; });

        internal static readonly ProtoSender<MessageBody_C_SC> scC = new(
            MessageBody_C_SCSerializer.GetLength,
            MessageBody_C_SCSerializer.Serialize,
            MessageBody_C_SCSerializer.Deserialize,
            (v, d) => { v.delivery = d; return v; });

        internal static readonly ProtoSender<MessageBody_D_CS> csD = new(
            MessageBody_D_CSSerializer.GetLength,
            MessageBody_D_CSSerializer.Serialize,
            MessageBody_D_CSSerializer.Deserialize,
            (v, d) => { v.delivery = d; return v; });

        internal static readonly ProtoSender<MessageBody_D_SC> scD = new(
            MessageBody_D_SCSerializer.GetLength,
            MessageBody_D_SCSerializer.Serialize,
            MessageBody_D_SCSerializer.Deserialize,
            (v, d) => { v.delivery = d; return v; });

        internal static readonly ProtoSender<MessageBody_E_SC> scE = new(
            MessageBody_E_SCSerializer.GetLength,
            MessageBody_E_SCSerializer.Serialize,
            MessageBody_E_SCSerializer.Deserialize,
            (v, d) => { v.delivery = d; return v; });

        internal static readonly ProtoSender<MessageBody_F_CS> csF = new(
            MessageBody_F_CSSerializer.GetLength,
            MessageBody_F_CSSerializer.Serialize,
            MessageBody_F_CSSerializer.Deserialize,
            (v, d) => { v.delivery = d; return v; });

        internal static readonly ProtoSender<MessageBody_F_SC> scF = new(
            MessageBody_F_SCSerializer.GetLength,
            MessageBody_F_SCSerializer.Serialize,
            MessageBody_F_SCSerializer.Deserialize,
            (v, d) => { v.delivery = d; return v; });

        internal static readonly ProtoSender<MessageBody_f_CS> csf = new(
            MessageBody_f_CSSerializer.GetLength,
            MessageBody_f_CSSerializer.Serialize,
            MessageBody_f_CSSerializer.Deserialize,
            (v, d) => { v.delivery = d; return v; });

        internal static readonly ProtoSender<MessageBody_f_SC> scf = new(
            MessageBody_f_SCSerializer.GetLength,
            MessageBody_f_SCSerializer.Serialize,
            MessageBody_f_SCSerializer.Deserialize,
            (v, d) => { v.delivery = d; return v; });

        internal static readonly ProtoSender<MessageBody_H_CS> csH = new(
            MessageBody_H_CSSerializer.GetLength,
            MessageBody_H_CSSerializer.Serialize,
            MessageBody_H_CSSerializer.Deserialize,
            (v, d) => { v.delivery = d; return v; });

        internal static readonly ProtoSender<MessageBody_H_SC> scH = new(
            MessageBody_H_SCSerializer.GetLength,
            MessageBody_H_SCSerializer.Serialize,
            MessageBody_H_SCSerializer.Deserialize,
            (v, d) => { v.delivery = d; return v; });

        internal static readonly ProtoSender<MessageBody_M_CS> csM = new(
            MessageBody_M_CSSerializer.GetLength,
            MessageBody_M_CSSerializer.Serialize,
            MessageBody_M_CSSerializer.Deserialize,
            (v, d) => { v.delivery = d; return v; });

        internal static readonly ProtoSender<MessageBody_M_SC> scM = new(
            MessageBody_M_SCSerializer.GetLength,
            MessageBody_M_SCSerializer.Serialize,
            MessageBody_M_SCSerializer.Deserialize,
            (v, d) => { v.delivery = d; return v; });

        internal static readonly ProtoSender<MessageBody_N_CS> csN = new(
            MessageBody_N_CSSerializer.GetLength,
            MessageBody_N_CSSerializer.Serialize,
            MessageBody_N_CSSerializer.Deserialize,
            (v, d) => { v.delivery = d; return v; });

        internal static readonly ProtoSender<MessageBody_N_SC> scN = new(
            MessageBody_N_SCSerializer.GetLength,
            MessageBody_N_SCSerializer.Serialize,
            MessageBody_N_SCSerializer.Deserialize,
            (v, d) => { v.delivery = d; return v; });

        internal static readonly ProtoSender<MessageBody_Q_CS> csQ = new(
            MessageBody_Q_CSSerializer.GetLength,
            MessageBody_Q_CSSerializer.Serialize,
            MessageBody_Q_CSSerializer.Deserialize,
            (v, d) => { v.delivery = d; return v; });

        internal static readonly ProtoSender<MessageBody_Q_SC> scQ = new(
            MessageBody_Q_SCSerializer.GetLength,
            MessageBody_Q_SCSerializer.Serialize,
            MessageBody_Q_SCSerializer.Deserialize,
            (v, d) => { v.delivery = d; return v; });

        internal static readonly ProtoSender<MessageBody_R_SC> scR = new(
            MessageBody_R_SCSerializer.GetLength,
            MessageBody_R_SCSerializer.Serialize,
            MessageBody_R_SCSerializer.Deserialize,
            (v, d) => { v.delivery = d; return v; });
    }
}
