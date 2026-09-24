namespace USBScope.Live;

/// <summary>
/// Diffs a snapshot against the previous one and produces
/// connection/disconnection events (live mode).
/// </summary>
public sealed class LiveMonitor
{
    private readonly Dictionary<string, Usb.USBScopeInfo> _last = new();

    /// <summary>Updates the state and returns the events since the first snapshot.</summary>
    public List<UsbEventInfo> Refresh(List<Usb.USBScopeInfo> current)
    {
        var events = new List<UsbEventInfo>();
        var now = DateTime.Now;
        var currentKeys = new HashSet<string>(current.Select(d => d.Key));

        foreach (var d in current)
            if (!_last.ContainsKey(d.Key) && d.Available)
                events.Add(new UsbEventInfo(now, "Connected", d));

        foreach (var kv in _last)
            if (!currentKeys.Contains(kv.Key))
                events.Add(new UsbEventInfo(now, "Disconnected", kv.Value));

        _last.Clear();
        foreach (var d in current)
            _last[d.Key] = d;
        return events;
    }

    /// <summary>Updates the state without producing events (manual refresh).</summary>
    public void SyncOnly(List<Usb.USBScopeInfo> current)
    {
        _last.Clear();
        foreach (var d in current)
            _last[d.Key] = d;
    }
}
