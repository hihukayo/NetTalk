# ==========================================================
# Makefile — TCP 聊天室服务端
# ==========================================================
# 在 Linux 虚拟机中运行:
#   make          # 编译
#   make run      # 编译并运行（默认端口 8888）
#   make run ARGS=6666  # 指定端口
#   make clean    # 清理
# ==========================================================

CXX      = g++
CXXFLAGS = -std=c++17 -O2 -Wall -Wextra -pthread
LDFLAGS  =
TARGET   = chat_server
SRCS     = main.cpp
OBJS     = $(SRCS:.cpp=.o)

.PHONY: all run clean

all: $(TARGET)

$(TARGET): $(OBJS)
	$(CXX) $(CXXFLAGS) -o $@ $^ $(LDFLAGS)

%.o: %.cpp
	$(CXX) $(CXXFLAGS) -c -o $@ $<

run: $(TARGET)
	./$(TARGET) $(ARGS)

clean:
	rm -f $(OBJS) $(TARGET)
