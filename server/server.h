#ifndef CHAT_SERVER_H
#define CHAT_SERVER_H

/*
 * ==========================================================
 * ChatServer — TCP 聊天室服务端
 * ==========================================================
 * 基于 epoll 的并发 TCP 服务器。
 *
 * 功能:
 *   - 多客户端同时在线
 *   - 群聊（所有人可见）
 *   - 私聊（两人私密）
 *   - 在线用户列表
 *   - 用户上线/下线通知
 *   - 心跳检测
 * ==========================================================
 */

#include <string>
#include <map>
#include <memory>
#include <vector>
#include <algorithm>
#include <functional>
#include <iostream>
#include <ctime>

#include <sys/epoll.h>
#include <sys/socket.h>
#include <netinet/in.h>
#include <arpa/inet.h>
#include <fcntl.h>
#include <unistd.h>
#include <cstring>

#include "protocol.h"
#include "client_session.h"

// 最大并发连接数
const int MAX_EVENTS = 1024;
// 心跳超时时间（秒）
const int HEARTBEAT_TIMEOUT = 60;

class ChatServer {
public:
    ChatServer() : epollFd_(-1), serverFd_(-1), running_(false) {}

    ~ChatServer() { stop(); }

    // 启动服务器，监听指定端口
    bool start(uint16_t port) {
        // 1. 创建 socket
        serverFd_ = socket(AF_INET, SOCK_STREAM | SOCK_NONBLOCK, 0);
        if (serverFd_ < 0) {
            std::cerr << "[错误] 创建 socket 失败: " << strerror(errno) << std::endl;
            return false;
        }

        // 2. 设置 SO_REUSEADDR，避免端口被占用
        int opt = 1;
        setsockopt(serverFd_, SOL_SOCKET, SO_REUSEADDR, &opt, sizeof(opt));

        // 3. 绑定端口
        struct sockaddr_in addr;
        std::memset(&addr, 0, sizeof(addr));
        addr.sin_family = AF_INET;
        addr.sin_addr.s_addr = INADDR_ANY;   // 监听所有网卡
        addr.sin_port = htons(port);

        if (bind(serverFd_, (struct sockaddr*)&addr, sizeof(addr)) < 0) {
            std::cerr << "[错误] 绑定端口 " << port << " 失败: " << strerror(errno) << std::endl;
            ::close(serverFd_);
            return false;
        }

        // 4. 开始监听
        if (listen(serverFd_, 128) < 0) {
            std::cerr << "[错误] 监听失败: " << strerror(errno) << std::endl;
            ::close(serverFd_);
            return false;
        }

        // 5. 创建 epoll 实例
        epollFd_ = epoll_create1(0);
        if (epollFd_ < 0) {
            std::cerr << "[错误] 创建 epoll 失败: " << strerror(errno) << std::endl;
            ::close(serverFd_);
            return false;
        }

        // 6. 注册监听 socket 到 epoll
        struct epoll_event ev;
        ev.events = EPOLLIN | EPOLLET;  // 边缘触发
        ev.data.fd = serverFd_;
        if (epoll_ctl(epollFd_, EPOLL_CTL_ADD, serverFd_, &ev) < 0) {
            std::cerr << "[错误] epoll 注册失败" << std::endl;
            ::close(epollFd_);
            ::close(serverFd_);
            return false;
        }

        running_ = true;
        std::cout << "============================================" << std::endl;
        std::cout << "  💬 TCP 聊天室服务器已启动" << std::endl;
        std::cout << "  监听端口: " << port << std::endl;
        std::cout << "  服务器 IP: ";
        // 显示本机 IP
        char hostname[256];
        gethostname(hostname, sizeof(hostname));
        std::cout << getLocalIP() << std::endl;
        std::cout << "============================================" << std::endl;
        std::cout << "等待客户端连接..." << std::endl;

        return true;
    }

