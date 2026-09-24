using System.Globalization;
using System.IO;
using System.Reflection;

namespace USBScope.Usb;

/// <summary>
/// Offline VID/PID database trimmed from linux-usb.org usb.ids
/// (embedded resource <c>Data/usb.ids</c>: vendor lines + device lines only).
/// Provides the vendor (marca) for every known VID and the product name
/// for known VID/PID pairs. Parsing is lazy and thread-safe.
/// </summary>
public static class UsbIdDatabase
{
    private const string ResourceName = "USBScope.Data.usb.ids";

    private static readonly Lazy<Db> _db = new(Load, isThreadSafe: true);

    public static int VendorCount => _db.Value.Vendors.Count;
    public static int ProductCount => _db.Value.Products.Count;

    public static bool TryGetVendor(ushort vid, out string vendor)
        => _db.Value.Vendors.TryGetValue(vid, out vendor!);

    public static string GetVendorOrDefault(ushort vid, string defaultValue = "—")
        => TryGetVendor(vid, out var v) ? v : defaultValue;

    public static bool TryGetProduct(ushort vid, ushort pid, out string product)
        => _db.Value.Products.TryGetValue(Key(vid, pid), out product!);

    private static int Key(ushort vid, ushort pid) => (vid << 16) | pid;

    private sealed class Db
    {
        public required Dictionary<ushort, string> Vendors { get; init; }
        public required Dictionary<int, string> Products { get; init; }
    }

    private static Db Load()
    {
        var vendors = new Dictionary<ushort, string>(4096);
        var products = new Dictionary<int, string>(22000);
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName);
        if (stream is null)
            return new Db { Vendors = vendors, Products = products };
        using var reader = new StreamReader(stream);
        ushort currentVid = 0;
        bool hasVid = false;
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (line.Length == 0 || line[0] == '#')
                continue;
            if (line[0] == '\t')
            {
                // Product line: "\tPID  name" (deeper levels ignored).
                if (!hasVid || line.Length < 2 || line[1] == '\t' || line.Length < 6)
                    continue;
                if (!ushort.TryParse(line.AsSpan(1, 4), NumberStyles.HexNumber, null, out ushort pid))
                    continue;
                string name = line.Length > 7 ? line[7..].Trim() : "";
                if (name.Length == 0)
                    continue;
                products.TryAdd(Key(currentVid, pid), name);
            }
            else
            {
                // Vendor line: "VID  name".
                if (line.Length < 6)
                {
                    hasVid = false;
                    continue;
                }
                if (!ushort.TryParse(line.AsSpan(0, 4), NumberStyles.HexNumber, null, out ushort vid))
                {
                    hasVid = false;
                    continue;
                }
                string name = line.Length > 6 ? line[6..].Trim() : "";
                if (name.Length == 0)
                {
                    hasVid = false;
                    continue;
                }
                vendors.TryAdd(vid, name);
                currentVid = vid;
                hasVid = true;
            }
        }
        return new Db { Vendors = vendors, Products = products };
    }
}
