#if UNITY_WEBGL
using System;
using Utils;
using WeChatWASM;

namespace ProtocolWrapper.Protocols.WeChatTcp
{
    /// <summary>
    /// 微信小程序 TCP 客户端
    /// </summary>
    internal class ClientWeChatTcp : ProtocolBase
    {
        protected WXTCPSocket WeChatSocket;

        /// <summary>
        /// 初始化客户端
        /// </summary>
        /// <param name="ip">服务器 IP</param>
        /// <param name="port">服务器端口</param>
        public new void Init(string ip, int port)
        {
            // 创建微信 TCPSocket 实例（基础库3.1.1+支持）
            WeChatSocket = WXBase.CreateTCPSocket();

            base.Init(ip, port);

            // 注册事件监听
            RegisterSocketEvents();

            // 连接服务器
            try
            {
                WeChatSocket.Connect(new TCPSocketConnectOption
                {
                    address = IP,
                    port = Port
                });
                Initialized = true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WeChatTcp] 连接服务器失败：{ex.Message}");
                Initialized = false;
                return;
            }

            // 触发初始化完成事件
            Protocol.OnClientInitialized?.Invoke();
        }

        /// <summary>
        /// 注册Socket各类事件监听
        /// </summary>
        private void RegisterSocketEvents()
        {
            // 错误事件
            WeChatSocket.OnError(r =>
            {
                Debug.LogError($"[WeChatTcp] 错误：信息={r.errMsg}");
            });

            // 连接成功事件
            WeChatSocket.OnConnect(_ =>
            {
                Debug.Log("[WeChatTcp] Socket 连接成功");
            });

            // 关闭事件
            WeChatSocket.OnClose(_ =>
            {
                Debug.Log("[WeChatTcp] Socket 已关闭");
            });

            // 接收消息事件
            WeChatSocket.OnMessage((res) =>
            {
                if (Cancelled)
                {
                    Debug.LogError("[WeChatTcp] 已被取消，停止接收");
                    return;
                }

                try
                {
                    byte[] receivedData = res.message;
                    ReceiveBuffer.Write(receivedData);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[WeChatTcp] 接收数据处理失败：{ex.Message}");
                }
            });

            // 绑定WiFi成功事件（仅安卓支持）
            WeChatSocket.OnBindWifi(_ =>
            {
                Debug.Log("[WeChatTcp] Socket 绑定WiFi成功");
            });
        }


        /// <summary>
        /// 发送数据（适配微信 write 接口）
        /// </summary>
        /// <param name="bytes">待发送字节数组</param>
        /// <param name="length">发送长度</param>
        public override void Send(byte[] bytes, int length)
        {
            if (!Initialized)
            {
                Debug.LogError("[WeChatTcp] 未完成初始化");
                return;
            }
            if (Cancelled)
            {
                Debug.LogError("[WeChatTcp] 已被取消");
                return;
            }
            if (length <= 0 || bytes == null || bytes.Length < length)
            {
                Debug.LogError("[WeChatTcp] 发送参数错误：长度不合法");
                return;
            }

            try
            {
                // 截取有效数据
                byte[] sendData = new byte[length];
                Array.Copy(bytes, 0, sendData, 0, length);

                // 调用TCP的write方法发送数据
                WeChatSocket.Write(sendData);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WeChatTcp] 发送失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 关闭 Socket 连接
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