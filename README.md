<p align="center"><img src="assets/hero.jpg" alt="AION2 Status — free overlay addon for AION 2: boss timers, map with teleport routes, server status" width="900"></p>

<p align="center">
<b>English</b> · <a href="README.ru.md">Русский</a>
</p>

<p align="center">
<a href="../../releases/latest"><img alt="Latest release" src="https://img.shields.io/github/v/release/Cartsoon/aion2-status?label=download&color=c9993f"></a>
<a href="../../releases"><img alt="Downloads" src="https://img.shields.io/github/downloads/Cartsoon/aion2-status/total?color=8a6cd1"></a>
<img alt="Windows 10/11" src="https://img.shields.io/badge/Windows-10%20%7C%2011-2b6cb0">
<img alt="English and Russian" src="https://img.shields.io/badge/UI-English%20%7C%20Russian-555">
<img alt="Free" src="https://img.shields.io/badge/price-free-2e8b57">
</p>

# AION2 Status — free AION 2 overlay: boss timers, map with routes, server status

**AION2 Status** is a free **overlay addon for AION 2** (Global / EU, Steam). Live **field boss timers**, an
**interactive map with the route from the nearest teleport**, **Abyss bosses**, rifts and invasions, sound
reminders, server status and ping — all drawn on top of the game window.
**Not a bot and not a cheat:** it never touches the game process, its memory or its network packets.