    // 主事件循环
    void run() {
        if (!running_) return;

        struct epoll_event events[MAX_EVENTS];

        while (running_) {
            int nfds = epoll_wait(epollFd_, events, MAX_EVENTS, 1000); // 1秒超时，用于定时检测心跳

            for (int i = 0; i < nfds; ++i) {
                if (events[i].data.fd == serverFd_) {
                    // 有新连接
                    handleNewConnection();
                } else {
                    // 客户端有数据
                    auto it = sessions_.find(events[i].data.fd);
                    if (it != sessions_.end()) {
                        if (!it->second->onReadable()) {
                            // 客户端断开，清理
                            removeSession(it->second.get());
                        }
                    }
                }
            }

            // 心跳检测：移除超时的客户端
            checkHeartbeat();
        }
    }

    // 停止服务器
    void stop() {
        running_ = false;
        for (auto& [fd, session] : sessions_) {
            session->close();
        }
        sessions_.clear();

        if (serverFd_ >= 0) {
            ::close(serverFd_);
            serverFd_ = -1;
        }
        if (epollFd_ >= 0) {
            ::close(epollFd_);
            epollFd_ = -1;
        }
        std::cout << "[信息] 服务器已停止" << std::endl;
    }

    bool isRunning() const { return running_; }

private:
    int epollFd_;
    int serverFd_;
    bool running_;

    // 所有已连接的客户端 <socket_fd, ClientSession>
    std::map<int, std::unique_ptr<ClientSession>> sessions_;
    // 用户名 → socket_fd 映射
    std::map<std::string, int> nameToFd_;
    // 记录最后活跃时间（用于心跳检测）
    std::map<int, time_t> lastActive_;

    // ==========================================================
    // 处理新客户端连接
    // ==========================================================
    void handleNewConnection() {
        struct sockaddr_in clientAddr;
        socklen_t addrLen = sizeof(clientAddr);

        while (true) {
            int clientFd = accept4(serverFd_, (struct sockaddr*)&clientAddr,
                                   &addrLen, SOCK_NONBLOCK);
            if (clientFd < 0) {
                if (errno == EAGAIN || errno == EWOULDBLOCK) break;
                std::cerr << "[错误] accept 失败: " << strerror(errno) << std::endl;
                break;
            }

            char ipStr[INET_ADDRSTRLEN];
            inet_ntop(AF_INET, &clientAddr.sin_addr, ipStr, sizeof(ipStr));
            int port = ntohs(clientAddr.sin_port);
            std::cout << "[连接] 新客户端: " << ipStr << ":" << port << std::endl;

            // 注册到 epoll
            struct epoll_event ev;
            ev.events = EPOLLIN | EPOLLET;
            ev.data.fd = clientFd;
            epoll_ctl(epollFd_, EPOLL_CTL_ADD, clientFd, &ev);

            // 创建 ClientSession
            auto session = std::make_unique<ClientSession>(
                clientFd,
                // 收到消息的回调
                [this](ClientSession* sender, const Message& msg) {
                    handleMessage(sender, msg);
                },
                // 断开的回调
                [this](ClientSession* session) {
                    // 已经在 removeSession 中处理，这里主要是清理资源
                }
            );

            sessions_[clientFd] = std::move(session);
            lastActive_[clientFd] = time(nullptr);
        }
    }

    // ==========================================================
    // 处理客户端消息
    // ==========================================================
    void handleMessage(ClientSession* sender, const Message& msg) {
        // 更新活跃时间
        lastActive_[sender->fd()] = time(nullptr);

        switch (msg.type) {
            case MessageType::LOGIN:
                handleLogin(sender, msg);
                break;

            case MessageType::MSG:
                handleChatMessage(sender, msg);
                break;

            case MessageType::PRIVATE:
                handlePrivateMessage(sender, msg);
                break;

            case MessageType::QUIT:
                handleLogout(sender);
                break;

            case MessageType::PING:
                handlePing(sender);
                break;

            default:
                std::cout << "[警告] 未知消息类型: "
                          << static_cast<int>(msg.type) << std::endl;
                break;
        }
    }

