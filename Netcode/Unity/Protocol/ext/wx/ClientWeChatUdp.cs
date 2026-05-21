#if UNITY_WEBGL
using System;
using System.Net;
using System.Security.Cryptography;
using Utils;
using WeChatWASM;

namespace ProtocolWrapper.Protocols.WeChatUdp
{
    /// <summary>
    /// 微信小程序 UDP 客户端
    /// </summary>
    internal class ClientWeChatUdp : ProtocolBase
    {
        //protected WeChatWASM.WX wx;
        protected WXUDPSocket WeChatSocket;

        /// <summary>
        /// 初始化客户端
        /// </summary>
        /// <param name="ip">服务器 IP</param>
        /// <param name="port">服务器端口</param>
        /// <param name="socketType">套接字族（udp4/udp6）</param>
        public new void Init(string ip, int port)
        {
            // 创建微信 UDPSocket 实例（需根据实际桥接方式调用 wx.createUDPSocket）
            WeChatSocket = WXBase.CreateUDPSocket();
            WeChatSocket.Bind();

            base.Init(ip, port);

            // 错误事件
            WeChatSocket.OnError(r =>
            {
                Debug.LogError($"[WeChatUdp] 错误： 信息={r.errMsg}");
            });

            // 关闭事件
            WeChatSocket.OnClose(_ =>
            {
                Debug.Log("[WeChatUdp] Socket 已关闭");
            });
            Initialized = true;

            // 触发初始化完成事件
            Protocol.OnClientInitialized?.Invoke();

            // 监听消息事件
            WeChatSocket.OnMessage((res) =>
            {
                if (Cancelled)
                {
                    Debug.LogError("[WeChatUdp] 已被取消，停止接收");
                    return;
                }

                try
                {
                    byte[] receivedData = res.message;
                    ReceiveBuffer.Write(receivedData);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[WeChatUdp] 接收数据处理失败：{ex.Message}");
                }
            });
        }

        /// <summary>
        /// 发送数据（适配微信 send 接口）
        /// </summary>
        /// <param name="bytes">待发送字节数组</param>
        /// <param name="length">起始索引</param>
        public override void Send(byte[] bytes, int length)
        {
            if (!Initialized)
            {
                Debug.LogError("[WeChatUdp] 未完成初始化");
                return;
            }
            if (Cancelled)
            {
                Debug.LogError("[WeChatUdp] 已被取消");
                return;
            }

            try
            {
                // 截取有效数据（从 indexStart 到末尾）
                byte[] sendData = new byte[length];
                Array.Copy(bytes, 0, sendData, 0, length);

                WeChatSocket.Send(new UDPSocketSendOption
                {
                    address = IP,
                    port = Port,
                    message = sendData
                });
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WeChatUdp] 发送失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 关闭 Socket
        /// </summary>
        public override void ShutDown()
        {
            Cancelled = true;
            WeChatSocket?.Close();
            base.ShutDown();
        }

        protected override void ReleaseUnmanagedMenory()
        {
            WeChatSocket = null;
            base.ReleaseUnmanagedMenory();
        }

        protected override void ReleaseManagedMenory()
        {
            base.ReleaseManagedMenory();
        }
    }
}
#endif