namespace ChatClient;

/// <summary>
/// 聊天室主窗口
/// </summary>
public class ChatForm : Form
{
    private readonly NetworkClient _client;
    private readonly string _username;
    private readonly RichTextBox _messageBox;
    private readonly TextBox _inputBox;
    private readonly ContextMenuStrip _userMenu;
    private readonly Label _statusLabel;
    private readonly Label _userCountLabel;
    private readonly Button _sendBtn;
    private readonly Button _logoutBtn;
    private readonly System.Windows.Forms.Timer _heartbeatTimer;
    private readonly FlowLayoutPanel _emojiPanel;
    private readonly Panel _emojiContainer;

    // 表情快捷词典
    private static readonly Dictionary<string, string> EmojiMap = new()
    {
        { "smile", "😀" }, { "joy", "😂" }, { "lol", "🤣" }, { "blush", "😊" },
        { "heart_eyes", "😍" }, { "cool", "😎" }, { "wink", "😜" }, { "cry", "😅" },
        { "sweat", "😁" }, { "plead", "🥺" }, { "angry", "😤" }, { "scream", "😱" },
        { "hug", "🤗" }, { "think", "🤔" }, { "sleep", "😴" }, { "partying", "🥳" },
        { "pray", "🙏" }, { "thumbsup", "👍" }, { "thumbsdown", "👎" }, { "clap", "👏" },
        { "fire", "🔥" }, { "star", "⭐" }, { "heart", "❤️" }, { "broken_heart", "💔" },
        { "100", "💯" }, { "tada", "🎉" }, { "confetti", "🎊" }, { "gift", "🎁" },
        { "sparkles", "✨" }, { "bulb", "💡" }, { "mega", "📢" }, { "question", "❓" },
    };

    public ChatForm(NetworkClient client, string username, string? lastUserList = null)
    {
        _client = client;
        _username = username;

        Text = $"💬 TCP 聊天室 - {username}";
        Size = new Size(1200, 750);
        MinimumSize = new Size(900, 500);
        WindowState = FormWindowState.Normal;
        StartPosition = FormStartPosition.CenterScreen;
        FormClosing += (s, e) =>
        {
            _client.Send(new Message { Type = MessageType.QUIT });
            _client.Disconnect();
        };

        // ======== 顶部栏 ========
        var topBar = new Panel { Height = 48, BackColor = Color.FromArgb(7, 193, 96), Dock = DockStyle.Top };

        var titleLabel = new Label
        {
            Text = "💬 TCP 聊天室",
            ForeColor = Color.White,
            Font = new Font("微软雅黑", 14, FontStyle.Bold),
            Location = new Point(15, 10),
            AutoSize = true
        };
        topBar.Controls.Add(titleLabel);

        _userMenu = new ContextMenuStrip();
        _userMenu.Font = new Font("微软雅黑", 12);
        _userMenu.ItemClicked += (s, e) =>
        {
            if (e.ClickedItem != null && e.ClickedItem.Text != _username)
                StartPrivateChat(e.ClickedItem.Text);
        };

        _userCountLabel = new Label
        {
            Text = "在线: 0 人",
            ForeColor = Color.White,
            Font = new Font("微软雅黑", 12),
            AutoSize = true,
            Location = new Point(850, 14)
        };
        topBar.Controls.Add(_userCountLabel);

        var viewOnlineBtn = new Button
        {
            Text = "👥 查看在线",
            FlatStyle = FlatStyle.Flat,
            Font = new Font("微软雅黑", 9, FontStyle.Bold),
            ForeColor = Color.FromArgb(7, 193, 96),
            BackColor = Color.White,
            FlatAppearance = { BorderSize = 0 },
            Size = new Size(90, 30),
            Location = new Point(985, 9),
            Cursor = Cursors.Hand
        };
        viewOnlineBtn.Click += (s, e) =>
        {
            if (_userMenu.Items.Count > 0)
                _userMenu.Show(viewOnlineBtn, new Point(0, viewOnlineBtn.Height));
        };
        topBar.Controls.Add(viewOnlineBtn);

        _logoutBtn = new Button
        {
            Text = "退出",
            FlatStyle = FlatStyle.Flat,
            Font = new Font("黑体", 10, FontStyle.Bold),
            ForeColor = Color.FromArgb(7, 193, 96),
            BackColor = Color.White,
            FlatAppearance = { BorderSize = 0 },
            Size = new Size(90, 30),
            Location = new Point(1085, 9),
            Cursor = Cursors.Hand
        };
        _logoutBtn.Click += (s, e) => Close();
        topBar.Controls.Add(_logoutBtn);

        Controls.Add(topBar);

        // ======== 主体区域 ========
        var mainPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(5, 20, 5, 5) };

