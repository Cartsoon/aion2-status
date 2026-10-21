// AION2 Status — открытая выдержка из src/Compact.cs (2026-10-22)
// Source-available excerpt for security review. Full source is closed. See ../LICENSE.
//
// Окно поверх игры: WS_EX_NOACTIVATE + MA_NOACTIVATE (клик не отнимает фокус), WS_EX_TRANSPARENT при закреплении.
// Overlay window styles: no activation on click, click-through when pinned.
// L.T(...) — перевод строки интерфейса на английский / UI string translation.

protected override CreateParams CreateParams
{
    get
    {
        var cp = base.CreateParams;
        cp.ExStyle |= 0x00000080 | 0x08000000;   // WS_EX_TOOLWINDOW (нет в Alt+Tab) | WS_EX_NOACTIVATE (не отнимает фокус у игры)
        cp.ExStyle |= WS_EX_LAYERED;   // рисуем сами через UpdateLayeredWindow: прозрачный фон, чёткий текст
        if (clickThrough) cp.ExStyle |= WS_EX_TRANSPARENT;   // закреплено: мышь проходит насквозь
        return cp;
    }
}

// ……

protected override void WndProc(ref Message m)
{
    if (m.Msg == 0x0021) { m.Result = (IntPtr)3; return; }   // WM_MOUSEACTIVATE → MA_NOACTIVATE: клик по панели не забирает фокус у игры
    base.WndProc(ref m);
}