    // ==========================================================
    // 处理登录
    // ==========================================================
    void handleLogin(ClientSession* session, const Message& msg) {
        if (msg.args.empty()) {
            Message errMsg;
            errMsg.type = MessageType::ERR;
            errMsg.args = {"用户名不能为空"};
            session->sendMessage(errMsg);
            return;
        }

        std::string username = msg.args[0];

        // 检查用户名是否已被占用
        if (nameToFd_.find(username) != nameToFd_.end()) {
            Message errMsg;
            errMsg.type = MessageType::ERR;
            errMsg.args = {"用户名已被占用，请换一个"};
            session->sendMessage(errMsg);
            return;
        }

        // 检查用户名长度
        if (username.empty() || username.length() > 20) {
            Message errMsg;
            errMsg.type = MessageType::ERR;
            errMsg.args = {"用户名长度需在 1-20 个字符之间"};
            session->sendMessage(errMsg);
            return;
        }

        // 登录成功
        session->setUsername(username);
        nameToFd_[username] = session->fd();

        // 发送 OK 确认
        Message okMsg;
        okMsg.type = MessageType::OK;
        session->sendMessage(okMsg);

        // 广播新用户加入
        Message joinedMsg;
        joinedMsg.type = MessageType::JOINED;
        joinedMsg.args = {username};
        broadcast(joinedMsg, nullptr);

        // 向所有用户广播更新后的用户列表
        broadcastUserList();

        std::cout << "[登录] " << username << " 加入了聊天室" << std::endl;
    }

    // ==========================================================
    // 处理群聊消息
    // ==========================================================
    void handleChatMessage(ClientSession* session, const Message& msg) {
        if (!session->isLoggedIn()) {
            Message errMsg;
            errMsg.type = MessageType::ERR;
            errMsg.args = {"请先登录"};
            session->sendMessage(errMsg);
            return;
        }

        if (msg.args.empty() || msg.args[0].empty()) {
            Message errMsg;
            errMsg.type = MessageType::ERR;
            errMsg.args = {"消息不能为空"};
            session->sendMessage(errMsg);
            return;
        }

        // 广播群聊消息给所有在线用户（含发送者自己）
        Message bcastMsg;
        bcastMsg.type = MessageType::MSG;
        bcastMsg.args = {session->username(), msg.args[0]};
        broadcast(bcastMsg, nullptr);

        std::cout << "[群聊] " << session->username() << ": "
                  << msg.args[0] << std::endl;
    }

    // ==========================================================
    // 处理私聊消息
    // ==========================================================
    void handlePrivateMessage(ClientSession* session, const Message& msg) {
        if (!session->isLoggedIn()) {
            Message errMsg;
            errMsg.type = MessageType::ERR;
            errMsg.args = {"请先登录"};
            session->sendMessage(errMsg);
            return;
        }

        if (msg.args.size() < 2) {
            Message errMsg;
            errMsg.type = MessageType::ERR;
            errMsg.args = {"私聊格式错误，请使用: /w 用户名 消息"};
            session->sendMessage(errMsg);
            return;
        }

        std::string target = msg.args[0];
        std::string content = msg.args[1];

        // 检查目标用户是否存在
        auto it = nameToFd_.find(target);
        if (it == nameToFd_.end()) {
            Message errMsg;
            errMsg.type = MessageType::ERR;
            errMsg.args = {"用户 '" + target + "' 不在线"};
            session->sendMessage(errMsg);
            return;
        }

        // 转发私聊消息给目标用户
        Message privateMsg;
        privateMsg.type = MessageType::PRIVATE;
        privateMsg.args = {session->username(), content};

        auto targetSession = sessions_[it->second].get();
        if (targetSession) {
            targetSession->sendMessage(privateMsg);
        }

        // 也回显给发送者（让他看到自己发了什么）
        Message echoMsg;
        echoMsg.type = MessageType::PRIVATE;
        echoMsg.args = {"[私聊 -> " + target + "]", content};
        session->sendMessage(echoMsg);

        std::cout << "[私聊] " << session->username() << " → "
                  << target << ": " << content << std::endl;
    }

