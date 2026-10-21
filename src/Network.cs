// AION2 Status — открытая выдержка из src/Aion2Status.cs (2026-10-22)
// Source-available excerpt for security review. Full source is closed. See ../LICENSE.
//
// Все сетевые подключения к игровым серверам: одно пустое TCP-подключение без данных (порт 13328/13700),
// повторная проба мимо VPN, HTTP GET к публичным лентам (Steam, plaync, shugo.gg, aion2timers).
// All connections to game servers: one empty TCP connect with no payload, an optional retry outside the VPN,
// and plain HTTP GETs of public feeds.
// L.T(...) — перевод строки интерфейса на английский / UI string translation.

static class Net
{
    static bool Split(string ep, out IPAddress ip, out int port)
    {
        ip = null; port = 0;
        int i = ep.LastIndexOf(':');
        return i > 0 && IPAddress.TryParse(ep.Substring(0, i), out ip) && int.TryParse(ep.Substring(i + 1), out port);
    }

    static IPAddress directCache;
    static DateTime directAt = DateTime.MinValue;

    // Локальный адрес физической сети (Ethernet/Wi-Fi со шлюзом), если основной маршрут идёт через VPN; иначе null.
    public static IPAddress DirectAddress()
    {
        if ((DateTime.UtcNow - directAt).TotalSeconds < 60) return directCache;
        directAt = DateTime.UtcNow;
        directCache = null;
        try
        {
            IPAddress def = null;
            using (var u = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                u.Connect(new IPEndPoint(IPAddress.Parse("1.1.1.1"), 53));
                def = ((IPEndPoint)u.LocalEndPoint).Address;
            }
            foreach (var ni in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != System.Net.NetworkInformation.OperationalStatus.Up) continue;
                var t = ni.NetworkInterfaceType;
                if (t != System.Net.NetworkInformation.NetworkInterfaceType.Ethernet && t != System.Net.NetworkInformation.NetworkInterfaceType.Wireless80211
                    && t != System.Net.NetworkInformation.NetworkInterfaceType.GigabitEthernet) continue;
                string name = (ni.Name + " " + ni.Description).ToLowerInvariant();
                if (Regex.IsMatch(name, "wintun|wireguard|amnezia|tap|tun|vpn|virtual|hyper-v|vethernet|vmware|virtualbox|loopback")) continue;
                var props = ni.GetIPProperties();
                if (!props.GatewayAddresses.Any(g => g.Address.AddressFamily == AddressFamily.InterNetwork && !g.Address.Equals(IPAddress.Any))) continue;
                foreach (var ua in props.UnicastAddresses)
                {
                    if (ua.Address.AddressFamily != AddressFamily.InterNetwork) continue;
                    if (ua.Address.Equals(def)) return directCache = null;   // VPN не основной маршрут — перепроверять незачем
                    return directCache = ua.Address;
                }
            }
        }
        catch { }
        return directCache;
    }

    // Подключиться и подождать holdMs: сервер сам закрыл соединение (Closed) или держит его открытым (Open).
    public static Probe Run(string ep, int holdMs, IPAddress bind)
    {
        var r = new Probe { Kind = PK.Fail, ConnectMs = -1, CloseMs = -1 };
        IPAddress ip; int port;
        if (!Split(ep, out ip, out port)) return r;
        Socket s = null;
        try
        {
            s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            if (bind != null) s.Bind(new IPEndPoint(bind, 0));
            var sw = Stopwatch.StartNew();
            var ar = s.BeginConnect(ip, port, null, null);
            if (!ar.AsyncWaitHandle.WaitOne(5000)) return r;
            s.EndConnect(ar);
            r.ConnectMs = (int)sw.ElapsedMilliseconds;
            r.Kind = PK.Open;
            if (holdMs <= 0) return r;
            s.ReceiveTimeout = holdMs;
            try
            {
                int n = s.Receive(new byte[256]);
                r.CloseMs = (int)sw.ElapsedMilliseconds;
                r.Kind = n == 0 ? PK.Closed : PK.Data;
            }
            catch (SocketException e)
            {
                if (e.SocketErrorCode == SocketError.TimedOut) r.Kind = PK.Open;
                else { r.Kind = PK.Reset; r.CloseMs = (int)sw.ElapsedMilliseconds; }
            }
        }
        catch { }
        finally { if (s != null) s.Close(); }
        return r;
    }
}

// ……

// Одна проба: пускает / не пускает / нет ответа. Если через VPN «не пускает», перепроверяем напрямую:
// сервер может отбивать только адрес VPN (лимит подключений), а сам при этом работать.
static ServerInfo ProbeServer(string ep)
{
    var info = new ServerInfo { Ep = ep, At = DateTime.UtcNow };
    var r = Net.Run(ep, HoldMs, null);
    info.Ms = r.ConnectMs;
    info.State = Classify(r);
    if (info.State == Srv.Rejects)
    {
        var direct = Net.DirectAddress();
        if (direct != null)
        {
            var d = Net.Run(ep, HoldMs, direct);
            if (Classify(d) == Srv.Accepts) { info.State = Srv.Accepts; info.ViaDirect = true; info.Ms = d.ConnectMs; }
        }
    }
    return info;
}

// ……

internal static string Get(string url)
{
    using (var wc = new TimedClient())
    {
        wc.Encoding = Encoding.UTF8;
        wc.Headers[HttpRequestHeader.UserAgent] = "AION2Status/1.0";
        return wc.DownloadString(url);
    }
}
