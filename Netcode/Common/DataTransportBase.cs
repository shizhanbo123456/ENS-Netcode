using Ens.ProtoFormat;
using ProtocolWrapper;
using System;
using System.Collections.Generic;
using Utils;

public abstract class DataTransportBase//具有信息收发功能
{
    internal float hbRecvTime = Time.time+EnsInstance.DisconnectThreshold;//new ReachTime(EnsInstance.DisconnectThreshold, ReachTime.InitTimeFlagType.ReachAfter);
    internal float hbSendTime = Time.time + EnsInstance.HeartbeatMsgInterval;//new ReachTime(EnsInstance.HeartbeatMsgInterval, ReachTime.InitTimeFlagType.ReachAfter);
    internal DeliverySource DeliverySource = DeliverySource.Get();

    /// <summary>
    /// 需要重传的传输语义会把已发出去的字节存到这里；不需要重传的一端（Host/本地客户端）为 null
    /// </summary>
    internal KeyLibrary KeyLibrary;

    /// <summary>
    /// 本端用于发送的缓冲区；不能发送时返回 null
    /// </summary>
    internal virtual SendBuffer GetSendBuffer() { return null; }

    internal void Send<TBody>(Delivery delivery, TBody body, ProtoSender<TBody> proto) where TBody : struct
    {
        SendBuffer buffer = GetSendBuffer();
        if (buffer == null || DeliverySource == null) return;
        SendFrame(buffer, DeliverySource.DeliveryToId(delivery), body, proto, KeyLibrary, delivery);
    }
    internal abstract void Update();
    internal abstract void FlushSendBuffer();
    internal virtual void ShutDown()
    {
        if (DeliverySource != null)
        {
            DeliverySource.Return(DeliverySource);
            DeliverySource = null;
        }
    }
    internal abstract ProtocolBase GetProtocolBase();


    /// <summary>
    /// 写一个完整包：[消息头][delivery] 由结构体自带，这里补长度尾和分隔符<br></br>
    /// key 不为空且需要重传时，把刚写好的字节拷贝一份给 KeyLibrary
    /// </summary>
    internal static void SendFrame<TBody>(SendBuffer SendBuffer, ushort deliveryId, TBody body, ProtoSender<TBody> proto, KeyLibrary key = null, Delivery deliveryKind = Delivery.Unreliable) where TBody : struct
    {
        body = proto.SetDelivery(body, deliveryId);
        int bodyLength = proto.GetLength(body);
        if (bodyLength > 1400)
        {
            Debug.LogError("写入了超长数据");
            return;
        }
        SendBuffer.RequireLength(bodyLength+20);//预留更多空间，方便填充分隔符和消息尾
        int bytesStart = SendBuffer.indexStart;
        if (!proto.Write(body, SendBuffer.bytes, ref SendBuffer.indexStart))
        {
            SendBuffer.indexStart = bytesStart;
            Debug.LogError("写入失败：缓冲区空间不足");
            return;
        }
        int length = SendBuffer.indexStart - bytesStart;
#if UNITY_EDITOR
        if (length != bodyLength)
        {
            Debug.LogError($"长度计算错误,理论{bodyLength}，实际{length}");
        }
#endif
        ProtocolBase.LengthToBytes(length, out var a1, out var a2);
        SendBuffer.bytes[SendBuffer.indexStart++] = a1;
        SendBuffer.bytes[SendBuffer.indexStart++] = a2;
        SendBuffer.AddSeparator();
        if (key != null && deliveryKind != Delivery.Unreliable)
        {
            key.Track(deliveryKind, deliveryId, SendBuffer.bytes[bytesStart], SendBuffer.bytes, bytesStart, length);
        }
    }

    /// <summary>
    /// 写无 body 的包（如校验应答帧）
    /// </summary>
    internal static void SendFrame(SendBuffer SendBuffer, byte messageType, ushort delivery)
    {
        SendBuffer.RequireLength(3+20);
        int bytesStart = SendBuffer.indexStart;
        ByteSerializer.Serialize(messageType, SendBuffer.bytes, ref SendBuffer.indexStart);
        UshortSerializer.Serialize(delivery, SendBuffer.bytes, ref SendBuffer.indexStart);
        int length = SendBuffer.indexStart - bytesStart;
        ProtocolBase.LengthToBytes(length, out var a1, out var a2);
        SendBuffer.bytes[SendBuffer.indexStart++] = a1;
        SendBuffer.bytes[SendBuffer.indexStart++] = a2;
        SendBuffer.AddSeparator();
    }

    /// <summary>
    /// 直接发送已经序列化好的包字节（KeyLibrary 重传用）
    /// </summary>
    internal static void Send(SendBuffer SendBuffer, byte[] body, int length)
    {
        SendBuffer.RequireLength(length+20);
        Buffer.BlockCopy(body, 0, SendBuffer.bytes, SendBuffer.indexStart, length);
        SendBuffer.indexStart += length;
        ProtocolBase.LengthToBytes(length, out var a1, out var a2);
        SendBuffer.bytes[SendBuffer.indexStart++] = a1;
        SendBuffer.bytes[SendBuffer.indexStart++] = a2;
        SendBuffer.AddSeparator();
    }



