using GestionMagasin.Data;
using System.Windows.Forms;

namespace GestionMagasin.WinForms;

/// <summary>Lance un backup automatique par date locale une fois par jour lorsque l'application est ouverte.</summary>
internal sealed class AutomaticBackupScheduler : IDisposable
{
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 60_000 };
    private readonly SqliteBackupService _backup;
    private DateOnly _lastBackupDate;
    public AutomaticBackupScheduler(SqliteBackupService backup)
    {
        _backup = backup; _timer.Tick += (_, _) => Check(); _timer.Start(); Check();
    }
    private void Check()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        if (_lastBackupDate == today) return;
        try { _backup.CreerBackup(true); _lastBackupDate = today; }
        catch (Exception ex) { System.Diagnostics.Trace.TraceError("Backup automatique échoué: {0}", ex); }
    }
    public void Dispose() => _timer.Dispose();
}
