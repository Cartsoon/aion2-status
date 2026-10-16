// AION2 Status — открытая выдержка из src/License.cs (2026-10-17)
// Source-available excerpt for security review. Full source is closed. See ../LICENSE.
//
// Код устройства для ключа Премиума: 8 байт SHA-256 от MachineGuid — сам GUID никто не видит.
// Плагин его никуда не отправляет: к боту он попадает, только когда ты сам жмёшь «Купить» (ссылка t.me/…?start=hw_…).
// Device code for the Premium key: 8 bytes of SHA-256(MachineGuid). The plugin never sends it; it reaches the bot
// only inside the t.me link you open yourself with «Buy».
// L.T(...) — перевод строки интерфейса на английский / UI string translation.

// код устройства: SHA-256(«AION2STATUS|» + MachineGuid) — первые 8 байт
public static byte[] Hwid
{
    get
    {
        if (hwid != null) return hwid;
        string guid = "";
        try
        {
            using (var hk = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
            using (var k = hk.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography"))
                if (k != null) guid = Convert.ToString(k.GetValue("MachineGuid", ""));
        }
        catch { }
        if (string.IsNullOrEmpty(guid)) guid = Environment.MachineName;
        using (var sha = SHA256.Create())
            hwid = sha.ComputeHash(Encoding.UTF8.GetBytes("AION2STATUS|" + guid.ToLowerInvariant())).Take(8).ToArray();
        return hwid;
    }
}
