# Безопасность · Security

<a href="#english">English below</a>

## Как плагин работает с игрой

AION 2 защищена античитом NCGuard. Плагин написан так, чтобы с точки зрения игры его просто не было:

- **Не трогает процесс игры.** Нет `OpenProcess`, `ReadProcessMemory`, `WriteProcessMemory`, внедрения DLL, оверлея через DirectX/хуки рендера. Окно плагина — обычное окно Windows, нарисованное поверх (`WS_EX_LAYERED`).
- **Ничего не нажимает.** Нет `SendInput`, `keybd_event`, `mouse_event`, `PostMessage` в окно игры, нет макросов и автокликов. Кнопка «В чат» только копирует текст в буфер обмена — вставляешь его сам.
- **Не перехватывает ввод.** Нет `SetWindowsHookEx` — ни клавиатурных, ни мышиных хуков. Плагин в момент своего же действия смотрит, зажаты ли Alt / Ctrl / Shift или кнопка мыши (`Control.ModifierKeys` / `Control.MouseButtons`, то есть `GetKeyState` — только в своём окне; `GetAsyncKeyState` не используется), например для «Shift + колесо — прозрачность». Нажатия он не записывает и не хранит.
- **Горячие клавиши** — `RegisterHotKey` (не хук), только назначенные сочетания.
- **Не забирает фокус.** Окна поверх игры (игровая панель, мини-полоса, значок, карта, окно настроек ⚙, всплывашки, «Что нового») — `WS_EX_NOACTIVATE`, клик отвечает `MA_NOACTIVATE`. После закрытия меню передний план возвращается игре. Клавиатуру берут только окна, где ты сам печатаешь текст (поиск по карте, имя метки), — после закрытия они отдают передний план обратно игре. Основное окно и окно «Карта Премиум» (ввод ключа) — обычные окна, открываются только по твоему клику. Код — [src/FocusGuard.cs](src/FocusGuard.cs), [src/OverlayWindow.cs](src/OverlayWindow.cs).
- **Читает только две вещи:**
  - последнюю строку журнала `%LOCALAPPDATA%\NCSOFT\NccrData\com.ncsoft.aion2global\*.breadcrumb.log` — чтобы понять, в мире ли ты (файл открывается только на чтение);
  - вывод `netstat -ano -p tcp` — к какому серверу подключён процесс `AION2`.

  Код — [src/GameDetect.cs](src/GameDetect.cs).
- **Без прав администратора.** Манифест `asInvoker` — [src/app.manifest](src/app.manifest).

## Сеть

Плагин не шлёт данные о тебе. Все запросы — либо пустое TCP-подключение к серверу игры (без единого байта данных), либо `GET` публичных лент. Полный список адресов и частота — [docs/network.md](docs/network.md).

Телеметрии и аналитики нет. Плагин не шлёт данные о тебе. Единственное, что он может скачать, — подписанное обновление, и только когда ты сам нажмёшь «Обновить» (см. ниже).

## Обновления

Плагин раз в 12 часов (первый раз — через 40 секунд после запуска) проверяет подписанный манифест `update.json` на сайте `aion2status.com`, а запасным путём — в релизе на GitHub. Проверку можно выключить: ⚙ → «Обновления» → «Проверять автоматически» (поле `UpdateCheck`). Проверить сразу — кнопка там же, пункт трея «Версия … · проверить обновления» или «Версия 0.5» в меню игровой панели. Запросы — с тайм-аутом 15 с.

Что защищает установку:

- **Подпись.** Манифест подписан ECDSA P-256 тем же ключом, что и «Карта Премиума», но над строкой с доменным префиксом `AION2UPDATE|…` — поэтому подпись лицензии и подпись обновления нельзя подменить одну другой. Без верной подписи ничего не скачивается.
- **Только https и только известные хосты.** Новый `.exe` берётся исключительно с `github.com`, `objects.githubusercontent.com`, `release-assets.githubusercontent.com` или `aion2status.com`. Любой другой адрес или `http://` отклоняется.
- **Проверка файла.** Скачанный `.exe` принимается, только если его SHA-256 совпадает с указанным в подписанном манифесте; иначе файл удаляется. Ничего, кроме этого проверенного файла, плагин не запускает.
- **По твоему решению.** Найдя обновление, плагин показывает плашку «⬆ версия», пункт меню и подсказку, но ставит его только после твоего клика «Обновить».
- **Откат.** Файл скачивается рядом как `….exe.new`, после проверки SHA-256 меняется местами с текущим (прежний остаётся как `….exe.old` и удаляется при следующем запуске). Если новая версия не запустилась, прежний exe возвращается на место. Если папка закрыта для записи, плагин ничего не меняет и открывает страницу скачивания.

## Ключ Премиума

Ключ — строка с цифровой подписью (ECDSA P-256), проверяется прямо в плагине, без интернета. Он привязан к коду устройства: 8 байт SHA-256 от `MachineGuid` Windows — сам GUID никто не видит ([src/DeviceCode.cs](src/DeviceCode.cs)). Код устройства уходит боту, только когда ты сам нажимаешь «Купить».

## Файл

Релизный `.exe` — сборка .NET Framework 4, защищена ConfuserEx от копирования: переименование, запутывание логики, шифрование строк и ресурсов. Упаковщика, anti-debug и anti-tamper нет — файл не распаковывает и не меняет сам себя, антивирусы проверяют его как обычную программу. Хэши SHA-256 каждого релиза — в описании релиза и в [CHECKSUMS.txt](CHECKSUMS.txt). Как проверить — [docs/verify.md](docs/verify.md).

