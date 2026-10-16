# Сеть · Network

Все подключения, которые делает плагин. Других нет: нет телеметрии и аналитики. Единственная «служебная» связь с нашими серверами — проверка обновлений (её можно выключить, см. ниже).
Every connection the plugin makes. There are no others: no telemetry or analytics. The only "service" call to our servers is the update check (it can be turned off, see below).

## Серверы игры · Game servers

| Куда · Where | Что · What | Как часто · How often |
|---|---|---|
| Твой игровой сервер, TCP `:13328` (Cloudflare Spectrum) · your game server | Пустое TCP-подключение без данных, держится до 2,5 с · empty TCP connect, no payload, held ≤ 2.5 s | раз в 6 с во время запуска, 15 с при сбоях, 30 с в обычном режиме · every 6 s while starting, 15 s on errors, 30 s normally |
| Ещё один известный сервер игры · one other known game server | То же, по одному за цикл · same, one per cycle | так же · same |
| Сервер входа, TCP `:13700` · login server | Только установка соединения, сразу закрывается · connect only, closed at once | так же · same |
| Повтор мимо VPN · retry outside the VPN | Если через VPN сервер «не пускает», одна проба с адреса физической сети · if the server rejects via VPN, one probe from the physical NIC | только в этом случае · only then |
| `1.1.1.1:443`, `8.8.8.8:443` | Проверка интернета (только connect) · internet check (connect only) | только если сервер не ответил · only when the server didn't answer |

Адреса серверов плагин не знает заранее — берёт их из `netstat` у запущенной игры ([src/GameDetect.cs](../src/GameDetect.cs)).
The plugin learns server addresses from `netstat` of the running game.

## Пинг · Ping

| Куда · Where | Что · What | Как часто · How often |
|---|---|---|
| Твой игровой сервер · your game server | ICMP echo | раз в секунду, пока пинг показан на экране · once a second while ping is on screen |
| `cloudflare.com` (104.16.133.229 / 104.16.132.229), TCP `:80` | Если VPN с TUN отвечает на ICMP сам (≤ 1 мс, TTL ≥ 100): `GET /cdn-cgi/trace` по одному keep-alive соединению · when a TUN VPN answers ICMP locally: `GET /cdn-cgi/trace` over one keep-alive connection | так же; раз в минуту снова пробуем ICMP · same; ICMP retried every minute |

Код · Code: [src/Ping.cs](../src/Ping.cs).

## Публичные ленты (HTTPS GET) · Public feeds

| Адрес · URL | Зачем · Why | Как часто · How often |
|---|---|---|
| `api.steampowered.com/ISteamNews/GetNewsForApp` | Объявления о техработах в Steam · maintenance announcements | раз в 10 мин (2 мин, если сервер не в норме), если plaync не дал окно · every 10 min (2 min when not online), when plaync gives no window |
| `api-global-community.plaync.com/aion2_global/board/notice_en/…` | Официальные объявления · official notices | раз в 10 мин (2 мин, если не в норме) · every 10 min (2 min when not online) |
| `api.steampowered.com/ISteamUserStats/GetNumberOfCurrentPlayers` | Онлайн в Steam · players online | раз в минуту · every minute |
| `api.steampowered.com/ISteamChartsService/GetGamesByConcurrentPlayers` | Пик за сутки · 24h peak | раз в 5 мин · every 5 min |
| `shugo.gg/timers.json` | Расписание событий · event schedule | раз в 6 ч (при ошибке — 20 мин) · every 6 h (20 min after an error) |
| `www.aion2timers.com/api/fieldboss.php?action=feed&server=<сервер>` | Живые таймеры полевых боссов · live field boss timers | раз в 2 мин · every 2 min |
| `aion2.plaync.com/en-us/api/gameinfo/servers?region=<регион>` | Список серверов для выбора · server list | при выборе сервера · when you pick a server |

В запросах только `User-Agent: AION2Status/1.0` (у пинга — `AION2Status-ping`, у проверки обновлений — `AION2Status/<версия>`), без cookies и идентификаторов. Тайм-аут ответа — 20 с (обновления — 15 с, скачивание exe — 30 с).
Окно настроек ⚙ своих подключений не делает: список серверов грузится только когда ты выбираешь сервер, «Проверить обновления» — та же проверка, что ниже.
Requests carry only `User-Agent: AION2Status/1.0` (ping: `AION2Status-ping`, update check: `AION2Status/<version>`) — no cookies, no identifiers. Response timeout is 20 s (updates 15 s, exe download 30 s).
The ⚙ settings window makes no connections of its own: the server list loads only when you pick a server, and "Check for updates" is the same check as below.

Код · Code: [src/Network.cs](../src/Network.cs).

## Обновления · Updates

| Адрес · URL | Зачем · Why | Как часто · How often |
|---|---|---|
| `aion2status.com/update.json` | Подписанный манифест обновления (HTTPS GET) · signed update manifest | через 40 с после запуска, затем раз в 12 ч; сразу — по кнопке «Проверить» (⚙ → «Обновления», трей, «Версия 0.5» в меню панели) · 40 s after start, then every 12 h; at once on "Check" (⚙ → Updates, tray, "Version 0.5" in the panel menu) |
| `api.github.com/repos/.../releases/latest` → `update.json` | Запасной путь, если сайт недоступен · fallback when the site is down | так же, только при ошибке сайта · same, only if the site fails |
| `github.com` / `objects.githubusercontent.com` / `release-assets.githubusercontent.com` / `aion2status.com` | Скачивание нового `.exe` — только с этих хостов, только по твоему клику «Обновить» · download of the new `.exe` — only from these hosts, only on your "Update" click | один раз при установке · once, on install |

Манифест подписан ECDSA P-256 (префикс `AION2UPDATE|…`), новый `.exe` принимается только при совпадении SHA-256. Проверку можно выключить. Код · Code: [src/Update.cs](../src/Update.cs).
The manifest is signed with ECDSA P-256 (prefix `AION2UPDATE|…`); the new `.exe` is accepted only if its SHA-256 matches. The check can be disabled.

## Ссылки, которые открываешь ты · Links you open yourself

Браузер открывается только по твоему клику: страница новостей Steam/plaync, [@Aion2Status_Bot](https://t.me/Aion2Status_Bot) («Купить» — с кодом устройства в ссылке), FunPay.
The browser opens only on your click: Steam/plaync news, the Telegram bot ("Buy" includes the device code in the link), FunPay.
