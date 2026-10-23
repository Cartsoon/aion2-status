// AION2 Status — открытая выдержка из src/Aion2Status.cs (2026-10-23)
// Source-available excerpt for security review. Full source is closed. See ../LICENSE.
//
// Как плагин узнаёт твой сервер: список TCP-подключений процесса AION2 (netstat) и последняя строка
// журнала игры *.breadcrumb.log — только чтение файла, без доступа к процессу и памяти игры.
// How the plugin learns your server: netstat for the AION2 process + reading the last line of the game's
// breadcrumb log (read-only; the game process and memory are never touched).
// L.T(...) — перевод строки интерфейса на английский / UI string translation.

// Пока игра запущена: какой сервер — «твой» (к нему подключение при входе в мир), какие ещё бывают, в мире ли ты.
void LearnFromRunningGame()
{
    Process[] ps;
    try { ps = Process.GetProcessesByName("AION2"); } catch { return; }
    if (ps.Length == 0) { inGame = false; return; }

    string flow = LastFlowLine();
    inGame = flow != null && (flow.Contains(".Ingame") || flow.Contains("Teleport.") || flow.StartsWith("HUD.") || flow.StartsWith("Dungeon."));
    bool justEntered = flow != null && flow.Contains("SelectCharacterInLobby");

    var pids = new HashSet<string>(ps.Select(p => p.Id.ToString()));
    string output;
    try
    {
        var psi = new ProcessStartInfo("netstat", "-ano -p tcp")
        {
            UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true
        };
        using (var p = Process.Start(psi))
        {
            output = p.StandardOutput.ReadToEnd();
            p.WaitForExit(5000);
        }
    }
    catch { return; }
    bool changed = false;
    foreach (var line in output.Split('\n'))
    {
        var parts = line.Split(new[] { ' ', '\t', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 5 || !pids.Contains(parts[parts.Length - 1]) || parts[3] != "ESTABLISHED") continue;
        string remote = parts[2];
        if (remote.EndsWith(":13700") && remote != cfg.Login) { cfg.Login = remote; changed = true; }
        if (!remote.EndsWith(":13328")) continue;
        if (!cfg.Games.Contains(remote))
        {
            cfg.Games.Add(remote);
            while (cfg.Games.Count > 12) cfg.Games.RemoveAt(0);
            changed = true;
        }
        if (justEntered && remote != cfg.Home) { cfg.Home = remote; changed = true; }
    }
    if (changed) cfg.Save();
}

// ……

// Последняя строка журнала игры (%LOCALAPPDATA%\NCSOFT\NccrData\com.ncsoft.aion2global\*.breadcrumb.log)
string LastFlowLine()
{
    try
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"NCSOFT\NccrData\com.ncsoft.aion2global");
        var f = new DirectoryInfo(dir).GetFiles("*.breadcrumb.log").OrderByDescending(x => x.LastWriteTimeUtc).FirstOrDefault();
        if (f == null) return null;
        lastFlowFile = f.FullName;
        string lastLine = null;
        using (var fs = new FileStream(f.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
        using (var sr = new StreamReader(fs, Encoding.UTF8))
        {
            string l;
            while ((l = sr.ReadLine()) != null)
                if (l.Contains("] [0] ") && !l.Contains("MEM.")) lastLine = l;
        }
        if (lastLine == null) return null;
        int i = lastLine.IndexOf("] [0] ");
        return lastLine.Substring(i + 6).Trim();
    }
    catch { return null; }
}
