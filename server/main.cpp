/*
 * ==========================================================
 * TCP 聊天室 — 服务端入口
 * ==========================================================
 * 编译: g++ -std=c++17 -O2 main.cpp -o chat_server
 * 运行: ./chat_server [端口号]
 *
 * 默认端口: 8888
 * ==========================================================
 */

#include <iostream>
#include <cstdlib>
#include <csignal>
#include "server.h"

ChatServer* g_server = nullptr;

// 处理 Ctrl+C 信号，优雅关闭服务器
void signalHandler(int signum) {
    std::cout << "\n[信息] 收到信号 " << signum << "，正在关闭服务器..." << std::endl;
    if (g_server) {
        g_server->stop();
    }
    exit(0);
}

int main(int argc, char* argv[]) {
    // 注册信号处理函数
    signal(SIGINT, signalHandler);
    signal(SIGTERM, signalHandler);

    // 默认端口 8888
    uint16_t port = 8888;
    if (argc >= 2) {
        int p = std::atoi(argv[1]);
        if (p > 0 && p <= 65535) {
            port = static_cast<uint16_t>(p);
        } else {
            std::cerr << "[错误] 无效端口号，使用默认端口 8888" << std::endl;
        }
    }

    // 创建并启动服务器
    ChatServer server;
    g_server = &server;

    if (!server.start(port)) {
        std::cerr << "[错误] 服务器启动失败！" << std::endl;
        return 1;
    }

    // 进入主事件循环
    server.run();

    return 0;
}
