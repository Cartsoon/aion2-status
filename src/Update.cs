// AION2 Status — открытая выдержка из src/Update.cs (2026-10-22)
// Source-available excerpt for security review. Full source is closed. See ../LICENSE.
//
// Обновления: файл обновления подписан нашим ключом (ECDSA P-256), подпись и SHA-256 проверяются ДО записи exe,
// скачивание только по https с разрешённых адресов, при сбое — откат. Без подписи ничего не ставится.
// Updates: signed manifest (ECDSA P-256) and SHA-256 verified before anything is written; https + host allow-list; rollback.
// L.T(...) — перевод строки интерфейса на английский / UI string translation.

static bool VerifySig(string pubB64, byte[] msg, byte[] sig)
{
    try
    {
        using (CngKey ck = CngKey.Import(Convert.FromBase64String(pubB64), CngKeyBlobFormat.EccPublicBlob))
        using (ECDsaCng ec = new ECDsaCng(ck))
        {
            ec.HashAlgorithm = CngAlgorithm.Sha256;
            return ec.VerifyData(msg, sig);
        }
    }
    catch { return false; }
}

// ……

static bool HostAllowed(string url)
{
    try
    {
        Uri u = new Uri(url);
        string host = u.Host.ToLowerInvariant();
        if (Profile.Test && host == "127.0.0.1") return true;   // локальный тест-сервер только в тестовом профиле
        if (u.Scheme != "https") return false;
        return host == "github.com" || host == "objects.githubusercontent.com"
            || host == "release-assets.githubusercontent.com" || host == "aion2status.com";
    }
    catch { return false; }
}

// ……

// targetExe: обычно сам себя (selfReplace). Для теста — копия exe в отдельной папке.
static bool DoInstall(string targetExe, UpdateInfo info, bool selfReplace, out string err)
{
    err = null;
    string dir = Path.GetDirectoryName(targetExe);
    if (!FolderWritable(dir))
    {
        // page_url не входит в подпись — открываем только https-страницу с разрешённого хоста
        if (selfReplace && !string.IsNullOrEmpty(info.PageUrl) && PageAllowed(info.PageUrl)) Open(info.PageUrl);
        err = NotWritable;
        return false;
    }
    string newPath = targetExe + ".new";
    try { if (File.Exists(newPath)) File.Delete(newPath); } catch { }
    if (!Download(info.ExeUrl, newPath, out err)) { try { File.Delete(newPath); } catch { } return false; }

    string got = Sha256File(newPath);
    if (!string.Equals(got, (info.ExeSha256 ?? "").Trim(), StringComparison.OrdinalIgnoreCase))
    {
        try { File.Delete(newPath); } catch { }
        NotifyUi(L.T("Файл обновления повреждён — отменил установку"));
        err = "sha mismatch: got " + got + " want " + info.ExeSha256;
        return false;
    }

    string oldPath = targetExe + ".old";
    try { if (File.Exists(oldPath)) File.Delete(oldPath); } catch { }
    bool renamed = false;
    try
    {
        File.Move(targetExe, oldPath);          // работающий exe переименовать можно
        renamed = true;
        File.Move(newPath, targetExe);
    }
    catch (Exception ex)
    {
        try { if (renamed && !File.Exists(targetExe) && File.Exists(oldPath)) File.Move(oldPath, targetExe); } catch { }
        err = "swap: " + ex.Message;
        return false;
    }

    int pid = Process.GetCurrentProcess().Id;
    try { Process.Start(targetExe, "--restart-after " + pid + " --updated"); }
    catch (Exception ex)
    {
        err = "start: " + ex.Message;
        // новая версия не запустилась — возвращаем прежний exe на место (новый остаётся рядом как .new)
        try
        {
            File.Move(targetExe, newPath);
            try { File.Move(oldPath, targetExe); }
            catch { if (!File.Exists(targetExe)) File.Move(newPath, targetExe); }
        }
        catch { }
        return false;
    }
    return true;
}
