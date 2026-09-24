namespace USBScope.Live;

/// <summary>Connection/disconnection event detected in live mode.</summary>
public sealed class UsbEventInfo
{
    public string TimeText { get; }
    public string EventKind { get; }
    public string DeviceName { get; }
    public string Vendor { get; }
    public string Standard { get; }
    public string SpeedText { get; }
    public double SpeedMbps { get; }

    public UsbEventInfo(DateTime time, string kind, Usb.USBScopeInfo device)
    {
        TimeText = time.ToString("yyyy-MM-dd HH:mm:ss");
        EventKind = kind;
        DeviceName = device.DeviceName;
        Vendor = device.Vendor;
        Standard = device.Standard;
        SpeedText = device.SpeedText;
        SpeedMbps = device.SpeedMbps;
    }
}
