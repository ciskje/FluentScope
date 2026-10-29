namespace FluentScope.Live;

using System.IO;

/// <summary>Appends live events to events.log under %LOCALAPPDATA%\FluentScope.</summary>
public static class EventLogWriter
{
    public static string LogPath
    {
        get
        {
            // %LOCALAPPDATA%: the program folder may be read-only
            // (e.g. C:\Program Files installs), so logs always go here.
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "FluentScope");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "events.log");
        }
    }

    public static void Append(UsbEventInfo e)
    {
        try
        {
            File.AppendAllText(LogPath,
                $"{e.TimeText} | {e.EventKind} | {e.Vendor} | {e.DeviceName} | {e.Standard} | {e.SpeedText}\n");
        }
        catch
        {
            // Logging is optional: it must never crash the application.
        }
    }
}
