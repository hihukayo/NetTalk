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
    private readonly ListBox _atListBox;
    private readonly Panel _atContainer;
    private readonly string _logPath;

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
        _logPath = Path.Combine(AppContext.BaseDirectory, $"聊天记录_{DateTime.Now:yyyy-MM-dd}.txt");

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

        // 绿色栏底部分隔线
        var separator = new Panel
        {
            Dock = DockStyle.Top, Height = 2, BackColor = Color.FromArgb(5, 160, 80)
        };
        Controls.Add(separator);

        var mainPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(5, 45, 5, 5), BackColor = Color.FromArgb(245, 245, 245) };

        _messageBox = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            BackColor = Color.FromArgb(245, 245, 245),
            Font = new Font("微软雅黑", 10),
            BorderStyle = BorderStyle.None,
            DetectUrls = false
        };
        mainPanel.Controls.Add(_messageBox);

        // 输入区（固定高度，可滚动查看上文）
        var bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 120, Padding = new Padding(5) };
        const int inputH = 75;

        _inputBox = new TextBox
        {
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Location = new Point(0, 0),
            Size = new Size(600, inputH),
            Font = new Font("微软雅黑", 11),
            BorderStyle = BorderStyle.Fixed3D,
            Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right
        };
        _inputBox.KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Escape)
            {
                _emojiContainer.Visible = false;
                _atContainer.Visible = false;
            }
            if (e.KeyCode == Keys.Enter && e.Modifiers != Keys.Shift)
            {
                e.SuppressKeyPress = true;
                SendMessage();
            }
        };
        bottomPanel.Controls.Add(_inputBox);

        var hintLabel = new Label
        {
            Text = "💡 /w 用户名 消息 → 私聊　　/emoji 名称 → 发表情",
            ForeColor = Color.Gray,
            Font = new Font("微软雅黑", 8),
            Location = new Point(5, inputH + 3),
            AutoSize = true
        };
        bottomPanel.Controls.Add(hintLabel);

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

        _heartbeatTimer = new System.Windows.Forms.Timer { Interval = 30000 };
        _heartbeatTimer.Tick += (s, e) =>
        {
            if (_client.IsConnected)
                _client.Send(new Message { Type = MessageType.PING });
        };
        _heartbeatTimer.Start();

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
            Location = new Point(8, 6),
            AutoSize = true
        };
        _emojiContainer.Controls.Add(emojiTitle);

        // 关闭按钮（红色 ✕，右上角）
        var closeBtn = new Button
        {
            Text = "ｘ",
            Size = new Size(18, 30),
            Location = new Point(_emojiContainer.Width - 22, 1),
            FlatStyle = FlatStyle.Flat,
            FlatAppearance = { BorderSize = 0 },
            Font = new Font("微软雅黑", 9, FontStyle.Bold),
            ForeColor = Color.FromArgb(220, 50, 50),
            BackColor = Color.White,
            Cursor = Cursors.Hand,
            TabStop = false,
            Padding = new Padding(0),
            Margin = new Padding(0)
        };
        closeBtn.MouseEnter += (s, e) => closeBtn.BackColor = Color.FromArgb(255, 235, 235);
        closeBtn.MouseLeave += (s, e) => closeBtn.BackColor = Color.White;
        closeBtn.Click += (s, e) => _emojiContainer.Visible = false;
        _emojiContainer.Controls.Add(closeBtn);
        closeBtn.BringToFront();

        // 分隔线
        var sepLine = new Label
        {
            Width = _emojiContainer.Width - 10,
            Height = 1,
            BackColor = Color.FromArgb(220, 220, 220),
            Location = new Point(5, 34)
        };
        _emojiContainer.Controls.Add(sepLine);

        // 表情网格
        _emojiPanel = new FlowLayoutPanel
        {
            Location = new Point(3, 37),
            Size = new Size(_emojiContainer.Width - 6, _emojiContainer.Height - 40),
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


        // ====== @ M-gM-^TM-(M-fM-^HM-7M-eM-<M-9M-eM-^GM-:M-eM-^HM-^WM-hM-!M-( ======
        _atContainer = new Panel
        {
            Size = new Size(180, 220),
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Visible = false
        };

        var atTitle = new Label
        {
            Text = "👥 用户",
            Font = new Font("微软雅黑", 9, FontStyle.Bold),
            ForeColor = Color.FromArgb(80, 80, 80),
            Location = new Point(8, 5),
            AutoSize = true
        };
        _atContainer.Controls.Add(atTitle);

        var atClose = new Button
        {
            Text = "✕",
            Size = new Size(18, 18),
            Location = new Point(_atContainer.Width - 22, 4),
            FlatStyle = FlatStyle.Flat,
            FlatAppearance = { BorderSize = 0 },
            Font = new Font("微软雅黑", 8, FontStyle.Bold),
            ForeColor = Color.FromArgb(200, 40, 40),
            BackColor = Color.White,
            Cursor = Cursors.Hand,
            TabStop = false
        };
        atClose.Click += (s, e) => _atContainer.Visible = false;
        _atContainer.Controls.Add(atClose);

        var atSep = new Label
        {
            Width = _atContainer.Width - 10,
            Height = 1,
            BackColor = Color.FromArgb(220, 220, 220),
            Location = new Point(5, 26)
        };
        _atContainer.Controls.Add(atSep);

        _atListBox = new ListBox
        {
            Location = new Point(5, 29),
            Size = new Size(_atContainer.Width - 10, _atContainer.Height - 33),
            BorderStyle = BorderStyle.None,
            Font = new Font("M-hM-^AM-^EM-^HM-^YM-^HM-^IM-^M-^BM-^AM-^L", 12),
            BackColor = Color.White,
            ForeColor = Color.FromArgb(51, 51, 51)
        };
        _atListBox.Click += (s, e) =>
        {
            if (_atListBox.SelectedItem != null)
                InsertAtUser(_atListBox.SelectedItem.ToString()!);
        };
        _atListBox.DoubleClick += (s, e) =>
        {
            if (_atListBox.SelectedItem != null)
                InsertAtUser(_atListBox.SelectedItem.ToString()!);
        };
        _atListBox.KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Enter && _atListBox.SelectedItem != null)
            {
                InsertAtUser(_atListBox.SelectedItem.ToString()!);
                e.SuppressKeyPress = true;
            }
        };
        _atContainer.Controls.Add(_atListBox);
        Controls.Add(_atContainer);
        _atContainer.BringToFront();

        _inputBox.TextChanged += (s, e) => CheckAtMention();
        _inputBox.LostFocus += (s, e) => _atContainer.Visible = false;

        _client.OnMessageReceived += OnMessage;
        _client.OnDisconnected += OnDisconnected;

        // 先用 LoginForm 收到的用户列表初始化，没收到则只显示自己
        UpdateUserList(lastUserList ?? _username);

        // 窗体显示后再输出加入消息（构造时 RichTextBox 可能未完成初始化）
        this.Shown += (s, e) =>
        {
            AppendCentered($"🎉 {_username} 加入了聊天室", Color.Gray);
            SaveToLog($"🎉 {_username} 加入了聊天室");
        };
        this.Activated += (s, e) => Text = $"💬 TCP 聊天室 - {_username}";

        Resize += (s, e) =>
        {
            _userCountLabel.Location = new Point(ClientSize.Width - 350, 14);
            viewOnlineBtn.Location = new Point(ClientSize.Width - 215, 10);
            _logoutBtn.Location = new Point(ClientSize.Width - 115, 10);
        };
    }

    // 发送消息
    private void SendMessage()
    {
        string rawText = _inputBox.Text;
        string content = rawText.Trim();
        if (string.IsNullOrEmpty(content)) return;

        // /emoji 命令 — 在任意位置输入都生效
        int emojiIndex = content.IndexOf("/emoji");
        if (emojiIndex >= 0)
        {
            string rest = content[(emojiIndex + 6)..].TrimStart();
            if (string.IsNullOrEmpty(rest))
            {
                // /emoji → 保留前面已输入的文字，弹出表情面板
                _inputBox.Text = content[..emojiIndex];
                _inputBox.SelectionStart = _inputBox.Text.Length;
                ShowEmojiPicker();
            }
            else
            {
                // /emoji 名称 → 替换为实际表情
                string name = rest.Split(' ')[0];
                if (EmojiMap.TryGetValue(name, out var emoji))
                {
                    string before = content[..emojiIndex];
                    string after = rest[name.Length..].TrimStart();
                    _inputBox.Text = before + emoji + (after.Length > 0 ? " " + after : "");
                    _inputBox.SelectionStart = _inputBox.Text.Length;
                }
                else
                {
                    AppendCentered($"⚠️ 未知表情: {name}", Color.Gray);
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
                    SaveToLog($"💌 私聊 我 → {target}: {msg}");
                }
            }
            else
                AppendCentered($"💡 私聊格式: /w 用户名 消息", Color.Gray);
        }
        else
        {
            // 群聊
            _client.Send(new Message { Type = MessageType.MSG, Args = { content } });
            SaveToLog($"{_username}: {content}");
        }

        _inputBox.Clear();
        _inputBox.Focus();
    }

    // 弹出表情面板（在输入框上方）
    private void ShowEmojiPicker()
    {
        // 定位在输入框正上方（用屏幕坐标换算，不受布局嵌套影响）
        Point inputScreen = _inputBox.PointToScreen(Point.Empty);
        Point formScreen = PointToScreen(Point.Empty);

        int panelX = 5;
        int panelY = inputScreen.Y - formScreen.Y - _emojiContainer.Height - 5;
        if (panelY < 30) panelY = 30; // 防止超出顶部

        _atContainer.Visible = false; // 关闭 @ 面板
        _emojiContainer.Location = new Point(panelX, panelY);
        _emojiContainer.Visible = !_emojiContainer.Visible;
        if (_emojiContainer.Visible)
            _emojiContainer.Focus();
    }

    // 检测输入框中的 @ 并弹出用户列表
    private void CheckAtMention()
    {
        int pos = _inputBox.SelectionStart;
        string text = _inputBox.Text;

        // 从光标往前找最后一个 @
        int atIdx = -1;
        for (int i = pos - 1; i >= 0; i--)
        {
            if (text[i] == '@')
            {
                // @ 前面必须是空格或开头
                if (i == 0 || text[i - 1] == ' ')
                {
                    atIdx = i;
                    break;
                }
            }
            else if (text[i] == ' ')
                break; // 遇到空格停止
        }

        if (atIdx < 0)
        {
            _atContainer.Visible = false;
            return;
        }

        // 获取 @ 后的过滤文字
        string filter = text[(atIdx + 1)..pos].Trim();
        _atListBox.Items.Clear();

        // 从 _userMenu 获取在线用户列表
        foreach (ToolStripItem item in _userMenu.Items)
        {
            string name = item.Text;
            if (string.IsNullOrEmpty(filter) ||
                name.StartsWith(filter, StringComparison.OrdinalIgnoreCase))
                _atListBox.Items.Add(name);
        }

        if (_atListBox.Items.Count == 0)
        {
            _atContainer.Visible = false;
            return;
        }

        // 定位面板在输入框上方
        Point inputScreen = _inputBox.PointToScreen(new Point(
            _inputBox.GetPositionFromCharIndex(atIdx).X, 0));
        Point formScreen = PointToScreen(Point.Empty);

        int panelX = Math.Max(5, inputScreen.X - formScreen.X);
        int panelY = inputScreen.Y - formScreen.Y - _atContainer.Height - 5;
        if (panelY < 30) panelY = 30;

        _atContainer.Location = new Point(panelX, panelY);
        _atContainer.Visible = true;
        _atContainer.BringToFront();
        _atListBox.SelectedIndex = 0;
    }

    // 插入 @用户 到输入框
    private void InsertAtUser(string user)
    {
        int pos = _inputBox.SelectionStart;
        string text = _inputBox.Text;

        // 找光标前的 @ 位置
        int atIdx = -1;
        for (int i = pos - 1; i >= 0; i--)
        {
            if (text[i] == '@' && (i == 0 || text[i - 1] == ' '))
            {
                atIdx = i;
                break;
            }
            if (text[i] == ' ') break;
        }

        if (atIdx >= 0)
        {
            string before = text[..atIdx];
            string after = text[pos..];
            _inputBox.Text = before + "@" + user + " " + after;
            _inputBox.SelectionStart = atIdx + user.Length + 2;
        }

        _atContainer.Visible = false;
        _inputBox.Focus();
    }

    // 添加一组表情到面板（含分类标题）
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

    // 双击用户 → 私聊
    // 收到消息
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
                        if (!isMe)
                        {
                            SaveToLog($"{msg.Args[0]}: {msg.Args[1]}");
                            // 被 @ 提醒
                            if (msg.Args[1].Contains($"@{_username}", StringComparison.OrdinalIgnoreCase))
                            {
                                Text = $"💬 有人@你 - {_username}";
                                TopMost = true;
                                TopMost = false;
                            }
                        }
                    }
                    break;

                case MessageType.PRIVATE:
                    if (msg.Args.Count >= 2)
                    {
                        if (msg.Args[0].StartsWith("[私聊"))
                        {
                            string target = msg.Args[0].Substring(5).TrimEnd(']');
                            AppendCentered($"💌 我 → {target}: {msg.Args[1]}", Color.FromArgb(7, 193, 96));
                            SaveToLog($"💌 私聊 我 → {target}: {msg.Args[1]}");
                        }
                        else
                        {
                            AppendCentered($"💌 {msg.Args[0]} 私信你: {msg.Args[1]}", Color.Gray);
                            SaveToLog($"💌 私聊 {msg.Args[0]} → 我: {msg.Args[1]}");
                        }
                    }
                    break;

                case MessageType.JOINED:
                    if (msg.Args.Count > 0)
                    {
                        AppendCentered($"🎉 {msg.Args[0]} 加入了聊天室", Color.Gray);
                        SaveToLog($"🎉 {msg.Args[0]} 加入了聊天室");
                    }
                    break;

                case MessageType.LEFT:
                    if (msg.Args.Count > 0)
                    {
                        AppendCentered($"👋 {msg.Args[0]} 离开了聊天室", Color.Gray);
                        SaveToLog($"👋 {msg.Args[0]} 离开了聊天室");
                    }
                    break;

                case MessageType.USERS:
                    UpdateUserList(msg.Args.Count > 0 ? msg.Args[0] : "");
                    break;

                case MessageType.ERR:
                    if (msg.Args.Count > 0)
                    {
                        AppendCentered($"⚠️ {msg.Args[0]}", Color.FromArgb(198, 40, 40));
                        SaveToLog($"⚠️ {msg.Args[0]}");
                    }
                    break;
            }
        });
    }

    // 保存到聊天记录文件
    private void SaveToLog(string text)
    {
        try { File.AppendAllText(_logPath, $"[{DateTime.Now:HH:mm:ss}] {text}\n", System.Text.Encoding.UTF8); }
        catch { }
    }

    // 断开连接
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

    // 消息显示
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

        // 内容（@用户名 显示为蓝色）
        try
        {
            int contentStart = timeStart + time.Length + 1;
            AppendColoredText(content, contentStart, contentColor ?? Color.FromArgb(51, 51, 51));
        }
        catch (Exception ex)
        {
            try { File.AppendAllText("crash.log", $"[{DateTime.Now}] AppendBubble: {ex.Message}\n"); } catch { }
        }

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

    // 渲染内容（识别 @用户名 染成蓝色）
    private void AppendColoredText(string text, int startOffset, Color defaultColor)
    {
        var atColor = Color.FromArgb(30, 120, 220); // 蓝色
        int pos = 0;
        while (pos < text.Length)
        {
            int atIdx = text.IndexOf('@', pos);
            if (atIdx < 0 || atIdx == text.Length - 1)
            {
                // 剩余部分用默认颜色
                _messageBox.Select(startOffset + pos, text.Length - pos);
                _messageBox.SelectionColor = defaultColor;
                _messageBox.SelectionFont = new Font("微软雅黑", 12);
                break;
            }

            // @ 前面的文字
            if (atIdx > pos)
            {
                _messageBox.Select(startOffset + pos, atIdx - pos);
                _messageBox.SelectionColor = defaultColor;
                _messageBox.SelectionFont = new Font("微软雅黑", 12);
            }

            // 找 @用户名 结束位置
            int endIdx = atIdx + 1;
            while (endIdx < text.Length && !char.IsWhiteSpace(text[endIdx]) && text[endIdx] != '/' && text[endIdx] != '，' && text[endIdx] != '。')
                endIdx++;

            // @用户名 染蓝色
            _messageBox.Select(startOffset + atIdx, endIdx - atIdx);
            _messageBox.SelectionColor = atColor;
            _messageBox.SelectionFont = new Font("微软雅黑", 12, FontStyle.Bold);

            pos = endIdx;
        }
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

    // 居中单行提示（系统消息、私信、错误）
    private void AppendCentered(string text, Color color)
    {
        try
        {
            int start = _messageBox.TextLength;
            string time = DateTime.Now.ToString("HH:mm:ss");
            string line = $"{text}  {time}\n";
            _messageBox.AppendText(line);
            _messageBox.Select(start, line.Length);
            _messageBox.SelectionColor = color;
            _messageBox.SelectionStart = _messageBox.TextLength;
            try { _messageBox.ScrollToCaret(); } catch { }
        }
        catch (Exception ex)
        {
            // 防止 Emoji 等字符渲染异常导致消息被吞
            try { File.AppendAllText("crash.log", $"[{DateTime.Now}] AppendCentered: {ex.Message}\n"); } catch { }
        }
    }

    // 更新用户列表
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