    // ==========================================================
    // 处理登出
    // ==========================================================
    void handleLogout(ClientSession* session) {
        std::string username = session->username();
        std::cout << "[登出] " << username << " 离开了聊天室" << std::endl;
        removeSession(session);
    }

    // ==========================================================
    // 处理心跳
    // ==========================================================
    void handlePing(ClientSession* session) {
        Message pong;
        pong.type = MessageType::PONG;
        session->sendMessage(pong);
    }

    // ==========================================================
    // 移除会话
    // ==========================================================
    void removeSession(ClientSession* session) {
        std::string username = session->username();

        // 清理映射关系
        if (!username.empty()) {
            nameToFd_.erase(username);
        }
        lastActive_.erase(session->fd());
        sessions_.erase(session->fd());

        // 广播用户离开
        if (!username.empty()) {
            Message leftMsg;
            leftMsg.type = MessageType::LEFT;
            leftMsg.args = {username};
            broadcast(leftMsg, nullptr);

            // 更新用户列表
            broadcastUserList();
            std::cout << "[断开] " << username << " 已断开连接" << std::endl;
        } else {
            std::cout << "[断开] 未登录的客户端已断开连接" << std::endl;
        }
    }

    // ==========================================================
    // 广播消息（可选择排除某个客户端）
    // ==========================================================
    void broadcast(const Message& msg, ClientSession* exclude) {
        auto packet = msg.encode();
        for (auto& [fd, session] : sessions_) {
            if (session.get() != exclude) {
                // 非阻塞发送，忽略错误
                write(fd, packet.data(), packet.size());
            }
        }
    }

    // ==========================================================
    // 广播在线用户列表（发送给所有已登录用户）
    // ==========================================================
    void broadcastUserList() {
        Message userMsg;
        userMsg.type = MessageType::USERS;

        std::string userList;
        for (const auto& [name, fd] : nameToFd_) {
            if (!userList.empty()) userList += ",";
            userList += name;
        }
        userMsg.args = {userList};

        // 发送给所有已登录用户
        auto packet = userMsg.encode();
        for (auto& [fd, session] : sessions_) {
            if (session->isLoggedIn()) {
                write(fd, packet.data(), packet.size());
            }
        }
    }

    // ==========================================================
    // 心跳检测：移除长时间无响应的客户端
    // ==========================================================
    void checkHeartbeat() {
        time_t now = time(nullptr);
        std::vector<int> toRemove;

        for (auto& [fd, session] : sessions_) {
            auto it = lastActive_.find(fd);
            if (it != lastActive_.end()) {
                if (now - it->second > HEARTBEAT_TIMEOUT) {
                    toRemove.push_back(fd);
                }
            }
        }

        for (int fd : toRemove) {
            auto it = sessions_.find(fd);
            if (it != sessions_.end()) {
                std::cout << "[心跳] " << it->second->username()
                          << " 心跳超时，断开连接" << std::endl;
                removeSession(it->second.get());
            }
        }
    }

    // ==========================================================
    // 获取本机局域网IP
    // ==========================================================
    std::string getLocalIP() {
        int fd = socket(AF_INET, SOCK_DGRAM, 0);
        if (fd < 0) return "127.0.0.1";

        struct sockaddr_in addr;
        std::memset(&addr, 0, sizeof(addr));
        addr.sin_family = AF_INET;
        addr.sin_addr.s_addr = inet_addr("8.8.8.8");
        addr.sin_port = htons(53);

        if (connect(fd, (struct sockaddr*)&addr, sizeof(addr)) == 0) {
            struct sockaddr_in localAddr;
            socklen_t len = sizeof(localAddr);
            if (getsockname(fd, (struct sockaddr*)&localAddr, &len) == 0) {
                char ip[INET_ADDRSTRLEN];
                inet_ntop(AF_INET, &localAddr.sin_addr, ip, sizeof(ip));
                ::close(fd);
                return ip;
            }
        }
        ::close(fd);
        return "127.0.0.1";
    }
};

#endif // CHAT_SERVER_H
