#ifndef PROTOCOL_H
#define PROTOCOL_H

/*
 * ==========================================================
 * 协议定义
 * ==========================================================
 * 通信格式：[4字节 payload长度(大端)] + [payload(UTF-8文本)]
 *
 * payload 格式: TYPE|arg1|arg2|...
 *
 * 消息类型:
 *   LOGIN|用户名          → 客户端请求登录
 *   OK                    → 服务端确认成功
 *   ERR|错误信息          → 服务端返回错误
 *   MSG|发送者|内容       → 服务端广播群聊消息
 *   PRIVATE|发送者|内容   → 服务端转发私聊消息
 *   USERS|用户1,用户2,... → 服务端发送在线用户列表
 *   JOINED|用户名         → 服务端通知新用户加入
 *   LEFT|用户名           → 服务端通知用户离开
 *   QUIT                  → 客户端请求断开
 *   PING                  → 心跳检测
 *   PONG                  → 心跳回复
 * ==========================================================
 */

#include <cstdint>
#include <string>
#include <vector>
#include <sstream>

// 消息类型枚举
enum class MessageType : uint8_t {
    LOGIN    = 0x01,   // 登录请求
    OK       = 0x02,   // 成功响应
    ERR      = 0x03,   // 错误响应
    MSG      = 0x04,   // 群聊消息
    PRIVATE  = 0x05,   // 私聊消息
    USERS    = 0x06,   // 用户列表
    JOINED   = 0x07,   // 用户加入
    LEFT     = 0x08,   // 用户离开
    QUIT     = 0x09,   // 断开连接
    PING     = 0x0A,   // 心跳
    PONG     = 0x0B    // 心跳回复
};

// 用字符串表示的消息类型前缀（供调试/日志使用）
inline const char* msgTypeToString(MessageType type) {
    switch (type) {
        case MessageType::LOGIN:    return "LOGIN";
        case MessageType::OK:       return "OK";
        case MessageType::ERR:      return "ERR";
        case MessageType::MSG:      return "MSG";
        case MessageType::PRIVATE:  return "PRIVATE";
        case MessageType::USERS:    return "USERS";
        case MessageType::JOINED:   return "JOINED";
        case MessageType::LEFT:     return "LEFT";
        case MessageType::QUIT:     return "QUIT";
        case MessageType::PING:     return "PING";
        case MessageType::PONG:     return "PONG";
        default:                    return "UNKNOWN";
    }
}

// ==========================================================
// 消息结构体
// ==========================================================
struct Message {
    MessageType type;
    std::vector<std::string> args;  // 按 '|' 分割的参数列表

    // 将消息编码为网络字节流
    // 格式: [4字节大端长度][payload]
    std::vector<char> encode() const {
        // 构造 payload: "TYPE|arg1|arg2|..."
        std::ostringstream oss;
        oss << static_cast<int>(type);
        for (const auto& arg : args) {
            oss << '|' << arg;
        }
        std::string payload = oss.str();

        // 总长度 = 4字节长度头 + payload
        uint32_t len = static_cast<uint32_t>(payload.size());
        std::vector<char> packet(4 + len);

        // 写入大端长度
        packet[0] = static_cast<char>((len >> 24) & 0xFF);
        packet[1] = static_cast<char>((len >> 16) & 0xFF);
        packet[2] = static_cast<char>((len >> 8) & 0xFF);
        packet[3] = static_cast<char>(len & 0xFF);

        // 写入 payload
        std::copy(payload.begin(), payload.end(), packet.begin() + 4);

        return packet;
    }

    // 从网络字节流解码消息
    // data: 包含 payload 的完整数据（不含4字节长度头）
    static Message decode(const char* data, size_t len) {
        Message msg;
        std::string s(data, len);

        // 解析第一个 '|' 之前的内容作为 type
        auto pos = s.find('|');
        if (pos == std::string::npos) {
            // 只有 type，没有参数
            msg.type = static_cast<MessageType>(std::stoi(s));
            return msg;
        }

        // 解析 type
        msg.type = static_cast<MessageType>(std::stoi(s.substr(0, pos)));
        s = s.substr(pos + 1);

        // 解析剩余参数（按 '|' 分割）
        while (!s.empty()) {
            auto pipe = s.find('|');
            if (pipe == std::string::npos) {
                msg.args.push_back(s);
                break;
            }
            msg.args.push_back(s.substr(0, pipe));
            s = s.substr(pipe + 1);
        }

        return msg;
    }
};

#endif // PROTOCOL_H
