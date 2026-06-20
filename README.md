# 💬 TCP 聊天室 — Linux 课设大作业

跨平台 TCP 聊天室项目：
- **服务端** → Linux（C++，epoll 高并发）
- **客户端** → Windows（C# WinForms 图形界面）

---

## 📋 项目结构

```
C:\Users\Hihukayo\Desktop\ChatRoom\
├── server/              ← 📁 放到 Linux 虚拟机编译
│   ├── protocol.h       通信协议
│   ├── client_session.h 客户端连接管理
│   ├── server.h         epoll 并发服务器
│   ├── main.cpp         入口
│   └── Makefile
│
├── client/              ← 🖥️ 在 Windows 上直接编译运行
│   ├── Protocol.cs      协议定义
│   ├── NetworkClient.cs TCP 网络通信
│   ├── LoginForm.cs     登录窗口
│   ├── ChatForm.cs      聊天主界面
│   ├── Program.cs       入口
│   └── ChatClient.csproj
│
└── README.md
```

---

## 🚀 运行方法

### 第一步：启动服务端（在 VMware 虚拟机里）

```bash
# 把 server/ 文件夹传到虚拟机
cd server
make
./chat_server
```

看到以下效果即成功：

```
============================================
  💬 TCP 聊天室服务器已启动
  监听端口: 8888
  服务器 IP: 192.168.x.xxx
============================================
等待客户端连接...
```

> 记下这个 **IP 地址**，客户端连接要用

### 第二步：运行客户端（在 Windows 上）

```bash
# 在 ChatRoom 目录执行
cd client
dotnet run
```

或者在 Visual Studio 里：
1. 打开 `client/ChatClient.csproj`
2. 按 `F5` 运行

## 🎮 功能

| 功能 | 操作 | 说明 |
|------|------|------|
| 群聊 | 输入文字按 Enter | 所有人可见 |
| 私聊 | `/w 用户名 消息` 或 **双击用户列表** | 私密对话 |
| 在线用户 | 右侧列表实时显示 | 自动更新 |
| 退出 | 点右上角「退出」 | 通知服务器下线 |

## ⚙️ 虚拟机网络设置

VMware 里把网卡设为 **桥接模式**，然后：

```bash
# 查看 IP
ip addr show
# 如果防火墙挡了，开放端口
sudo ufw allow 8888
```

---

## 🛠 技术栈

| 部分 | 技术 |
|------|------|
| 服务端 | C++17, epoll, POSIX socket, Makefile |
| 客户端 | C#, .NET 10, WinForms, TcpClient |
| 协议 | 长度前缀二进制协议 (4字节头 + UTF-8 payload) |

## 📝 协议格式

```
[4字节 大端 payload长度] + [UTF-8 payload]

payload = TYPE|arg1|arg2|...

LOGIN|用户名      → 登录请求
OK               → 登录成功
ERR|错误信息      → 错误
MSG|发送者|内容   → 群聊
PRIVATE|发送者|内容 → 私聊
USERS|用户列表    → 在线用户
JOINED|用户名     → 用户加入
LEFT|用户名       → 用户离开
```

---

## 📄 实验报告建议结构

1. 需求分析
2. 概要设计（架构图）
3. 详细设计（协议、epoll、数据结构）
4. 核心代码讲解
5. 运行截图
6. 总结
