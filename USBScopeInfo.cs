namespace USBScope.Usb;

/// <summary>Device table row: one USB device attached to a hub port.</summary>
public sealed class USBScopeInfo
{
    public string DeviceName { get; init; } = "";
    public string Vendor { get; init; } = "—";
    public string VidPid { get; init; } = "—";
    public string Standard { get; init; } = "—";
    public string SpeedText { get; init; } = "—";
    public int SpeedRank { get; init; } = -1; // -1 = unknown, 0 = Low, 1 = Full, 2 = High, 3 = SS, 4 = SS+
    public double SpeedMbps { get; init; } = -1; // numeric sort key in Mbit/s (-1 = unknown)
    public string Location { get; init; } = "";
    public string Status { get; init; } = "OK";
    public bool Available { get; init; } = true;

    /// <summary>Stable identifier used by the live monitor to diff snapshots.</summary>
    public string Key { get; init; } = "";
}