**[⬇ Download the latest version](../../releases/latest)** · [Website](https://aion2status.com/en/) ·
[Telegram bot](https://t.me/Aion2Status_Bot) · [Premium on FunPay](https://funpay.com/lots/offer?id=79758695)

## Four themes for the in-game panel

<p align="center"><img src="assets/themes.webp" alt="AION 2 overlay panel themes: Classic, Aion, Anime, Angel" width="900"></p>

Classic, **Aion** (gold), **Anime** and **Angel** — pick one in ⚙ Settings → View → Theme. Soft feathers drift
around the anime themes. **Abyss** rows (Kaira, Nahma, siege bosses, Argo) get their own crimson-violet background.
Drag the panel by its header, resize it by any edge — 3 to 10 events.

## A kitten tells you when your boss is up

<p align="center"><img src="assets/cat.gif" alt="AION 2 boss reminder: a kitten pops out of the overlay frame with a small bubble" width="560"></p>

In the anime themes a kitten slides out of the frame with a small bubble — "Argo in 5 min · 15:30" — when a boss
you follow with the 🔔 is about to spawn, then hides again. No big pop-ups over the game.

## Top bar — the next boss above the game

<p align="center"><img src="assets/topbars.webp" alt="AION 2 boss timer bar on top of the screen in four themes" width="820"></p>

The next boss in large type above the game, blinking in the last 30 seconds, TP number, what comes next.
Four themes: Ethereal, Aion, Anime, Classic (Premium).

## Interactive AION 2 map with teleport routes

<p align="center"><img src="assets/map-en.webp" alt="AION 2 interactive map: boss, nearest teleport and route" width="760"></p>

Hover a row — a small map pops up next to the panel: where the boss is, the nearest teleport (TP #) and the route
from it, for both factions and the Abyss. Click — the big map with filters, search and your own marks.

## Features

**Free**
- **Your server's status** — maintenance, starting up, open; ping from your PC to the server and back, AION 2 players online on Steam.
- **Timers** — field bosses from a live player feed, rifts, invasions, Kaira and Nahma, hourly events, daily and weekly resets.
- **Reminders** — sound and a card (or the kitten) N minutes and 30 seconds before the start.
- **Map on hover** — where the boss is, the nearest teleport and the route from it; the Abyss for both factions.
- **To chat** — copies a line like "Lagta in 12 min, TP #57"; you paste it into the game chat yourself.
- **Modes** — main window, compact in-game panel with four themes, a single button.

**Premium** — **from 1 day to 12 months**: [FunPay](https://funpay.com/lots/offer?id=79758695) (activation code arrives
instantly) or 150 ⭐ in the [Telegram bot](https://t.me/Aion2Status_Bot)
- **Top bar** — the next boss above the game, four themes.
- **Map search** in English and Russian — bosses, teleports, gathering, cubes, traces.
- **Custom marks** with icons and "route here", map layers: named monsters, dungeons, ore, gems, herbs, merchants.

Activate: ⚙ Settings → Premium → **"Enter code or key…"** — paste the `A2-…` code, the key binds to your PC automatically.

## Safety

In short: **the plugin is a normal window drawn over the game.** It never touches the game process or files,
never reads game memory, never presses anything for you and installs no keyboard hooks. No admin rights.

| The plugin does | The plugin does **not** |
|---|---|
| Draws its own window over the game (no-activate style) | Inject into AION2, read or write its memory |
| Reads the last line of the game's `*.breadcrumb.log` to see if you are in the world | Modify game files or touch NCGuard |
| Runs `netstat` to see which server the game is connected to | Send keys or clicks to the game (no SendInput, no macros) |
| Opens one empty TCP connection to the game server every 6–30 s | Install global keyboard or mouse hooks |
| Downloads public feeds: Steam, plaync, shugo.gg, aion2timers | Collect telemetry or send any data about you |

Details: **[SECURITY.md](SECURITY.md)** · every address and how often — **[docs/network.md](docs/network.md)** ·
what is stored where — **[docs/files.md](docs/files.md)** · how to check the file — **[docs/verify.md](docs/verify.md)**.

## Open code

This repository holds **part of the source code** — exactly the parts that show what the plugin does with the
network, the game and your PC:

| File | What's inside |
|---|---|
| [src/Network.cs](src/Network.cs) | Server probes (one empty TCP connect) and HTTP requests to public feeds |
| [src/Ping.cs](src/Ping.cs) | Ping: ICMP, or HTTP round-trip through the tunnel behind a TUN VPN |
| [src/GameDetect.cs](src/GameDetect.cs) | How the plugin learns your server: `netstat` + game log (read-only) |
| [src/FocusGuard.cs](src/FocusGuard.cs) | Why the game never loses the keyboard; focus change log |
| [src/OverlayWindow.cs](src/OverlayWindow.cs) | Window styles: no activation on click, click-through when pinned |
| [src/DeviceCode.cs](src/DeviceCode.cs) | Device code for the key (a hash, not the MachineGuid itself) |
| [src/Update.cs](src/Update.cs) | Updates: signature and SHA-256 checked before install, https and allowed hosts only |
| [src/Hotkeys.cs](src/Hotkeys.cs) | Hotkeys via RegisterHotKey — no keyboard hooks |
| [src/app.manifest](src/app.manifest) | `asInvoker` — no admin rights |

The rest (UI, map, schedule, keys) stays closed so nobody re-publishes the plugin under another name.
Excerpts are refreshed with every release. License: [LICENSE](LICENSE) — you may read and audit, not copy or redistribute.

## Install

1. Download `AION2-Status.zip` from [Releases](../../releases/latest) and unzip it anywhere.
2. Run `AION2-Status.exe`. If Windows shows a blue SmartScreen window — "More info" → "Run anyway".
3. Pick your faction. In the game, set the display mode to "Borderless windowed".

The interface follows your Windows language (English / Russian); switch it in ⚙ → «Язык / Language».
A short guide is inside the zip (`How to use.txt`).

## FAQ

**Can I get banned for it?** AION2 Status is a normal window drawn over the game: no injection, no memory reading,
no packet sniffing, no keys or clicks sent to the game — see [Safety](#safety) and the open code below.

**Which servers and languages?** AION 2 Global / EU (Steam), both factions. Interface in English and Russian.

**Is it free?** Yes: timers, reminders, server status, the map on hover, chat lines and all four panel themes.
Premium adds the top bar, map search, custom marks and extra map layers.

**How do I activate a FunPay code?** ⚙ Settings → Premium → "Enter code or key…", paste `A2-XXXX-XXXX-XXXX`.

## Questions and bugs

- Bug or idea — [Issues](../../issues).
- Vulnerability — not in Issues, privately: see [SECURITY.md](SECURITY.md).
- Support and Premium — [@Aion2Status_Bot](https://t.me/Aion2Status_Bot).

---

<sub>AION2 Status is a fan project, not affiliated with NCSOFT. AION is a trademark of NCSOFT Corporation.</sub>
