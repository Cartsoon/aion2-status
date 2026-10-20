// AION2 Status — открытая выдержка из src/Compact.cs (2026-10-20)
// Source-available excerpt for security review. Full source is closed. See ../LICENSE.
//
// Фокус игры: окна плагина не забирают клавиатуру; после меню передний план возвращается игре.
// Плагин ничего не нажимает в игре: нет SendInput/keybd_event, нет хуков клавиатуры.
// Game focus: plugin windows never take the keyboard; no SendInput, no keyboard hooks.
// L.T(...) — перевод строки интерфейса на английский / UI string translation.

// Фокус игры: меню панели и значка после закрытия возвращают передний план игре (иначе клавиатура «уходит» к нам),
// а журнал focus.log записывает, какое окно и когда забрало передний план у AION 2 — чтобы найти виновника
static class FocusGuard
{
    [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetWindowText(IntPtr h, System.Text.StringBuilder s, int n);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetClassName(IntPtr h, System.Text.StringBuilder s, int n);

    static IntPtr lastFg;
    static bool lastWasGame;
    static Timer watch;

    static string ProcName(IntPtr h, out uint pid)
    {
        GetWindowThreadProcessId(h, out pid);
        try { return System.Diagnostics.Process.GetProcessById((int)pid).ProcessName; } catch { return "?"; }
    }

    public static bool GameInForeground()
    {
        uint pid;
        var fg = GetForegroundWindow();
        return fg != IntPtr.Zero && string.Equals(ProcName(fg, out pid), "AION2", StringComparison.OrdinalIgnoreCase);
    }

    // окно с вводом текста (поиск по карте) забирает клавиатуру: до открытия запоминаем игру, после закрытия возвращаем ей передний план
    public static IntPtr GameWindow() { return GameInForeground() ? GetForegroundWindow() : IntPtr.Zero; }
    public static void GiveBack(IntPtr game) { if (game != IntPtr.Zero) SetForegroundWindow(game); }

    // меню, открытое поверх игры: после закрытия — передний план обратно игре (если его не забрало наше большое окно)
    public static void Attach(ContextMenuStrip m)
    {
        IntPtr prev = IntPtr.Zero;
        m.Opening += (s, e) => { prev = GameInForeground() ? GetForegroundWindow() : IntPtr.Zero; };
        m.Closed += (s, e) =>
        {
            var p = prev;
            prev = IntPtr.Zero;
            if (p == IntPtr.Zero) return;
            m.BeginInvoke(new Action(() =>
            {
                uint pid;
                var fg = GetForegroundWindow();
                ProcName(fg, out pid);
                bool ours = pid == (uint)System.Diagnostics.Process.GetCurrentProcess().Id;
                bool bigWindow = Application.OpenForms.Cast<Form>().Any(f => f.Visible && f.Handle == fg && f.Text == "AION 2 Status" && f.Width > 200);
                if ((ours || fg == IntPtr.Zero) && !bigWindow) SetForegroundWindow(p);
            }));
        };
    }

    // раз в полсекунды смотрим, у кого передний план; уход с AION 2 пишем в %APPDATA%\Aion2Status\focus.log
    public static void StartLog()
    {
        if (watch != null) return;
        watch = new Timer { Interval = 500 };
        watch.Tick += (s, e) =>
        {
            var fg = GetForegroundWindow();
            if (fg == lastFg) return;
            lastFg = fg;
            uint pid;
            string name = ProcName(fg, out pid);
            bool game = string.Equals(name, "AION2", StringComparison.OrdinalIgnoreCase);
            if (lastWasGame && !game)
            {
                try
                {
                    var t = new System.Text.StringBuilder(160); GetWindowText(fg, t, 160);
                    var c = new System.Text.StringBuilder(96); GetClassName(fg, c, 96);
                    string path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Profile.Name, "focus.log");
                    System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
                    var fi = new System.IO.FileInfo(path);
                    if (fi.Exists && fi.Length > 256 * 1024) fi.Delete();
                    System.IO.File.AppendAllText(path, string.Format("{0:yyyy-MM-dd HH:mm:ss}  игра → {1} (pid {2})  «{3}»  [{4}]\r\n", DateTime.Now, name, pid, t, c), System.Text.Encoding.UTF8);
                }
                catch { }
            }
            lastWasGame = game;
        };
        watch.Start();
    }
}
