// AION2 Status — открытая выдержка из src/Aion2Status.cs (2026-10-21)
// Source-available excerpt for security review. Full source is closed. See ../LICENSE.
//
// Пинг: ICMP к твоему серверу, а если VPN отвечает на ICMP сам — время ответа HTTP через туннель к cloudflare.com.
// Ping: ICMP to your server; behind a TUN VPN that answers ICMP locally — HTTP keep-alive RTT to cloudflare.com.
// L.T(...) — перевод строки интерфейса на английский / UI string translation.

// ───────────────────────── пинг до твоего сервера (ICMP, не трогает игровой шлюз) ─────────────────────────
sealed class PingMeter
{
    readonly object gate = new object();
    readonly List<int> samples = new List<int>();   // мс; -1 = потеря
    public volatile string Target;                   // IP твоего сервера
    public volatile bool Active;
    public const int Keep = 90;

    public PingMeter()
    {
        var t = new Thread(Loop) { IsBackground = true, Name = "ping" };
        t.Start();
    }

    // VPN с TUN-адаптером (v2RayTun, sing-box, Clash…) отвечает на ICMP сам, с этого же ПК: «1 мс, ±0, TTL 128».
    // Тогда меряем HTTP через туннель: запрос к ближайшему узлу Cloudflare по одному открытому соединению —
    // тот же путь, что у игры (её серверы — Cloudflare Spectrum). Раз в минуту снова пробуем ICMP: VPN могли выключить.
    public volatile bool ViaTunnel;
    int localEchoes, remoteEchoes;
    DateTime nextIcmpCheck = DateTime.MinValue;
    static readonly string[] CfHosts = { "104.16.133.229", "104.16.132.229" };   // cloudflare.com
    int cfIdx;
    TcpClient http;
    System.IO.Stream hs;
    const int Skip = int.MinValue;   // не замер: ответ самого туннеля или первый запрос нового соединения

    void Loop()
    {
        using (var p = new System.Net.NetworkInformation.Ping())
        {
            string last = null;
            while (true)
            {
                var target = Target;
                if (!Active || string.IsNullOrEmpty(target)) { CloseHttp(); Thread.Sleep(700); continue; }
                if (target != last) { lock (gate) samples.Clear(); last = target; }
                var sw = Stopwatch.StartNew();
                int ms;
                if (!ViaTunnel || DateTime.UtcNow >= nextIcmpCheck)
                {
                    bool local;
                    ms = Icmp(p, target, out local);
                    if (local)
                    {
                        localEchoes++;
                        remoteEchoes = 0;
                        if (localEchoes >= 2 && !ViaTunnel) { ViaTunnel = true; lock (gate) samples.Clear(); }
                        nextIcmpCheck = DateTime.UtcNow.AddSeconds(60);
                        ms = ViaTunnel ? HttpRtt() : Skip;
                    }
                    else if (ms >= 0)
                    {
                        localEchoes = 0;
                        if (ViaTunnel)
                        {
                            if (++remoteEchoes < 2) { nextIcmpCheck = DateTime.UtcNow; ms = HttpRtt(); }   // ещё раз проверим в следующую секунду
                            else { remoteEchoes = 0; ViaTunnel = false; CloseHttp(); lock (gate) samples.Clear(); }   // VPN выключили — снова честный ICMP
                        }
                    }
                    else if (ViaTunnel) { nextIcmpCheck = DateTime.UtcNow.AddSeconds(60); ms = HttpRtt(); }   // ICMP не ответил — туннель остаётся, проверим через минуту
                }
                else ms = HttpRtt();
                if (ms != Skip) lock (gate) { samples.Add(ms); if (samples.Count > Keep) samples.RemoveAt(0); }
                int rest = 1000 - (int)sw.ElapsedMilliseconds;
                if (rest > 0) Thread.Sleep(rest);
            }
        }
    }

    // ICMP; local — ответил не сервер, а свой ПК (≤1 мс и TTL как у Windows, а у ответа из интернета TTL меньше 100)
    static int Icmp(System.Net.NetworkInformation.Ping p, string target, out bool local)
    {
        local = false;
        for (int attempt = 0; attempt < 2; attempt++)   // промах — сразу повтор: ICMP иногда режется, а игра при этом в порядке
        {
            try
            {
                var r = p.Send(target, attempt == 0 ? 1200 : 900);
                if (r.Status == System.Net.NetworkInformation.IPStatus.Success)
                {
                    local = r.RoundtripTime <= 5 && r.Options != null && r.Options.Ttl >= 100;
                    return (int)Math.Max(1, r.RoundtripTime);
                }
            }
            catch { }
        }
        return -1;
    }

