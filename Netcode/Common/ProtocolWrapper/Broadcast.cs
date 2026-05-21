using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Net.NetworkInformation; // 仅新增：获取子网广播必须的命名空间
using Utils;
namespace ProtocolWrapper
{
    // 存储接收消息的结构体，包含内容和接收时间
    public struct ReceivedMessage
    {
        public string Content;
        public float ReceiveTime;
    }
    public static class Broadcast
    {
        public static int Port = 9900;
        public static float broadcastInterval = 1f;
        public static float CleanupExpiredMessagesInterval = 0.5f;
        public static float messageTimeout = 5f;
        private static ReachTime reachTime = new ReachTime(-1, ReachTime.InitTimeFlagType.ReachAt);
        private static ReachTime CleanExpiredTime = new ReachTime(-1, ReachTime.InitTimeFlagType.ReachAt);
        private static Dictionary<string, string> BroadcastContent = new Dictionary<string, string>();
        private static Dictionary<string, List<ReceivedMessage>> ReceiveContent = new Dictionary<string, List<ReceivedMessage>>();
        //用原生 Socket
        private static Socket senderSocket;
        private static Socket receiverSocket;
        private static EndPoint broadcastEndPoint;
        public static bool Sending => senderSocket != null;
        public static bool Receiving => receiverSocket != null;
        private static byte[] bytes = new byte[1400];
        private static int length = 0;

        public static bool StartBroadcast()
        {
            if (senderSocket != null)
            {
                return true;
            }
            try
            {
                senderSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, System.Net.Sockets.ProtocolType.Udp);
                senderSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);
                string subnetBroadcastIp = GetSubnetBroadcastAddress();
                UnityEngine.Debug.Log(subnetBroadcastIp);
                if (!string.IsNullOrEmpty(subnetBroadcastIp))
                {
                    broadcastEndPoint = new IPEndPoint(IPAddress.Parse(subnetBroadcastIp), Port);
                }
                else
                {
                    // 保留原有fallback
                    broadcastEndPoint = new IPEndPoint(IPAddress.Broadcast, Port);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string GetSubnetBroadcastAddress()
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            try
            {
                foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    // 只取 已连接 + 无线Wi‑Fi网卡（手机热点必走这个）
                    if (ni.OperationalStatus != OperationalStatus.Up)
                        continue;

                    // 只留无线网卡，彻底排除虚拟机/虚拟网卡
                    if (ni.NetworkInterfaceType != NetworkInterfaceType.Wireless80211)
                        continue;

                    foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily != AddressFamily.InterNetwork)
                            continue;
                        if (IPAddress.IsLoopback(ip.Address))
                            continue;

                        byte[] ipBytes = ip.Address.GetAddressBytes();
                        byte[] maskBytes = ip.IPv4Mask.GetAddressBytes();
                        byte[] broadcastBytes = new byte[4];
                        for (int i = 0; i < 4; i++)
                        {
                            broadcastBytes[i] = (byte)((ipBytes[i] & maskBytes[i]) | (~maskBytes[i]));
                        }
                        return new IPAddress(broadcastBytes).ToString();
                    }
                }
            }
            catch { }
#elif UNITY_ANDROID
            try
            {
                foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus != OperationalStatus.Up) continue;
                    if (ni.NetworkInterfaceType != NetworkInterfaceType.Wireless80211 &&
                        ni.NetworkInterfaceType != NetworkInterfaceType.Ethernet) continue;

                    foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily != AddressFamily.InterNetwork) continue;
                        if (IPAddress.IsLoopback(ip.Address)) continue;

                        byte[] ipBytes = ip.Address.GetAddressBytes();
                        byte[] maskBytes = ip.IPv4Mask.GetAddressBytes();
                        byte[] broadcastBytes = new byte[4];
                        for (int i = 0; i < 4; i++)
                        {
                            broadcastBytes[i] = (byte)((ipBytes[i] & maskBytes[i]) | (~maskBytes[i]));
                        }
                        return new IPAddress(broadcastBytes).ToString();
                    }
                }
            }
            catch { }