    #region 分离出有效消息 -> ExtractData(byte[] data, List<Segment> parts)，传出的数据为消息头+消息体，不包含消息尾的长度

    protected static List<Segment> segments = new List<Segment>();

    protected static List<Segment> t_segments = new List<Segment>();
    public static void ExtractData(byte[] data,bool raw=false)
    {
        segments.Clear();
        t_segments.Clear();
        if (data == null || data.Length == 0) return;

        SplitDataBySeparator(data);
        if(raw)
        {
            foreach(var i in t_segments)segments.Add(i);
            return;
        }
        if (t_segments.Count == 0) return;

        int currentSegIndex = t_segments.Count - 1;
        while (currentSegIndex >= 0)
        {
            ProcessCandidateSegment(data, ref currentSegIndex);
        }
        t_segments.Reverse();
    }

    /// <summary>
    /// 按分隔符拆分，连续分隔符视为单一边界 | 修复：必处理最后一段数据
    /// 适配：消息首尾/消息间均有分隔符 → 拆分结果全为有效候选片段
    /// </summary>
    private static void SplitDataBySeparator(byte[] data)
    {
        int start = 0;
        byte separator = SendBuffer.Separator;
        while (data.Length > start && data[start] == separator) start++;
        int len = data.Length;

        for (int i = start; i < len; i++)
        {
            // 发现分隔符时，先统计连续分隔符的数量
            if (data[i] == separator)
            {
                int continuousSepCount = 1;
                // 向后遍历，统计连续的分隔符总数
                while (i + 1 < len && data[i + 1] == separator)
                {
                    continuousSepCount++;
                    i++;
                }

                // 核心条件：仅连续3个及以上分隔符，才视为有效分隔
                if (continuousSepCount >= 3)
                {
                    // 截取分隔符前的有效数据段（起始位置 到 分隔符开始前）
                    if (i - continuousSepCount + 1 > start)
                    {
                        t_segments.Add(new Segment(start, (i - continuousSepCount + 1) - start));
                    }
                    // 更新起始位置，跳过所有连续的有效分隔符
                    start = i + 1;
                }
            }
        }

        // 格式决定了无需处理最后一段，每个片段都被分隔符包裹了
    }
    private static void ProcessCandidateSegment(byte[] data,ref int currentSegIndex)
    {
        int checkStartIdx = t_segments[currentSegIndex].StartIndex;
        int checkTotalLength = t_segments[currentSegIndex].Length;
        int mergeSegIndex = currentSegIndex - 1;

        while (true)
        {
            // 最小长度阈值
            if (checkTotalLength < 3) { currentSegIndex = mergeSegIndex; return; }

            // 提取末尾2字节Length段（已编码，无分隔符，无需校验）
            int lenRightIdx = checkStartIdx + checkTotalLength - 1;
            int lenLeftIdx = lenRightIdx - 1;
            ProtocolBase.BytesToLength( data[lenLeftIdx],data[lenRightIdx],out int msgDeclaredLength);

            //核心长度校验逻辑
            if (checkTotalLength == msgDeclaredLength + 2)
            {
                segments.Add(new Segment(checkStartIdx, msgDeclaredLength));
                currentSegIndex = mergeSegIndex;
                return;
            }
            else if (checkTotalLength < msgDeclaredLength + 2)
            {
                // 长度不足 → 继续向前合并（被分隔符拆分）
                if (mergeSegIndex < 0) { currentSegIndex = mergeSegIndex; return; }
                checkStartIdx = t_segments[mergeSegIndex].StartIndex;
                checkTotalLength += t_segments[mergeSegIndex].Length;
                mergeSegIndex--;
            }
            else
            {
                // 长度超限 → 传输错误，放弃解析
                currentSegIndex = mergeSegIndex;
                return;
            }
        }
    }
    #endregion
}
public readonly struct Segment
{
    public int StartIndex { get; }
    public int Length { get; }

    public Segment(int startIndex, int length)
    {
        StartIndex = startIndex;
        Length = length;
    }
    public override string ToString()
    {
        return StartIndex + " " + Length;
    }
}
public struct MessageReader
{
    public const int BodyOffset = 3;
    public static byte Header(byte[] data, Segment s) => data[s.StartIndex];
    public static ushort Delivery(byte[] data, Segment s)
    {
        int indexStart = s.StartIndex + 1;
        return UshortSerializer.Deserialize(data, ref indexStart, s.StartIndex + 3);
    }
    public static int BodyIndexStart(Segment s) => s.StartIndex+3;
    public static int BodyIndexInvalid(Segment s) => s.StartIndex+s.Length;
}