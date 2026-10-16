# Что и где хранится · What is stored where

Плагин не ставится в систему и пишет только в профиль текущего пользователя. Удалить всё — три шага ниже.
The plugin is portable and writes only to the current user's profile. Full removal — three steps below.

| Где · Where | Что · What |
|---|---|
| Папка с `AION2-Status.exe` · the exe folder | Только сам файл. Исключение — установка обновления по кнопке «Обновить»: на время появляются `AION2-Status.exe.new` (скачанный файл) и `AION2-Status.exe.old` (прежняя версия, удаляется при следующем запуске) · just the file. Exception — installing an update on your "Update" click: `AION2-Status.exe.new` (download) and `AION2-Status.exe.old` (previous version, deleted on next start) appear for a while |
| `HKCU\Software\Aion2Status` | Настройки (то же, что в окне ⚙): режим, позиции окон, фракция, сервер, напоминания, язык, скрытые события (`Dismissed`, `HiddenForever`), проверка обновлений, ключ Премиума · settings (same as the ⚙ window): mode, window positions, faction, server, reminders, language, hidden events, update check, Premium key |
| `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` → `AION2 Status` | Только если включён автозапуск (меню в трее → «Запускать вместе с Windows») · only when autostart is on (tray menu) |
| `%APPDATA%\Aion2Status\timers.json` | Копия расписания событий shugo.gg — работает без интернета · cached event schedule |
| `%APPDATA%\Aion2Status\marks.txt` | Твои метки на карте (Премиум) · your map marks (Premium) |
| `%APPDATA%\Aion2Status\focus.log` | Какое окно и когда забрало передний план у игры (до 256 КБ) — помогает найти, кто «выкидывает» из игры · which window took the foreground from the game (≤ 256 KB) |
| `%APPDATA%\Aion2Status\errors.log` | Текст ошибки, если плагин упал · crash text, if any |

Плагин **читает**, но не меняет · The plugin **reads** but never changes:
- `%LOCALAPPDATA%\NCSOFT\NccrData\com.ncsoft.aion2global\*.breadcrumb.log` — журнал игры · the game's log;
- `HKLM\SOFTWARE\Microsoft\Cryptography\MachineGuid` — для кода устройства (хэш) · for the device code (hashed).

## Удалить полностью · Uninstall

1. Выключи автозапуск и закрой плагин — оба пункта в меню значка в трее · turn off autostart and quit, both in the tray icon menu.
2. Удали папку с `AION2-Status.exe` и `%APPDATA%\Aion2Status` · delete the exe folder and `%APPDATA%\Aion2Status`.
3. Удали ключ реестра `HKCU\Software\Aion2Status` · delete the registry key `HKCU\Software\Aion2Status`.