## Нашёл уязвимость?

Не пиши в Issues — напиши лично боту [@Aion2Status_Bot](https://t.me/Aion2Status_Bot): `/support security: суть проблемы`. Отвечаем в течение 3 дней. Исправление выходит отдельным релизом, автора благодарим в описании (если хочешь).

---

<a id="english"></a>

## How the plugin treats the game

AION 2 is protected by the NCGuard anti-cheat. The plugin is built so that, from the game's point of view, it simply isn't there:

- **Never touches the game process.** No `OpenProcess`, `ReadProcessMemory`, `WriteProcessMemory`, DLL injection or DirectX/render hooks. The plugin's overlay is a normal Windows window drawn on top (`WS_EX_LAYERED`).
- **Never presses anything.** No `SendInput`, `keybd_event`, `mouse_event`, no `PostMessage` to the game window, no macros or auto-clicks. "To chat" only copies text to the clipboard — you paste it yourself.
- **Never intercepts input.** No `SetWindowsHookEx` — no keyboard or mouse hooks. During its own UI actions the plugin checks whether Alt / Ctrl / Shift or a mouse button is held (`Control.ModifierKeys` / `Control.MouseButtons`, i.e. `GetKeyState` — own window only; `GetAsyncKeyState` is not used), e.g. "Shift + wheel = opacity". Nothing is logged or stored.
- **Hotkeys** — `RegisterHotKey` (not a hook), only the combinations you assign.
- **Never steals focus.** Windows over the game (in-game panel, mini bar, badge, map, the ⚙ settings window, toasts, "What's new") use `WS_EX_NOACTIVATE`; clicks return `MA_NOACTIVATE`. After a menu closes, the foreground goes back to the game. Only windows where you type text yourself (map search, marker name) take the keyboard, and they hand the foreground back to the game when closed. The main window and the Premium Map window (key entry) are normal windows that open only on your click. Code: [src/FocusGuard.cs](src/FocusGuard.cs), [src/OverlayWindow.cs](src/OverlayWindow.cs).
- **Reads only two things:**
  - the last line of `%LOCALAPPDATA%\NCSOFT\NccrData\com.ncsoft.aion2global\*.breadcrumb.log`, to tell whether you are in the world (opened read-only);
  - the output of `netstat -ano -p tcp`, to see which server the `AION2` process is connected to.

  Code: [src/GameDetect.cs](src/GameDetect.cs).
- **No admin rights.** Manifest `asInvoker`: [src/app.manifest](src/app.manifest).

## Network

The plugin sends no data about you. Every request is either an empty TCP connect to a game server (zero payload bytes) or a `GET` of a public feed. Full list of addresses and intervals: [docs/network.md](docs/network.md).

No telemetry, no analytics. The plugin sends no data about you. The only thing it can download is a signed update, and only after you click "Update" yourself (see below).

## Updates

Every 12 hours (first check 40 seconds after start) the plugin fetches a signed `update.json` manifest from `aion2status.com`, falling back to the GitHub release. The check can be turned off: ⚙ → "Updates" → "Check automatically" (the `UpdateCheck` field). To check right away use the button there, the tray item "Version … · check for updates" or "Version 0.5" in the in-game panel menu. Requests time out after 15 s.

What protects the install:

- **Signature.** The manifest is signed with ECDSA P-256 using the same key as the Premium key, but over a string with the domain prefix `AION2UPDATE|…` — so a license signature and an update signature can never be swapped. Nothing is downloaded without a valid signature.
- **HTTPS and known hosts only.** The new `.exe` is fetched exclusively from `github.com`, `objects.githubusercontent.com`, `release-assets.githubusercontent.com` or `aion2status.com`. Any other address, or `http://`, is rejected.
- **File check.** The downloaded `.exe` is accepted only if its SHA-256 matches the one in the signed manifest; otherwise it is deleted. The plugin runs nothing but that verified file.
- **Your choice.** When an update is found the plugin shows a "⬆ version" chip, a menu item and a hint, but installs only after you click "Update".
- **Rollback.** The file is downloaded next to the exe as `….exe.new`; after the SHA-256 check it is swapped with the running one (the old file stays as `….exe.old` and is deleted on the next start). If the new version fails to start, the previous exe is put back. If the folder is not writable, nothing is changed and the download page opens instead.

## Premium key

The key is a digitally signed string (ECDSA P-256), verified offline inside the plugin. It is bound to a device code: 8 bytes of SHA-256 of the Windows `MachineGuid` — the GUID itself is never revealed ([src/DeviceCode.cs](src/DeviceCode.cs)). The device code reaches the bot only when you press "Buy" yourself.

## The file

The release `.exe` is a .NET Framework 4 assembly protected against copying with ConfuserEx: renaming, control-flow obfuscation, string and resource encryption. No packer, no anti-debug, no anti-tamper — the file never unpacks or modifies itself, and antivirus engines scan it like any other program. SHA-256 of every release is in the release notes and [CHECKSUMS.txt](CHECKSUMS.txt). How to check: [docs/verify.md](docs/verify.md).

## Found a vulnerability?

Please don't open an Issue — message the bot privately: [@Aion2Status_Bot](https://t.me/Aion2Status_Bot) `/support security: what you found`. We reply within 3 days; the fix ships as a separate release and we credit you in the notes if you like.
