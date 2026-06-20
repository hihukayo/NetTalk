using System.Net.Sockets;
using System.Text;

namespace ChatClient;

/// <summary>
/// 网络通信模块，封装 TcpClient
/// </summary>
public class NetworkClient : IDisposable
{
    private TcpClient? _tcpClient;
    private NetworkStream? _stream;
    private Thread? _receiveThread;
    private volatile bool _running;
    private readonly byte[] _recvBuffer = new byte[8192];
    private readonly List<byte> _buffer = new();

    public string? Username { get; set; }
    public bool IsConnected => _tcpClient?.Connected ?? false;

    /// <summary>缓存最后一次收到的用户列表，供 ChatForm 初始化使用</summary>
    public string? LastUserList { get; private set; }

    // 事件
    public event Action? OnConnected;
    public event Action? OnDisconnected;
    public event Action<string>? OnConnectionError;
    public event Action<Message>? OnMessageReceived;

    /// <summary>
    /// 连接到服务器
    /// </summary>
    public bool Connect(string ip, int port)
    {
        try
        {
            _tcpClient = new TcpClient();
            _tcpClient.Connect(ip, port);
            _stream = _tcpClient.GetStream();
            _running = true;

            _receiveThread = new Thread(ReceiveLoop) { IsBackground = true };
            _receiveThread.Start();

            OnConnected?.Invoke();
            return true;
        }
        catch (Exception ex)
        {
            OnConnectionError?.Invoke($"连接失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 发送消息
    /// </summary>
    public void Send(Message msg)
    {
        if (_stream == null || !IsConnected) return;
        try
        {
            byte[] packet = msg.Encode();
            _stream.Write(packet, 0, packet.Length);
        }
        catch (Exception ex)
        {
            OnConnectionError?.Invoke($"发送失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 断开连接
    /// </summary>
    public void Disconnect()
    {
        _running = false;
        try { _stream?.Close(); } catch { }
        try { _tcpClient?.Close(); } catch { }
    }

    /// <summary>
    /// 接收线程：持续读取数据并解析消息
    /// </summary>
    private void ReceiveLoop()
    {
        while (_running && _stream != null)
        {
            try
            {
                int n = _stream.Read(_recvBuffer, 0, _recvBuffer.Length);
                if (n == 0) break; // 连接断开

                // 追加到缓冲区
                lock (_buffer)
                {
                    _buffer.AddRange(_recvBuffer.Take(n));
                }

                // 尝试解析消息
                TryParseMessages();
            }
            catch (Exception)
            {
                break;
            }
        }

        _running = false;
        OnDisconnected?.Invoke();
    }

    /// <summary>
    /// 从缓冲区提取完整消息
    /// </summary>
    private void TryParseMessages()
    {
        while (true)
        {
            byte[]? buf;
            lock (_buffer)
            {
                if (_buffer.Count < 4) return;
                buf = _buffer.ToArray();
            }

            // 读取大端长度
            int payloadLen = (buf[0] << 24) | (buf[1] << 16) | (buf[2] << 8) | buf[3];
            if (buf.Length < 4 + payloadLen) return;

            // 解码消息
            byte[] payload = new byte[payloadLen];
            Array.Copy(buf, 4, payload, 0, payloadLen);
            var msg = Message.Decode(payload);

            // 移除已处理数据
            lock (_buffer)
            {
                _buffer.RemoveRange(0, 4 + payloadLen);
            }

            // 缓存用户列表（无论谁订阅了事件都能获取到）
            if (msg.Type == MessageType.USERS && msg.Args.Count > 0)
                LastUserList = msg.Args[0];

            // 触发回调（捕获异常防止单个处理器崩溃导致接收循环中断）
            try {
                OnMessageReceived?.Invoke(msg);
            } catch {
                // 忽略处理器异常，继续接收后续消息
            }
        }
    }

    public void Dispose()
    {
        Disconnect();
        _tcpClient?.Dispose();
        _stream?.Dispose();
    }
}
