
namespace ChatClient;

static class Program
{
    [STAThread]
    static void Main()
    {
        // 全局异常处理
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (s, e) =>
        {
            string msg = $"UI 线程错误: {e.Exception}";
            File.AppendAllText("crash.log", $"[{DateTime.Now}] {msg}\n");
            MessageBox.Show(msg, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        };
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            string msg = $"严重错误: {e.ExceptionObject}";
            File.AppendAllText("crash.log", $"[{DateTime.Now}] {msg}\n");
            MessageBox.Show(msg, "严重错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        };

        ApplicationConfiguration.Initialize();

        var client = new NetworkClient();

        try
        {
            using var login = new LoginForm(client);
            if (login.ShowDialog() != DialogResult.OK)
                return;

            var chat = new ChatForm(client, client.Username!, client.LastUserList);
            Application.Run(chat);
        }
        catch (Exception ex)
        {
            string msg = $"程序崩溃: {ex}";
            File.AppendAllText("crash.log", $"[{DateTime.Now}] {msg}\n");
            MessageBox.Show(msg, "崩溃", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
