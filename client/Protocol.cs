using System.Text;

namespace ChatClient;

/// <summary>
/// 消息类型（与服务端 protocol.h 一致）
/// </summary>
public enum MessageType : byte
{
    LOGIN   = 0x01,   // 登录请求
    OK      = 0x02,   // 成功响应
    ERR     = 0x03,   // 错误响应
    MSG     = 0x04,   // 群聊消息
    PRIVATE = 0x05,   // 私聊消息
    USERS   = 0x06,   // 用户列表
    JOINED  = 0x07,   // 用户加入
    LEFT    = 0x08,   // 用户离开
    QUIT    = 0x09,   // 断开连接
    PING    = 0x0A,   // 心跳
    PONG    = 0x0B,   // 心跳回复
}

/// <summary>
/// 消息结构体，包含类型和参数列表
/// </summary>
public class Message
{
    public MessageType Type { get; set; }
    public List<string> Args { get; set; } = new();

    /// <summary>
    /// 将消息编码为字节流
    /// 格式: [4字节大端长度][UTF8 payload]
    /// Payload: TYPE|arg1|arg2|...
    /// </summary>
    public byte[] Encode()
    {
        string payload = $"{(int)Type}";
        foreach (var arg in Args)
            payload += "|" + arg;

        byte[] payloadBytes = Encoding.UTF8.GetBytes(payload);
        byte[] packet = new byte[4 + payloadBytes.Length];

        // 写入大端长度
        packet[0] = (byte)((payloadBytes.Length >> 24) & 0xFF);
        packet[1] = (byte)((payloadBytes.Length >> 16) & 0xFF);
        packet[2] = (byte)((payloadBytes.Length >> 8) & 0xFF);
        packet[3] = (byte)(payloadBytes.Length & 0xFF);

        // 写入 payload
        Array.Copy(payloadBytes, 0, packet, 4, payloadBytes.Length);

        return packet;
    }

    /// <summary>
    /// 从字节流解码消息
    /// </summary>
    public static Message Decode(byte[] data)
    {
        var msg = new Message();
        string s = Encoding.UTF8.GetString(data);

        int pos = s.IndexOf('|');
        if (pos == -1)
        {
            msg.Type = (MessageType)int.Parse(s);
            return msg;
        }

        msg.Type = (MessageType)int.Parse(s[..pos]);
        s = s[(pos + 1)..];

        msg.Args = s.Split('|').ToList();
        return msg;
    }
}
