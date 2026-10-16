# Как проверить файл · How to verify the file

## 1. Хэш · Checksum

Скачивай только из [Releases](../../../releases/latest) этого репозитория или с сайта [aion2status.com](https://aion2status.com).
Сверь SHA-256 с описанием релиза и [CHECKSUMS.txt](../CHECKSUMS.txt):
Download only from this repository's Releases or the official website, then compare SHA-256:

```powershell
Get-FileHash .\AION2-Status.zip -Algorithm SHA256
Get-FileHash .\AION2-Status.exe -Algorithm SHA256
```

Не совпало — не запускай и напиши нам ([SECURITY.md](../SECURITY.md)).
If it doesn't match, don't run it and tell us.

## 2. Антивирусы · Antivirus

В описании каждого релиза — ссылка на отчёт [VirusTotal](https://www.virustotal.com). Можно загрузить файл туда и самому.
Новый неподписанный `.exe` иногда помечают 1–2 эвристики из 70 — это нормально для небольших программ; «чисто» у Microsoft Defender, Kaspersky, ESET, Dr.Web — главное.
Each release links its VirusTotal report. A fresh unsigned exe may get 1–2 heuristic flags out of ~70; what matters is a clean result from the major engines.

## 3. Поведение · Behaviour

Проверь сам, что плагин делает только то, что написано в [network.md](network.md) и [files.md](files.md):
Check for yourself that the plugin does only what network.md and files.md say:

- **Сеть · Network** — [TCPView](https://learn.microsoft.com/sysinternals/downloads/tcpview) (Sysinternals): только адреса из списка. Only the listed addresses.
- **Файлы и реестр · Files and registry** — [Process Monitor](https://learn.microsoft.com/sysinternals/downloads/procmon), фильтр `Process Name is AION2-Status.exe`, операции `WriteFile` / `RegSetValue`: свои данные плагин пишет только в `HKCU\Software\Aion2Status` и `%APPDATA%\Aion2Status` (плюс служебные записи самой Windows и .NET — кэши шрифтов, иконка в трее). The plugin's own data goes only there (plus Windows/.NET housekeeping).
- **Игра · The game** — в [Process Explorer](https://learn.microsoft.com/sysinternals/downloads/process-explorer) у `AION2.exe` → вкладка DLLs: от плагина там ничего нет, в игру он не внедряется. Nothing from the plugin is loaded into the game.

## Почему SmartScreen показывает синее окно · Why SmartScreen warns

У плагина пока нет платной подписи кода, а SmartScreen доверяет файлам по «репутации» — числу скачиваний. Окно «Windows защитила ваш компьютер» появляется у любого нового неподписанного файла: «Подробнее» → «Выполнить в любом случае».
The plugin has no paid code-signing certificate yet, and SmartScreen trusts files by download reputation. Any new unsigned file gets this window: "More info" → "Run anyway".
