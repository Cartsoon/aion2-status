// AION2 Status — открытая выдержка из src/Hotkeys.cs (2026-10-20)
// Source-available excerpt for security review. Full source is closed. See ../LICENSE.
//
// Горячие клавиши: RegisterHotKey — Windows сообщает плагину только о назначенных сочетаниях.
// Никаких хуков клавиатуры, ничего не перехватывается и не отправляется в игру.
// Hotkeys: RegisterHotKey only — no keyboard hooks, nothing is captured or sent to the game.
// L.T(...) — перевод строки интерфейса на английский / UI string translation.

static void Register()
{
    Unregister();
    busy.Clear();
    if (sink == null || sink.Handle == IntPtr.Zero) return;
    var d = Parse(spec);
    var seen = new HashSet<string>();
    for (int i = 0; i < Ids.Length; i++)
    {
        string keys = d[Ids[i]];
        uint mods, vk;
        if (keys.Length == 0 || !seen.Add(keys) || !TryParse(keys, out mods, out vk)) continue;   // повтор сочетания — работает первое по списку
        if (RegisterHotKey(sink.Handle, i + 1, mods | MOD_NOREPEAT, vk)) registered.Add(i + 1);
        else busy.Add(Ids[i]);
    }
}

// ……

static void OnMessage(int n)
{
    int i = n - 1;
    if (i < 0 || i >= Ids.Length || suspended || handler == null) return;
    string id = Ids[i];
    // действие — после выхода из оконной процедуры (окна, меню и т. п. не открываются внутри WM_HOTKEY)
    if (ui != null) ui.Post(o => Fire(id), null);
    else Fire(id);
}