        _messageBox = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            BackColor = Color.FromArgb(245, 245, 245),
            Font = new Font("黑体", 10),
            BorderStyle = BorderStyle.None,
            DetectUrls = false
        };
        mainPanel.Controls.Add(_messageBox);

        // 输入区（自动增高）
        var bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 80, Padding = new Padding(5) };
        const int inputMinH = 35, inputMaxH = 180;

        _inputBox = new TextBox
        {
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Location = new Point(0, 0),
            Size = new Size(600, inputMinH),
            Font = new Font("微软雅黑", 11),
            BorderStyle = BorderStyle.Fixed3D,
            Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right
        };
        _inputBox.KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Escape)
            {
                _emojiPanel.Visible = false;
            }
            if (e.KeyCode == Keys.Enter && e.Modifiers != Keys.Shift)
            {
                e.SuppressKeyPress = true;
                SendMessage();
            }
        };
        bottomPanel.Controls.Add(_inputBox);

        // 输入框下方的私聊提示
        var hintLabel = new Label
        {
            Text = "💡 /w 用户名 消息 → 私聊　　/emoji 名称 → 发表情",
            ForeColor = Color.Gray,
            Font = new Font("微软雅黑", 8),
            Location = new Point(5, 38),
            AutoSize = true
        };
        bottomPanel.Controls.Add(hintLabel);

        // 自动增高输入框
        _inputBox.TextChanged += (s, e) =>
        {
            int lineCount = _inputBox.GetLineFromCharIndex(_inputBox.TextLength) + 1;
            int newH = Math.Clamp(lineCount * 22, inputMinH, inputMaxH);
            if (newH != _inputBox.Height)
            {
                _inputBox.Height = newH;
                bottomPanel.Height = newH + 30;
            }
            hintLabel.Location = new Point(5, newH + 3);
        };

        _sendBtn = new Button
        {
            Text = "发送",
            Size = new Size(70, 50),
            Location = new Point(605, 0),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            BackColor = Color.White,
            ForeColor = Color.FromArgb(7, 193, 96),
            FlatStyle = FlatStyle.Flat,
            FlatAppearance = { BorderColor = Color.FromArgb(7, 193, 96), BorderSize = 2 },
            Font = new Font("微软雅黑", 12, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _sendBtn.MouseEnter += (s, e) => _sendBtn.BackColor = Color.FromArgb(232, 245, 233);
        _sendBtn.MouseLeave += (s, e) => _sendBtn.BackColor = Color.White;
        _sendBtn.Click += (s, e) => SendMessage();
        bottomPanel.Controls.Add(_sendBtn);

        mainPanel.Controls.Add(bottomPanel);
        Controls.Add(mainPanel);

        // ======== 底栏 ========
        var bottomBar = new Panel { BackColor = Color.FromArgb(232, 245, 233), Dock = DockStyle.Bottom, Height = 28 };

        _statusLabel = new Label
        {
            Text = "✅ 已连接到服务器",
            ForeColor = Color.FromArgb(46, 125, 50),
            Font = new Font("微软雅黑", 9),
            Location = new Point(10, 5),
            AutoSize = true
        };
        bottomBar.Controls.Add(_statusLabel);

        Controls.Add(bottomBar);

        // ======== 心跳 ========
        _heartbeatTimer = new System.Windows.Forms.Timer { Interval = 30000 };
        _heartbeatTimer.Tick += (s, e) =>
        {
            if (_client.IsConnected)
                _client.Send(new Message { Type = MessageType.PING });
        };
        _heartbeatTimer.Start();

        // ======== 表情弹出面板（类似 Win + .）========
        _emojiContainer = new Panel
        {
            Size = new Size(440, 300),
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Visible = false
        };

        var emojiTitle = new Label
        {
            Text = "😀 表情",
            Font = new Font("微软雅黑", 9, FontStyle.Bold),
            ForeColor = Color.FromArgb(80, 80, 80),
            Location = new Point(8, 5),
            AutoSize = true
        };
        _emojiContainer.Controls.Add(emojiTitle);

        // 关闭按钮（右上角）
        var closeBtn = new Button
        {
            Text = "×",
            Size = new Size(20, 20),
            Location = new Point(_emojiContainer.Width - 24, 3),
            FlatStyle = FlatStyle.Flat,
            FlatAppearance = { BorderSize = 0 },
            Font = new Font("微软雅黑", 10, FontStyle.Bold),
            ForeColor = Color.Gray,
            BackColor = Color.White,
            Cursor = Cursors.Hand,
            TabStop = false
        };
        closeBtn.Click += (s, e) => _emojiContainer.Visible = false;
        _emojiContainer.Controls.Add(closeBtn);

        // 分隔线
        var sepLine = new Label
        {
            Width = _emojiContainer.Width - 10,
            Height = 1,
            BackColor = Color.FromArgb(220, 220, 220),
            Location = new Point(5, 26)
        };
        _emojiContainer.Controls.Add(sepLine);

        // 表情网格
        _emojiPanel = new FlowLayoutPanel
        {
            Location = new Point(3, 29),
            Size = new Size(_emojiContainer.Width - 6, _emojiContainer.Height - 33),
            BackColor = Color.White,
            AutoScroll = true
        };
        _emojiContainer.Controls.Add(_emojiPanel);

        Controls.Add(_emojiContainer);
        _emojiContainer.BringToFront();

        // 分组：笑脸与人物
        AddEmojiGroup("😀 笑脸",
            "😀","😃","😄","😁","😆","😅","🤣","😂",
            "🙂","🙃","😊","😇","🥰","😍","🤩","😘",
            "😗","😚","😋","😛","😜","🤪","😝","🤑",
            "🤗","🤭","🫢","🫣","🤫","🤔","🫡","🤐",
            "😐","😑","😶","🫥","😏","😒","🙄","😬",
            "😮","😯","😲","😳","🥺","😢","😭","😤",
            "😠","😡","🤬","😈","👿","💀","☠️","💩",
            "🤡","👹","👺","👻","👽","🤖","😺","😸",
            "😹","😻","😼","😽","🙀","😿","😾");

        // 分组：手势与庆祝
        AddEmojiGroup("👍 手势",
            "👍","👎","👌","✌️","🤞","🤟","🤘","🤙",
            "👋","🤚","🖐️","✋","🫸","🫷","👆","👇",
            "👈","👉","🖕","🤏","👊","✊","🤛","🤜",
            "👏","🙌","🫶","👐","🤲","🤝","🙏","💪",
            "🦵","🦶","👂","👃","🧠","🫀","👁️","👀",
            "👅","👄","🦷","🫦");

        // 分组：心形与符号
        AddEmojiGroup("❤️ 符号",
            "❤️","🧡","💛","💚","💙","💜","🖤","🤍",
            "🤎","💔","❣️","💕","💞","💓","💗","💖",
            "💘","💝","💟","☮️","✝️","☪️","🕉️","☸️",
            "✡️","🔯","🕎","☯️","🆔","🆕","🆖","🆗",
            "🆙","🆒","🆓","㊗️","㊙️","🈺","🈵","🔴",
            "🟠","🟡","🟢","🔵","🟣","🟤","⚫","⚪",
            "✅","❌","❓","❗","‼️","⁉️","➕","➖",
            "➗","♾️","💲","💱","🔁","🔂","▶️","⏩",
            "⏸️","⏹️","⏺️","🔄","🔃","🎦","🔞","♻️",
            "💯","🔥","⭐","🌟","✨","💫","💥","🌈");

        // 分组：物品与庆祝
        AddEmojiGroup("🎉 物品",
            "💡","🔦","🏮","🪔","📖","📕","📗","📘",
            "📙","📚","📓","📔","📒","📃","📜","📄",
            "📰","🗞️","📑","🔖","🏷️","💰","💴","💵",
            "💶","💷","💸","💳","🧾","✉️","📧","📨",
            "📩","📤","📥","📦","📪","📫","📬","📭",
            "📮","🗳️","✏️","✒️","🖊️","🖋️","🖌️","🖍️",
            "📝","📁","📂","🗂️","📅","📆","🗑️","🪜",
            "🔗","⛓️","🧰","🛠️","🔧","🔨","⚒️","🪛",
            "🔩","⚙️","🧲","💣","🧨","🎯","🎯","🎱",
            "🎮","🎰","🎲","♠️","♥️","♦️","♣️","🃏",
            "🎴","🀄","🎭","🎨","🎬","🎤","🎧","🎼",
            "🎵","🎶","🎙️","🎚️","🎛️","📻","🎷","🪗",
            "🎸","🎺","🎻","🥁","🪘","📯","🎉","🎊",
            "🎈","🎁","🎀","🪄","🪅","🎏","🎐","🎓",
            "🎒","📿","💎","🔮","🪷","🪴","🕯️","🪶");

        // 分组：动物与自然
        AddEmojiGroup("🐶 动物",
            "🐶","🐱","🐭","🐹","🐰","🦊","🐻","🐼",
            "🐨","🐸","🦁","🐯","🐮","🐷","🐗","🐵",
            "🐒","🦍","🦧","🐔","🐧","🐦","🐤","🐣",
            "🐥","🦆","🦅","🦉","🦇","🐺","🐗","🐴",
            "🦄","🐝","🪱","🐛","🦋","🐌","🐞","🐜",
            "🪰","🪲","🪳","🦟","🦗","🕷️","🦂","🐢",
            "🐍","🦎","🦖","🦕","🐙","🦑","🦐","🦞",
            "🦀","🐡","🐠","🐟","🐬","🐳","🐋","🦈",
            "🌹","🌸","🌺","🌻","🌷","🌱","🌿","🍀",
            "🌵","🎄","🌲","🌳","🌴","🌾","🍁","🍂",
            "🍃","🌍","🌎","🌏","🌋","🏔️","⛰️","🏝️");

        // 分组：食物与饮料
        AddEmojiGroup("🍔 食物",
            "🍇","🍈","🍉","🍊","🍋","🍌","🍍","🥭",
            "🍎","🍏","🍐","🍑","🍒","🍓","🫐","🥝",
            "🍅","🫒","🥥","🥑","🍆","🥔","🥕","🌽",
            "🌶️","🫑","🥒","🥬","🧄","🧅","🍄","🥜",
            "🌰","🍞","🥐","🥖","🫓","🧇","🥞","🧀",
            "🍖","🍗","🥩","🥓","🍔","🍟","🍕","🌭",
            "🥪","🌮","🌯","🫔","🥙","🧆","🥚","🍳",
            "🥘","🍲","🫕","🥣","🥗","🍿","🧈","🧂",
            "🥫","🍱","🍘","🍙","🍚","🍛","🍜","🍝",
            "🍠","🍢","🍣","🍤","🍥","🥮","🍡","🥟",
            "🦪","🍦","🍧","🍨","🍩","🍪","🎂","🍰",
            "🧁","🥧","🍫","🍬","🍭","🍮","🍯","🍼",
            "🥛","☕","🫖","🍵","🍶","🍾","🍷","🍸",
            "🍹","🍺","🍻","🥂","🥃","🫙","🥤","🧃",
            "🧊");

        // ======== 回调注册 ========
        _client.OnMessageReceived += OnMessage;
        _client.OnDisconnected += OnDisconnected;

        // 先用 LoginForm 收到的用户列表初始化，没收到则只显示自己
        UpdateUserList(lastUserList ?? _username);

        // 窗体显示后再输出加入消息（构造时 RichTextBox 可能未完成初始化）
        this.Shown += (s, e) => AppendCentered($"🎉 {_username} 加入了聊天室", Color.Gray);

        Resize += (s, e) =>
        {
            _userCountLabel.Location = new Point(ClientSize.Width - 350, 14);
            viewOnlineBtn.Location = new Point(ClientSize.Width - 215, 10);
            _logoutBtn.Location = new Point(ClientSize.Width - 115, 10);
        };
    }

    // ==========================================================
    // 发送消息
    // ==========================================================
    private void SendMessage()
    {
        string content = _inputBox.Text.Trim();
        if (string.IsNullOrEmpty(content)) return;

        // /emoji 命令 — 弹出表情面板 或 插入指定表情
        if (content.StartsWith("/emoji"))
        {
            string rest = content[6..].Trim();
            if (string.IsNullOrEmpty(rest))
            {
                // /emoji → 弹出表情面板
                _inputBox.Clear();
                ShowEmojiPicker();
            }
            else
            {
                // /emoji 名称 → 替换命令文字为实际表情
                if (EmojiMap.TryGetValue(rest, out var emoji))
                {
                    int selStart = _inputBox.SelectionStart;
                    string before = _inputBox.Text[..selStart];
                    int cmdStart = before.LastIndexOf("/emoji", StringComparison.Ordinal);
                    if (cmdStart >= 0)
                    {
                        _inputBox.Text = _inputBox.Text.Remove(cmdStart, selStart - cmdStart);
                        _inputBox.Text = _inputBox.Text.Insert(cmdStart, emoji);
                        _inputBox.SelectionStart = cmdStart + emoji.Length;
                    }
                    else
                    {
                        _inputBox.Text = _inputBox.Text.Insert(selStart, emoji);
                        _inputBox.SelectionStart = selStart + emoji.Length;
                    }
                }
                else
                {
                    AppendCentered($"⚠️ 未知表情: {rest}", Color.Gray);
                }
            }
            _inputBox.Focus();
            return;
        }

        // 替换 :name: 短码为实际 Emoji
        foreach (var kv in EmojiMap)
            content = content.Replace($":{kv.Key}:", kv.Value);

        if (content.StartsWith("/w ") && content.Length > 3)
        {
            // 私聊命令: /w 用户名 消息
            string rest = content[3..].Trim();
            int idx = rest.IndexOf(' ');
            if (idx > 0)
            {
                string target = rest[..idx];
                string msg = rest[(idx + 1)..].Trim();
                if (!string.IsNullOrEmpty(msg))
                {
                    _client.Send(new Message { Type = MessageType.PRIVATE, Args = { target, msg } });
                }
            }
            else
                AppendCentered($"💡 私聊格式: /w 用户名 消息", Color.Gray);
        }
        else
        {
            // 群聊
            _client.Send(new Message { Type = MessageType.MSG, Args = { content } });
        }

        _inputBox.Clear();
        _inputBox.Focus();
    }

    // ==========================================================
    // 弹出表情面板（在输入框上方）
    // ==========================================================
    private void ShowEmojiPicker()
    {
        // 定位在输入框正上方（用屏幕坐标换算，不受布局嵌套影响）
        Point inputScreen = _inputBox.PointToScreen(Point.Empty);
        Point formScreen = PointToScreen(Point.Empty);

        int panelX = 5;
        int panelY = inputScreen.Y - formScreen.Y - _emojiContainer.Height - 5;
        if (panelY < 30) panelY = 30; // 防止超出顶部

        _emojiContainer.Location = new Point(panelX, panelY);
        _emojiContainer.Visible = !_emojiContainer.Visible;
        if (_emojiContainer.Visible)
            _emojiContainer.Focus();
    }

    // ==========================================================
    // 添加一组表情到面板（含分类标题）
    // ==========================================================
    private void AddEmojiGroup(string groupName, params string[] emojis)
    {
        // 分类标题
        var groupLabel = new Label
        {
            Text = groupName,
            Font = new Font("微软雅黑", 8, FontStyle.Bold),
            ForeColor = Color.FromArgb(120, 120, 120),
            AutoSize = true,
            Margin = new Padding(4, 6, _emojiPanel.Width, 2),
            BackColor = Color.Transparent
        };
        _emojiPanel.Controls.Add(groupLabel);

        foreach (var emoji in emojis)
        {
            var btn = new Button
            {
                Text = emoji,
                Size = new Size(40, 40),
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                Font = new Font("Segoe UI Emoji", 18),
                BackColor = Color.White,
                Cursor = Cursors.Hand,
                Margin = new Padding(1)
            };
            string capturedEmoji = emoji;
            btn.Click += (s, e) =>
            {
                int start = _inputBox.SelectionStart;
                _inputBox.Text = _inputBox.Text.Insert(start, capturedEmoji);
                _inputBox.SelectionStart = start + capturedEmoji.Length;
                _inputBox.Focus();
                _emojiContainer.Visible = false;
            };
            btn.MouseEnter += (s, e) => btn.BackColor = Color.FromArgb(232, 245, 233);
            btn.MouseLeave += (s, e) => btn.BackColor = Color.White;
            _emojiPanel.Controls.Add(btn);
        }
    }

    // ==========================================================
    // 双击用户 → 私聊
    // ==========================================================
    // ==========================================================
    // 收到消息
    // ==========================================================
    private void OnMessage(Message msg)
    {
        this.Invoke(() =>
        {
            switch (msg.Type)
            {
                case MessageType.MSG:
                    if (msg.Args.Count >= 2)
                    {
                        bool isMe = msg.Args[0] == _username;
                        AppendBubble(msg.Args[0], msg.Args[1], isMe);
                    }
                    break;

                case MessageType.PRIVATE:
                    if (msg.Args.Count >= 2)
                    {
                        if (msg.Args[0].StartsWith("[私聊"))
                            AppendCentered($"💌 我 → {msg.Args[0].Substring(5).TrimEnd(']')}: {msg.Args[1]}", Color.FromArgb(7, 193, 96));
                        else
                            AppendCentered($"💌 {msg.Args[0]} 私信你: {msg.Args[1]}", Color.Gray);
                    }
                    break;

                case MessageType.JOINED:
                    if (msg.Args.Count > 0)
                        AppendCentered($"🎉 {msg.Args[0]} 加入了聊天室", Color.Gray);
                    break;

                case MessageType.LEFT:
                    if (msg.Args.Count > 0)
                        AppendCentered($"👋 {msg.Args[0]} 离开了聊天室", Color.Gray);
                    break;

                case MessageType.USERS:
                    UpdateUserList(msg.Args.Count > 0 ? msg.Args[0] : "");
                    break;

                case MessageType.ERR:
                    if (msg.Args.Count > 0)
                        AppendCentered($"⚠️ {msg.Args[0]}", Color.FromArgb(198, 40, 40));
                    break;
            }
        });
    }

    // ==========================================================
    // 断开连接
    // ==========================================================
    private void OnDisconnected()
    {
        this.Invoke(() =>
        {
            _statusLabel.Text = "❌ 连接已断开";
            _statusLabel.BackColor = Color.FromArgb(255, 235, 238);
            _statusLabel.ForeColor = Color.FromArgb(198, 40, 40);
            _sendBtn.Enabled = false;
            _inputBox.Enabled = false;
            _logoutBtn.Text = "已断开";
            _heartbeatTimer.Stop();
        });
    }

    // ==========================================================
    // 消息显示
    // ==========================================================
    private void AppendBubble(string sender, string content, bool isMe, Color? contentColor = null)
    {
        string time = DateTime.Now.ToString("HH:mm:ss");
        Color userColor = isMe ? Color.FromArgb(7, 193, 96) : Color.FromArgb(51, 51, 51);

        string line = $"{sender}  {time}\n{content}\n\n";
        int start = _messageBox.TextLength;
        _messageBox.AppendText(line);

        // 用户名：绿色/深色
        _messageBox.Select(start, sender.Length);
        _messageBox.SelectionColor = userColor;
        _messageBox.SelectionFont = new Font("黑体", 14, FontStyle.Bold);

        // 时间：灰色
        int timeStart = start + sender.Length + 2;
        _messageBox.Select(timeStart, time.Length);
        _messageBox.SelectionColor = Color.Gray;
        _messageBox.SelectionFont = new Font("微软雅黑", 10);

        // 内容
        int contentStart = timeStart + time.Length + 1;
        _messageBox.Select(contentStart, content.Length);
        _messageBox.SelectionColor = contentColor ?? Color.FromArgb(51, 51, 51);
        _messageBox.SelectionFont = new Font("微软雅黑", 12);

        _messageBox.SelectionStart = _messageBox.TextLength;
        _messageBox.ScrollToCaret();
    }

    private void AppendColored(string text, Color foreColor, Color backColor)
    {
        int start = _messageBox.TextLength;
        _messageBox.AppendText(text);
        _messageBox.Select(start, text.Length);
        _messageBox.SelectionColor = foreColor;
        _messageBox.SelectionBackColor = backColor;
        _messageBox.SelectionStart = _messageBox.TextLength;
        _messageBox.ScrollToCaret();
    }

    private void AppendSystem(string text)
    {
        int start = _messageBox.TextLength;
        string line = $"— {DateTime.Now:HH:mm:ss} {text} —\n\n";
        // 居中显示：前面加空格
        int pad = Math.Max(0, (60 - line.Length) / 2);
        if (pad > 0) line = new string(' ', pad) + line;
        _messageBox.AppendText(line);
        _messageBox.Select(start, line.Length);
        _messageBox.SelectionColor = Color.Gray;
        _messageBox.SelectionFont = new Font("黑体", 10);
        _messageBox.SelectionStart = _messageBox.TextLength;
        try { _messageBox.ScrollToCaret(); } catch { }
    }

    // ==========================================================
    // 居中单行提示（系统消息、私信、错误）
    // ==========================================================
    private void AppendCentered(string text, Color color)
    {
        int start = _messageBox.TextLength;
        string time = DateTime.Now.ToString("HH:mm:ss");
        string line = $"{text}  {time}\n";
        _messageBox.AppendText(line);
        _messageBox.Select(start, line.Length);
        _messageBox.SelectionColor = color;
        _messageBox.SelectionFont = new Font("微软雅黑", 10);
        _messageBox.SelectionStart = _messageBox.TextLength;
        try { _messageBox.ScrollToCaret(); } catch { }
    }

    // ==========================================================
    // 更新用户列表
    // ==========================================================
    private void UpdateUserList(string userData)
    {
        _userMenu.Items.Clear();
        if (!string.IsNullOrEmpty(userData))
        {
            var users = userData.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (var user in users)
            {
                var item = _userMenu.Items.Add(user, null, (s, e) => StartPrivateChat(user));
                if (user == _username)
                    item.ForeColor = Color.FromArgb(7, 193, 96);
            }
        }
        _userCountLabel.Text = $"在线: {_userMenu.Items.Count} 人";
    }

    private void StartPrivateChat(string target)
    {
        if (target == _username) return;
        var dialog = new Form
        {
            Text = $"💬 私聊 {target}",
            Size = new Size(400, 150),
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            MaximizeBox = false,
            MinimizeBox = false
        };

        var tb = new TextBox { Location = new Point(12, 20), Size = new Size(360, 30), Font = new Font("微软雅黑", 11) };
        var btn = new Button
        {
            Text = "发送", Location = new Point(300, 60), Size = new Size(70, 30),
            BackColor = Color.FromArgb(7, 193, 96), ForeColor = Color.White, FlatStyle = FlatStyle.Flat
        };
        btn.Click += (s, e) =>
        {
            if (!string.IsNullOrEmpty(tb.Text))
            {
                _client.Send(new Message { Type = MessageType.PRIVATE, Args = { target, tb.Text } });
                dialog.Close();
            }
        };
        tb.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) btn.PerformClick(); };

        dialog.Controls.Add(tb);
        dialog.Controls.Add(btn);
        dialog.ShowDialog(this);
    }
}
