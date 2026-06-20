<p align="center">
  <img src="https://img.shields.io/badge/C%2B%2B-17-00599C?style=flat&logo=c%2B%2B" alt="C++17"/>
  <img src="https://img.shields.io/badge/C%23-.NET%2010-512BD4?style=flat&logo=csharp" alt="C# .NET 10"/>
  <img src="https://img.shields.io/badge/Linux-epoll-FFCC00?style=flat&logo=linux" alt="Linux epoll"/>
  <img src="https://img.shields.io/badge/Windows-WinForms-0078D6?style=flat&logo=windows" alt="Windows WinForms"/>
  <img src="https://img.shields.io/badge/license-MIT-blue" alt="MIT License"/>
</p>

# NetTalk

> 跨平台 TCP 聊天室 — Linux (C++ epoll) 服务端 + Windows (C# WinForms) 客户端

NetTalk 是一个为 **Linux 网络编程课程设计** 而写的聊天室项目。服务端使用 epoll 边缘触发实现高并发，客户端是 Windows 图形界面，两者通过自定义二进制协议通信。

---

## 功能一览

### 聊天功能

<div align="center">

| 功能 | 说明 |
| :---: | :---: |
| **群聊** | 输入文字按 Enter 发送，所有人可见 |
| **私聊** | `/w 用户名 消息` 或双击用户列表弹出私聊窗口 |
| **在线用户** | 顶部查看在线用户列表，实时更新 |
| **表情面板** | 输入 `/emoji` 弹出分类表情面板（笑脸/手势/心形/物品/动物/食物六大类），点击即插入 |
| **表情短码** | 输入 `:smile:` `:joy:` `:heart:` 等短码自动替换为 Emoji |
| **/emoji 命令** | `/emoji smile` 直接替换为对应表情，`/emoji` 单独输入弹出面板 |
| **@ 提及** | 输入 `@` 弹出用户列表，支持筛选和 Enter/点击选择，自动补全 `@用户名` |

</div>

### 视觉与交互

<div align="center">

| 功能 | 说明 |
| :---: | :---: |
| **@ 蓝色高亮** | 聊天内容中的 `@用户名` 自动染成蓝色加粗 |
| **@ 提醒** | 被 @ 时窗口标题闪烁提示「有人@你」，聊天区显示蓝色提醒 |
| **彩色消息** | 自己发言绿色高亮、时间灰色、系统消息居中显示 |
| **私聊弹窗** | 双击用户列表弹出独立私聊窗口，不影响主界面聊天 |
| **优雅关闭** | 响应 Ctrl+C 和窗口关闭，安全释放所有资源 |

</div>

### 系统功能

<div align="center">

| 功能 | 说明 |
| :---: | :---: |
| **聊天记录** | 自动保存到 `聊天记录_YYYY-MM-DD.txt`，群聊/私聊/上下线/错误全记录 |
| **心跳检测** | 客户端每 30 秒发 PING，服务端 60 秒超时自动清理僵尸连接 |
| **全局异常保护** | 崩溃写入 `crash.log` 不弹窗闪退，WinForms 线程异常全捕获 |
| **断开检测** | 服务端或网络断开时客户端灰色提示，输入框禁用 |

</div>

---

## 技术栈

<div align="center">

| 维度 | 选型 | 理由 |
| :---: | :---: | :---: |
| **服务端语言** | C++17 | 系统编程、Socket 接口原生支持 |
| **I/O 模型** | epoll (ET 边缘触发) | 高并发场景下性能远优于 select/poll |
| **Socket** | POSIX (BSD socket) | Linux 标准网络编程接口 |
| **客户端** | C# .NET 10 + WinForms | Windows 原生 GUI，开发快 |
| **网络传输** | 自定义二进制协议 | 长度前缀法解决粘包，无解析歧义 |
| **构建** | Makefile (服务端) / dotnet (客户端) | 平台原生工具链 |

</div>

---

## 项目结构

```
NetTalk/
├── server/                  放到 Linux 上编译运行
│   ├── protocol.h           通信协议定义与编解码
│   ├── client_session.h     单客户端连接管理 (接收缓冲区 + 粘包处理)
│   ├── server.h             epoll 高并发服务器核心
│   ├── main.cpp             程序入口 + 信号处理
│   └── Makefile             g++ 编译脚本
│
├── client/                  在 Windows 上运行
│   ├── Protocol.cs          消息类型与编解码 (与服务端对应)
│   ├── NetworkClient.cs     TCP 网络通信 (独立接收线程)
│   ├── LoginForm.cs         登录窗口 (IP/端口/用户名)
│   ├── ChatForm.cs          聊天主界面
│   ├── Program.cs           程序入口 + 全局异常处理
│   └── ChatClient.csproj    .NET 10 项目文件
│
├── .gitignore
└── README.md
```

---

## 快速开始

### 前置要求

- **服务端**：Linux 环境 (物理机 / VMware / WSL)，g++ 支持 C++17
- **客户端**：Windows 系统，.NET 10 SDK ([下载](https://dotnet.microsoft.com/download))

### 第一步：启动服务端

```bash
# 1. 将 server/ 目录上传到 Linux 机器，或者直接在 Linux 上克隆

# 2. 编译
cd server
make

# 3. 运行（默认端口 8888）
./chat_server

# 4. 或者指定端口
make run ARGS=6666
```

看到以下输出即成功：

```
============================================
  NetTalk 服务器已启动
  监听端口: 8888
  服务器 IP: 192.168.x.xxx
============================================
等待客户端连接...
```

> 记下这个 **IP 地址**，客户端连接时需要填入。

### 第二步：运行客户端

```bash
cd client
dotnet run
```

或者在 Visual Studio 中打开 `client/ChatClient.csproj`，按 `F5`。

登录界面填入：
- **IP**：服务端的 IP 地址
- **端口**：默认 `8888`
- **用户名**：1-20 个字符，不重复

### 客户端快捷键与命令

<div align="center">

| 操作 | 效果 |
| :---: | :---: |
| `Enter` | 发送消息（支持 Shift+Enter 换行） |
| `/w 用户名 消息` | 发送私聊 |
| `/emoji` | 弹出表情选择面板 |
| `/emoji 名称` | 按名称插入表情（如 `/emoji smile` → 笑脸表情） |
| `:smile:` 等短码 | 输入过程中自动替换为实际 Emoji |
| `@` | 弹出在线用户列表，输入文字可筛选 |
| `Esc` | 关闭表情面板 / @用户面板 |
| `双击用户` | 弹出私聊窗口 |

</div>

---

## 通信协议

### 格式

```
[4字节 大端 payload长度] + [UTF-8 payload]

payload = TYPE|arg1|arg2|...
```

### 消息类型

<div align="center">

| 方向 | 类型 | 说明 |
| :---: | :---: | :---: |
| 客户端->服务端 | `LOGIN|用户名` | 登录请求 |
| 服务端->客户端 | `OK` | 登录成功 |
| 服务端->客户端 | `ERR|错误信息` | 拒绝/错误 |
| 服务端->客户端 | `MSG|发送者|内容` | 群聊广播 |
| 服务端->客户端 | `PRIVATE|发送者|内容` | 私聊转发 |
| 服务端->客户端 | `USERS|用户1,用户2,...` | 在线用户列表 |
| 服务端->客户端 | `JOINED|用户名` | 用户上线通知 |
| 服务端->客户端 | `LEFT|用户名` | 用户下线通知 |
| 客户端->服务端 | `QUIT` | 主动断开 |
| 客户端->服务端 | `PING` | 心跳请求 |
| 服务端->客户端 | `PONG` | 心跳回复 |

</div>

---

## 已完成功能清单

- [x] 群聊 / 私聊 / 在线用户列表
- [x] epoll 边缘触发高并发模型
- [x] 自定义二进制通信协议
- [x] 表情面板（六大分类，200+ Emoji，点击插入）
- [x] Emoji 短码自动替换（`:smile:` -> Emoji）
- [x] @ 提及用户（弹出列表 + 自动补全 + 蓝色高亮）
- [x] 被 @ 时窗口闪烁提醒
- [x] 聊天记录自动保存到本地文件
- [x] 客户端定时心跳（30s）+ 服务端超时检测（60s）
- [x] 客户端全局异常处理 + crash.log
- [x] 服务端优雅关闭（信号处理 + RAII 资源释放）

### 后续可改进

- [ ] 服务端完整发送缓冲区（解决 write 部分发送问题）
- [ ] 服务端日志写入文件（替代 std::cout）
- [ ] 消息已读/未读标记
- [ ] 聊天记录搜索
- [ ] 彩色日志与控制台
- [ ] Docker 一键部署

---

## 许可证

[MIT](LICENSE)