#endif
            return "255.255.255.255";
        }

        private static void BroadcastUpdate()
        {
            if (!reachTime.Reached) return;
            reachTime.ReachAfter(broadcastInterval);
            if (BroadcastContent.Count == 0) return;
            var s = Format.DictionarySeparator + Format.DictionaryToString(BroadcastContent, wrapAll: false) + Format.DictionarySeparator;
            try
            {
                length = 0;
                StringSerializer.Serialize(s, bytes, ref length);
                senderSocket.SendTo(bytes, 0, length, SocketFlags.None, broadcastEndPoint);
            }
            catch
            {
                EndBroadcast();
                Debug.LogError("广播发送失败，已自动关闭广播");
            }
        }
        public static void EndBroadcast()
        {
            if (senderSocket != null)
            {
                senderSocket.Close();
                senderSocket = null;
                broadcastEndPoint = null;
            }
        }
        public static void AddInfo(string header, string content)
        {
            if (BroadcastContent.ContainsKey(header))
                BroadcastContent[header] = content;
            else
                BroadcastContent.Add(header, content);
        }
        public static void RemoveInfo(string header)
        {
            if (BroadcastContent.ContainsKey(header))
                BroadcastContent.Remove(header);
        }
        public static void ClearSendContent()
        {
            BroadcastContent.Clear();
        }
        public static bool StartRecv()
        {
            if (receiverSocket != null)
            {
                Debug.LogWarning("接收已经开启");
                return true;
            }
            try
            {
                receiverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, System.Net.Sockets.ProtocolType.Udp);
                IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, Port);
                receiverSocket.Bind(localEndPoint);
                receiverSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 1);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError(e.ToString());
                return false;
            }
        }
        private static void RecvUpdate()
        {
            try
            {
                while (receiverSocket != null && receiverSocket.Available > 0)
                {
                    EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                    // 注意：为了安全，这里创建一个新的临时 buffer 接收，防止脏数据
                    byte[] tempBuffer = new byte[1400];
                    int receivedLen = receiverSocket.ReceiveFrom(tempBuffer, ref remoteEP);
                    Buffer.BlockCopy(tempBuffer, 0, bytes, 0, receivedLen);
                    int indexstart = 0;
                    string message = StringSerializer.Deserialize(bytes, ref indexstart, receivedLen);
                    int msgstart = message.IndexOf(Format.DictionarySeparator);
                    int msgend = message.LastIndexOf(Format.DictionarySeparator);
                    if (msgstart == -1 || msgend == -1 || msgend <= msgstart) continue;
                    string msg = message.Substring(msgstart, msgend - msgstart + 1);
                    var s = Format.SplitWithBoundaries(msg, Format.DictionarySeparator, removeBoundary: false);
                    foreach (var part in s)
                    {
                        HandleReceivedMesg(part);
                    }
                }
            }
            catch
            {
                // 忽略超时异常，只在严重错误时关闭
                // EndRecv(); 
                // Debug.LogError("接收广播失败，已自动关闭广播");
            }
        }
        public static void EndRecv()
        {
            if (receiverSocket != null)
            {
                receiverSocket.Close();
                receiverSocket = null;
            }
        }
        private static void HandleReceivedMesg(string mesg)
        {
            var keyValue = Format.SplitWithBoundaries(mesg, Format.DictionaryPair);
            if (keyValue.Count != 2) return;
            string header = keyValue[0];
            string content = keyValue[1];
            var receivedMsg = new ReceivedMessage
            {
                Content = content,
                ReceiveTime = Time.time
            };
            if (ReceiveContent.ContainsKey(header))
            {
                bool exists = false;
                for (int i = 0; i < ReceiveContent[header].Count; i++)
                {
                    ReceivedMessage item = ReceiveContent[header][i];
                    if (item.Content == content)
                    {
                        exists = true;
                        ReceiveContent[header][i] = receivedMsg;
                        break;
                    }
                }
                if (!exists)
                {
                    ReceiveContent[header].Add(receivedMsg);
                }
            }
            else
            {
                ReceiveContent[header] = new List<ReceivedMessage> { receivedMsg };
            }
        }
        private static void CleanupExpiredMessages()
        {
            float currentTime = Time.time;
            List<string> headersToRemove = new List<string>();
            foreach (var kvp in ReceiveContent)
            {
                kvp.Value.RemoveAll(msg => currentTime - msg.ReceiveTime > messageTimeout);
                if (kvp.Value.Count == 0)
                {
                    headersToRemove.Add(kvp.Key);
                }
            }
            foreach (var header in headersToRemove)
            {
                ReceiveContent.Remove(header);
            }
        }
        public static bool TryGetContents(string header, out List<ReceivedMessage> contents)
        {
            return ReceiveContent.TryGetValue(header, out contents);
        }
        public static void ClearRecvContent()
        {
            ReceiveContent.Clear();
        }
        public static void Update()
        {
            if (senderSocket != null)
                BroadcastUpdate();
            if (receiverSocket != null)
                RecvUpdate();
            if (CleanExpiredTime.Reached)
            {
                CleanExpiredTime.ReachAfter(CleanupExpiredMessagesInterval);
                CleanupExpiredMessages();
            }
        }
    }
}