using System.Net.Sockets;

namespace ChatClient;

/// <summary>
/// 登录窗口 — 美观设计版
/// </summary>
public class LoginForm : Form
{
    private readonly TextBox _ipBox;
    private readonly TextBox _portBox;
    private readonly TextBox _usernameBox;
    private readonly Button _connectBtn;
    private readonly Label _statusLabel;
    private readonly Panel _inputPanel;
    private readonly NetworkClient _client;

    // 主题色
    private static readonly Color Primary = Color.FromArgb(7, 193, 96);
    private static readonly Color PrimaryDark = Color.FromArgb(5, 160, 80);
    private static readonly Color BgLight = Color.FromArgb(245, 247, 250);
    private static readonly Color BorderColor = Color.FromArgb(220, 224, 230);

    public LoginForm(NetworkClient client)
    {
        _client = client;
        _client.OnConnected += OnConnected;
        _client.OnConnectionError += OnError;
        _client.OnMessageReceived += OnMessage;

        Text = "TCP 聊天室 - 登录";
        Size = new Size(720, 740);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = BgLight;

        // ====== 顶部绿色区域 ======
        var topPanel = new Panel
        {
            Height = 140,
            Dock = DockStyle.Top,
            BackColor = Primary
        };

        var titleLabel = new Label
        {
            Text = "💬 TCP 聊天室",
            Font = new Font("微软雅黑", 22, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            BackColor = Color.Transparent
        };
        titleLabel.Location = new Point(
            (topPanel.Width - titleLabel.Width) / 2, 35);
        topPanel.Controls.Add(titleLabel);

        var subtitleLabel = new Label
        {
            Text = "Linux 课设大作业",
            Font = new Font("微软雅黑", 11),
            ForeColor = Color.FromArgb(200, 255, 220),
            AutoSize = true,
            BackColor = Color.Transparent
        };
        subtitleLabel.Location = new Point(
            topPanel.Width - subtitleLabel.Width - 25, 100);
        topPanel.Resize += (s, e) =>
        {
            subtitleLabel.Location = new Point(
                topPanel.Width - subtitleLabel.Width - 25, 100);
        };
        topPanel.Controls.Add(subtitleLabel);

        Controls.Add(topPanel);

        // ====== 中间白色卡片（输入区）======
        _inputPanel = new Panel
        {
            BackColor = Color.White,
            Size = new Size(460, 420),
            Location = new Point(130, 170)
        };
        _inputPanel.Paint += (s, e) =>
        {
            var g = e.Graphics;
            using var pen = new Pen(BorderColor, 1);
            var rect = new Rectangle(0, 0, _inputPanel.Width - 1, _inputPanel.Height - 1);
            g.DrawRectangle(pen, rect);
        };

        // 卡片标题
        var cardTitle = new Label
        {
            Text = "连接到服务器",
            Font = new Font("微软雅黑", 14, FontStyle.Bold),
            ForeColor = Color.FromArgb(51, 51, 51),
            AutoSize = true,
            Location = new Point(35, 20)
        };
        _inputPanel.Controls.Add(cardTitle);

        // 分隔线
        var line = new Label
        {
            Width = 390,
            Height = 1,
            BackColor = BorderColor,
            Location = new Point(35, 50)
        };
        _inputPanel.Controls.Add(line);

        // ---- 输入字段 ----
        int startY = 70;
        int fieldWidth = 380;
        int rowH = 82;        // 每行高度
        int labelY = -5;      // 标签距行顶
        int boxY = 25;        // 输入框距行顶

        // 服务器
        var lblIp = new Label
        {
            Text = "服务器",
            Font = new Font("微软雅黑", 11, FontStyle.Bold),
            ForeColor = Color.FromArgb(80, 80, 80),
            Location = new Point(35, startY + labelY),
            AutoSize = true
        };
        _inputPanel.Controls.Add(lblIp);

        _ipBox = new TextBox
        {
            Text = "192.168.186.130",
            Font = new Font("微软雅黑", 12),
            Location = new Point(35, startY + boxY),
            Width = fieldWidth,
            Height = 34,
            BorderStyle = BorderStyle.FixedSingle,
            ForeColor = Color.FromArgb(51, 51, 51)
        };
        _inputPanel.Controls.Add(_ipBox);

        // 端口
        startY += rowH;
        var lblPort = new Label
        {
            Text = "端口",
            Font = new Font("微软雅黑", 11, FontStyle.Bold),
            ForeColor = Color.FromArgb(80, 80, 80),
            Location = new Point(35, startY + labelY),
            AutoSize = true
        };
        _inputPanel.Controls.Add(lblPort);

        _portBox = new TextBox
        {
            Text = "8888",
            Font = new Font("微软雅黑", 12),
            Location = new Point(35, startY + boxY),
            Width = fieldWidth,
            Height = 34,
            BorderStyle = BorderStyle.FixedSingle,
            ForeColor = Color.FromArgb(51, 51, 51)
        };
        _inputPanel.Controls.Add(_portBox);

        // 昵称
        startY += rowH;
        var lblName = new Label
        {
            Text = "昵称",
            Font = new Font("微软雅黑", 11, FontStyle.Bold),
            ForeColor = Color.FromArgb(80, 80, 80),
            Location = new Point(35, startY + labelY),
            AutoSize = true
        };
        _inputPanel.Controls.Add(lblName);

        _usernameBox = new TextBox
        {
            Font = new Font("微软雅黑", 12),
            Location = new Point(35, startY + boxY),
            Width = fieldWidth,
            Height = 34,
            BorderStyle = BorderStyle.FixedSingle,
            MaxLength = 20,
            ForeColor = Color.FromArgb(51, 51, 51)
        };
        _usernameBox.KeyPress += (s, e) =>
        {
            if (e.KeyChar == (char)Keys.Enter) TryConnect();
        };
        _inputPanel.Controls.Add(_usernameBox);

        // 连接按钮
        startY += rowH + 5;
        _connectBtn = new Button
        {
            Text = "🔗  连接服务器",
            Size = new Size(fieldWidth, 44),
            Location = new Point(35, startY),
            FlatStyle = FlatStyle.Flat,
            FlatAppearance = { BorderSize = 0 },
            BackColor = Primary,
            ForeColor = Color.White,
            Font = new Font("微软雅黑", 13, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _connectBtn.MouseEnter += (s, e) => _connectBtn.BackColor = PrimaryDark;
        _connectBtn.MouseLeave += (s, e) => _connectBtn.BackColor = Primary;
        _connectBtn.Click += (s, e) => TryConnect();
        _inputPanel.Controls.Add(_connectBtn);

        Controls.Add(_inputPanel);

        // ====== 底部状态栏 ======
        _statusLabel = new Label
        {
            Text = "就绪",
            ForeColor = Color.FromArgb(150, 150, 150),
            Font = new Font("微软雅黑", 10),
            AutoSize = true,
            BackColor = Color.Transparent
        };
        _statusLabel.Location = new Point(
            (Width - _statusLabel.Width) / 2, 600);
        Controls.Add(_statusLabel);

        // 窗口改变时保持居中
        Resize += (s, e) =>
        {
            _inputPanel.Location = new Point(
                (ClientSize.Width - _inputPanel.Width) / 2,
                _inputPanel.Location.Y);
            _statusLabel.Location = new Point(
                (ClientSize.Width - _statusLabel.Width) / 2, 600);
        };
    }

    private void TryConnect()
    {
        string ip = _ipBox.Text.Trim();
        if (string.IsNullOrEmpty(ip)) { MessageBox.Show("请输入服务器 IP"); return; }

        if (!int.TryParse(_portBox.Text.Trim(), out int port) || port < 1 || port > 65535)
        { MessageBox.Show("端口无效 (1-65535)"); return; }

        string username = _usernameBox.Text.Trim();
        if (string.IsNullOrEmpty(username) || username.Length > 20)
        { MessageBox.Show("昵称长度需 1-20 个字符"); return; }

        _client.Username = username;
        _connectBtn.Enabled = false;
        _connectBtn.Text = "⏳  连接中...";
        SetStatus("正在连接...", Primary);

        Task.Run(() => _client.Connect(ip, port));
    }

    private void SetStatus(string text, Color color)
    {
        _statusLabel.Text = text;
        _statusLabel.ForeColor = color;
        _statusLabel.Location = new Point(
            (ClientSize.Width - _statusLabel.Width) / 2, 600);
    }

    private void OnConnected()
    {
        this.Invoke(() =>
        {
            SetStatus("✅ 已连接，正在登录...", Primary);
            var loginMsg = new Message { Type = MessageType.LOGIN };
            loginMsg.Args.Add(_client.Username!);
            _client.Send(loginMsg);
        });
    }

    private void OnError(string error)
    {
        this.Invoke(() =>
        {
            SetStatus($"❌ {error}", Color.FromArgb(220, 50, 50));
            _connectBtn.Enabled = true;
            _connectBtn.Text = "🔗  连接服务器";
        });
    }

    private void OnMessage(Message msg)
    {
        this.Invoke(() =>
        {
            if (msg.Type == MessageType.OK)
            {
                // 登录成功，取消订阅防止后续消息持续 Invoke 已关闭的窗体
                _client.OnMessageReceived -= OnMessage;
                DialogResult = DialogResult.OK;
                Close();
            }
            else if (msg.Type == MessageType.ERR)
            {
                string reason = msg.Args.Count > 0 ? msg.Args[0] : "未知错误";
                SetStatus($"❌ {reason}", Color.FromArgb(220, 50, 50));
                _connectBtn.Enabled = true;
                _connectBtn.Text = "🔗  连接服务器";
                _client.Disconnect();
            }
        });
    }
}