    // время до первого байта ответа на GET по уже открытому соединению; -1 — потеря; Skip — первый запрос (в нём ещё установка соединения VPN)
    int HttpRtt()
    {
        try
        {
            bool fresh = false;
            if (http != null && hs != null && http.Connected && http.Client.Poll(0, SelectMode.SelectRead) && http.Client.Available == 0) CloseHttp();   // сервер закрыл простаивавшее соединение
            if (http == null || !http.Connected || hs == null)
            {
                CloseHttp();
                http = new TcpClient { NoDelay = true, ReceiveTimeout = 2500, SendTimeout = 2500 };
                var ar = http.BeginConnect(CfHosts[cfIdx % CfHosts.Length], 80, null, null);
                if (!ar.AsyncWaitHandle.WaitOne(2500)) { CloseHttp(); cfIdx++; return -1; }
                http.EndConnect(ar);
                hs = http.GetStream();
                fresh = true;
            }
            var req = Encoding.ASCII.GetBytes("GET /cdn-cgi/trace HTTP/1.1\r\nHost: cloudflare.com\r\nUser-Agent: AION2Status-ping\r\nConnection: keep-alive\r\n\r\n");
            var sw = Stopwatch.StartNew();
            hs.Write(req, 0, req.Length);
            int first = ReadResponse(sw);
            if (first < 0) { CloseHttp(); return -1; }
            return fresh ? Skip : Math.Max(1, first);
        }
        catch { CloseHttp(); cfIdx++; return -1; }
    }

    // дочитываем ответ целиком (иначе следующий замер прочтёт хвост этого); возвращает мс до первого байта
    int ReadResponse(Stopwatch sw)
    {
        var buf = new byte[4096];
        var acc = new System.IO.MemoryStream();
        int first = -1, headEnd = -1;
        while (headEnd < 0)
        {
            int n = hs.Read(buf, 0, buf.Length);
            if (n <= 0) return -1;
            if (first < 0) first = (int)sw.ElapsedMilliseconds;
            acc.Write(buf, 0, n);
            var all = acc.GetBuffer();
            for (int i = 3; i < acc.Length; i++)
                if (all[i - 3] == 13 && all[i - 2] == 10 && all[i - 1] == 13 && all[i] == 10) { headEnd = i + 1; break; }
            if (acc.Length > 65536) return -1;
        }
        string head = Encoding.ASCII.GetString(acc.GetBuffer(), 0, headEnd);
        int have = (int)acc.Length - headEnd;
        bool close = Regex.IsMatch(head, @"(?im)^connection:\s*close");
        var cl = Regex.Match(head, @"(?im)^content-length:\s*(\d+)");
        if (cl.Success)
        {
            int need = int.Parse(cl.Groups[1].Value) - have;
            while (need > 0) { int n = hs.Read(buf, 0, Math.Min(buf.Length, need)); if (n <= 0) return -1; need -= n; }
        }
        else if (Regex.IsMatch(head, @"(?im)^transfer-encoding:\s*chunked"))
        {
            var body = new StringBuilder(Encoding.ASCII.GetString(acc.GetBuffer(), headEnd, have));
            while (!body.ToString().EndsWith("0\r\n\r\n"))
            {
                int n = hs.Read(buf, 0, buf.Length);
                if (n <= 0) return -1;
                body.Append(Encoding.ASCII.GetString(buf, 0, n));
                if (body.Length > 65536) return -1;
            }
        }
        else close = true;   // длина не указана — ответ кончится закрытием соединения
        if (close) CloseHttp();
        return first;
    }

    void CloseHttp()
    {
        try { if (hs != null) hs.Dispose(); } catch { }
        try { if (http != null) http.Close(); } catch { }
        hs = null; http = null;
    }

    public int[] Samples() { lock (gate) return samples.ToArray(); }

    // последний ответ, среднее, разброс (средний скачок между соседними замерами), потери % — по последним 30 замерам
    public void Stats(out int last, out int avg, out int jitter, out int lossPct, out int count)
    {
        var a = Samples();
        last = -1; avg = -1; jitter = -1; lossPct = 0; count = a.Length;
        if (a.Length == 0) return;
        last = a[a.Length - 1];
        var w = a.Skip(Math.Max(0, a.Length - 30)).ToArray();
        var ok = w.Where(x => x >= 0).ToArray();
        lossPct = (int)Math.Round(100.0 * (w.Length - ok.Length) / w.Length);
        if (ok.Length == 0) return;
        avg = (int)Math.Round(ok.Average());
        if (ok.Length > 1)
        {
            double sum = 0;
            for (int i = 1; i < ok.Length; i++) sum += Math.Abs(ok[i] - ok[i - 1]);
            jitter = (int)Math.Round(sum / (ok.Length - 1));
        }
        else jitter = 0;
    }
}
