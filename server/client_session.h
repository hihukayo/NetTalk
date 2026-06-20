#ifndef CLIENT_SESSION_H
#define CLIENT_SESSION_H

/*
 * ==========================================================
 * ClientSession — 管理单个客户端连接
 * ==========================================================
 * 每个连接的客户端对应一个 ClientSession 对象。
 * 负责: 接收数据 → 解析消息 → 处理业务逻辑 → 发送响应
 * ==========================================================
 */

#include <string>
#include <vector>
#include <functional>
#include <cstdint>
#include <cstring>
#include <algorithm>
#include <unistd.h>
#include <sys/socket.h>
#include "protocol.h"

class ClientSession {
public:
    // 回调函数类型：当收到完整消息时调用
    using MessageCallback = std::function<void(ClientSession*, const Message&)>;
    // 回调函数类型：当连接断开时调用
    using DisconnectCallback = std::function<void(ClientSession*)>;

    ClientSession(int fd, MessageCallback onMsg, DisconnectCallback onDisc)
        : sockfd_(fd)
        , onMessage_(std::move(onMsg))
        , onDisconnect_(std::move(onDisc))
        , loggedIn_(false)
    {
    }

    ~ClientSession() {
        close();
    }

    // 禁止拷贝
    ClientSession(const ClientSession&) = delete;
    ClientSession& operator=(const ClientSession&) = delete;

    int fd() const { return sockfd_; }

    const std::string& username() const { return username_; }
    void setUsername(const std::string& name) { username_ = name; loggedIn_ = true; }

    bool isLoggedIn() const { return loggedIn_; }

    // 发送消息
    bool sendMessage(const Message& msg) {
        auto packet = msg.encode();
        return sendRaw(packet.data(), packet.size());
    }

    // 读取数据（由事件循环调用）
    // 返回 true 表示连接正常，false 表示断开
    bool onReadable() {
        char buf[4096];
        ssize_t n = read(sockfd_, buf, sizeof(buf));

        if (n <= 0) {
            // 连接断开或出错
            if (onDisconnect_) onDisconnect_(this);
            return false;
        }

        // 把新数据追加到接收缓冲区
        recvBuf_.insert(recvBuf_.end(), buf, buf + n);

        // 尝试解析出完整的消息
        while (tryDecodeMessage()) {
            // 成功解析出一条消息，回调
        }

        return true;
    }

    // 关闭连接
    void close() {
        if (sockfd_ >= 0) {
            ::close(sockfd_);
            sockfd_ = -1;
        }
        recvBuf_.clear();
    }

private:
    int sockfd_;
    std::string username_;
    bool loggedIn_;

    // 接收缓冲区
    std::vector<char> recvBuf_;

    MessageCallback onMessage_;
    DisconnectCallback onDisconnect_;

    // 发送原始数据
    bool sendRaw(const char* data, size_t len) {
        if (sockfd_ < 0) return false;
        ssize_t sent = write(sockfd_, data, len);
        return sent == static_cast<ssize_t>(len);
    }

    // 尝试从缓冲区解析出一条完整消息
    // 成功则触发回调并返回 true，缓冲区不足则返回 false
    bool tryDecodeMessage() {
        // 至少需要4字节长度头
        if (recvBuf_.size() < 4) return false;

        // 读取大端长度
        uint32_t payloadLen =
            (static_cast<unsigned char>(recvBuf_[0]) << 24) |
            (static_cast<unsigned char>(recvBuf_[1]) << 16) |
            (static_cast<unsigned char>(recvBuf_[2]) << 8)  |
            (static_cast<unsigned char>(recvBuf_[3]));

        // 检查是否收齐了整个 payload
        if (recvBuf_.size() < 4 + payloadLen) return false;

        // 解码消息
        Message msg = Message::decode(recvBuf_.data() + 4, payloadLen);

        // 从缓冲区移除已处理的数据
        recvBuf_.erase(recvBuf_.begin(), recvBuf_.begin() + 4 + payloadLen);

        // 触发回调
        if (onMessage_) onMessage_(this, msg);

        return true;
    }
};

#endif // CLIENT_SESSION_H
