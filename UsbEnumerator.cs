using System.Runtime.InteropServices;
using System;
using USBScope.Natives;

namespace USBScope.Usb;

/// <summary>
/// Enumerates every USB hub (GUID_DEVINTERFACE_USB_HUB interface, including each root hub)
/// and, for every port, queries the attached device for name, VID/PID, speed and standard.
/// Same approach as Microsoft's official "usbview" application.
/// </summary>
public static class UsbEnumerator
{
    // V1 buffer: base struct (35 bytes) + room for 30 pipes, like usbview does.
    private const int PipeInfoSize = 11; // USB_ENDPOINT_DESCRIPTOR(7) + ULONG(4), pack 1
    private const int MaxPipes = 30;
    private static readonly int V1BufferSize = Marshal.SizeOf<UsbNodeConnectionInformationEx>() + MaxPipes * PipeInfoSize;

    private static readonly IntPtr InvalidHandle = new(-1);

    public static List<USBScopeInfo> Enumerate()
    {
        var result = new List<USBScopeInfo>();
        foreach (var hub in EnumerateHubs())
        {
            var handle = Kernel32.CreateFile(
                hub.InterfacePath, 0,
                Kernel32.FILE_SHARE_READ | Kernel32.FILE_SHARE_WRITE,
                IntPtr.Zero, Kernel32.OPEN_EXISTING, Kernel32.FILE_ATTRIBUTE_NORMAL, IntPtr.Zero);

            if (handle == IntPtr.Zero || handle == InvalidHandle)
            {
                result.Add(new USBScopeInfo
                {
                    DeviceName = $"Devices behind \"{hub.Name}\"",
                    Location = hub.Name,
                    Standard = "—",
                    SpeedText = "—",
                    Status = "Speed unavailable (permissions?)",
                    Available = false,
                    Key = "hub-unreadable:" + hub.InstanceId,
                });
                continue;
            }

            try
            {
                int ports = GetPortCount(handle);
                for (int port = 1; port <= ports; port++)
                {
                    var info = QueryPort(handle, port, hub);
                    if (info != null)
                        result.Add(info);
                }
            }
            finally
            {
                Kernel32.CloseHandle(handle);
            }
        }
        return result;
    }

    private static List<HubInfo> EnumerateHubs()
    {
        var hubs = new List<HubInfo>();
        var guid = UsbIoctl.DevInterfaceUsbHub;
        IntPtr h = SetupApi.SetupDiGetClassDevs(ref guid, IntPtr.Zero, IntPtr.Zero,
            SetupApi.DIGCF_PRESENT | SetupApi.DIGCF_DEVICEINTERFACE);
        if (h == IntPtr.Zero || h == InvalidHandle)
            return hubs;

        try
        {
            for (uint devIndex = 0; ; devIndex++)
            {
                var di = new SetupApi.SP_DEVINFO_DATA { cbSize = Marshal.SizeOf<SetupApi.SP_DEVINFO_DATA>() };
                if (!SetupApi.SetupDiEnumDeviceInfo(h, devIndex, ref di))
                    break; // ERROR_NO_MORE_ITEMS

                string name = GetFriendlyName(h, ref di) ?? "USB hub";
                string instanceId = GetInstanceId(h, ref di);

                // Each devnode can expose one or more USB_HUB interfaces: enumerate them all.
                for (uint ifIndex = 0; ; ifIndex++)
                {
                    var ifd = new SetupApi.SP_DEVICE_INTERFACE_DATA { cbSize = Marshal.SizeOf<SetupApi.SP_DEVICE_INTERFACE_DATA>() };
                    if (!SetupApi.SetupDiEnumDeviceInterfaces(h, ref di, ref guid, ifIndex, ref ifd))
                        break; // ERROR_NO_MORE_ITEMS

                    string? path = SetupApi.TryGetDevicePath(h, ref ifd);
                    if (string.IsNullOrEmpty(path))
                        continue;

                    hubs.Add(new HubInfo
                    {
                        Name = name,
                        InstanceId = instanceId,
                        InterfacePath = path,
                    });
                }
            }
        }
        finally
        {
            SetupApi.SetupDiDestroyDeviceInfoList(h);
        }
        return hubs;
    }

    private static string? GetFriendlyName(IntPtr h, ref SetupApi.SP_DEVINFO_DATA di)
    {
        foreach (uint prop in new[] { SetupApi.SPDRP_FRIENDLYNAME, SetupApi.SPDRP_DEVICEDESC })
        {
            var buf = new byte[512];
            if (SetupApi.SetupDiGetDeviceRegistryProperty(h, ref di, prop, out _, buf, (uint)buf.Length, out _))
            {
                string s = System.Text.Encoding.Unicode.GetString(buf);
                int nul = s.IndexOf('\0');
                if (nul >= 0)
                    s = s[..nul];
                if (!string.IsNullOrWhiteSpace(s))
                    return s;
            }
        }
        return null;
    }

    private static string GetInstanceId(IntPtr h, ref SetupApi.SP_DEVINFO_DATA di)
    {
        var sb = new char[1024];
        if (SetupApi.SetupDiGetDeviceInstanceId(h, ref di, sb, sb.Length, out _))
            return new string(sb).TrimEnd('\0');
        return "";
    }

    private static int GetPortCount(IntPtr h)
    {
        // USB_HUB_INFORMATION_EX has variable size: allocate a wide buffer
        // and only read HighestPortNumber (USHORT at offset 4, after the HubType ULONG).
        const int bufSize = 128;
        IntPtr buf = Marshal.AllocHGlobal(bufSize);
        try
        {
            for (int i = 0; i < bufSize; i++)
                Marshal.WriteByte(buf, i, 0);
            bool ok = Kernel32.DeviceIoControl(h, UsbIoctl.IOCTL_USB_GET_HUB_INFORMATION_EX,
                buf, (uint)bufSize, buf, (uint)bufSize, out _, IntPtr.Zero);
            if (!ok)
                return 0;
            return Math.Max(0, (int)Marshal.ReadInt16(buf, 4));
        }
        finally
        {
            Marshal.FreeHGlobal(buf);
        }
    }

    private static USBScopeInfo? QueryPort(IntPtr h, int port, HubInfo hub)
    {
        var v1 = QueryV1(h, port);
        if (v1 is null || v1.Value.ConnectionStatus != UsbIoctl.DeviceConnected)
            return null;
        var conn = v1.Value;

        uint ssFlags = QueryV2Flags(h, port);
        int? ssPlusGbps = null;
        if ((ssFlags & UsbIoctl.FlagOperatingAtSuperSpeedPlusOrHigher) != 0)
            ssPlusGbps = QuerySsPlusMaxGbps(h, port);
        string standard;
        string speed;
        int rank;
        double mbps;
        (standard, speed, rank, mbps) = MapSpeed(conn.Speed, ssFlags, ssPlusGbps);

        string? iProduct = QueryProductName(h, port, conn.DeviceDescriptor);
        string vendor = UsbIdDatabase.GetVendorOrDefault(conn.DeviceDescriptor.idVendor);
        UsbIdDatabase.TryGetProduct(conn.DeviceDescriptor.idVendor, conn.DeviceDescriptor.idProduct, out string? dbProduct);

        // The PID database enriches the Device column:
        // - iProduct present + known product with a different name → "iProduct (db name)";
        // - iProduct missing → db product name, else vendor-qualified fallback.
        string name;
        if (iProduct is not null)
        {
            name = dbProduct is not null
                && !iProduct.Contains(dbProduct, StringComparison.OrdinalIgnoreCase)
                && !dbProduct.Contains(iProduct, StringComparison.OrdinalIgnoreCase)
                ? $"{iProduct} ({dbProduct})"
                : iProduct;
        }
        else if (dbProduct is not null)
        {
            name = dbProduct;
        }
        else if (vendor != "—")
        {
            name = $"{vendor} USB device (VID_{conn.DeviceDescriptor.idVendor:X4}&PID_{conn.DeviceDescriptor.idProduct:X4})";
        }
        else
        {
            name = $"USB device (VID_{conn.DeviceDescriptor.idVendor:X4}&PID_{conn.DeviceDescriptor.idProduct:X4})";
        }

        return new USBScopeInfo
        {
            DeviceName = name,
            Vendor = vendor,
            VidPid = $"{conn.DeviceDescriptor.idVendor:X4} / {conn.DeviceDescriptor.idProduct:X4}",
            Standard = standard,
            SpeedText = speed,
            SpeedRank = rank,
            SpeedMbps = mbps,
            Location = $"{hub.Name} — port {port}",
            Key = hub.InstanceId + ":" + port + ":" + conn.DeviceDescriptor.idVendor + ":" + conn.DeviceDescriptor.idProduct,
        };
    }

    private static UsbNodeConnectionInformationEx? QueryV1(IntPtr h, int port)
    {
        IntPtr buf = Marshal.AllocHGlobal(V1BufferSize);
        try
        {
            var v = new UsbNodeConnectionInformationEx { ConnectionIndex = (uint)port };
            Marshal.StructureToPtr(v, buf, false);
            bool ok = Kernel32.DeviceIoControl(h, UsbIoctl.IOCTL_USB_GET_NODE_CONNECTION_INFORMATION_EX,
                buf, (uint)V1BufferSize, buf, (uint)V1BufferSize, out _, IntPtr.Zero);
            if (!ok)
                return null;
            return (UsbNodeConnectionInformationEx)Marshal.PtrToStructure(buf, typeof(UsbNodeConnectionInformationEx))!;
        }
        finally
        {
            Marshal.FreeHGlobal(buf);
        }
    }

    /// <summary>Queries the SuperSpeed flags (V2). Returns 0 when the IOCTL is not supported.</summary>
    private static uint QueryV2Flags(IntPtr h, int port)
    {
        int size = Marshal.SizeOf<UsbNodeConnectionInformationExV2>(); // 16 bytes
        IntPtr buf = Marshal.AllocHGlobal(size);
        try
        {
            var v = new UsbNodeConnectionInformationExV2
            {
                ConnectionIndex = (uint)port,
                Length = (uint)size,
            };
            // USB_PROTOCOLS.AsUlong32 bitmask: request USB 2.0 + 3.0.
            v.SupportedUsbProtocols.AsUlong32 = UsbIoctl.ProtocolUsb200 | UsbIoctl.ProtocolUsb300;
            Marshal.StructureToPtr(v, buf, false);
            bool ok = Kernel32.DeviceIoControl(h, UsbIoctl.IOCTL_USB_GET_NODE_CONNECTION_INFORMATION_EX_V2,
                buf, (uint)size, buf, (uint)size, out _, IntPtr.Zero);
            if (!ok)
                return 0;
            var r = (UsbNodeConnectionInformationExV2)Marshal.PtrToStructure(buf, typeof(UsbNodeConnectionInformationExV2))!;
            return r.Flags;
        }
        finally
        {
            Marshal.FreeHGlobal(buf);
        }
    }

    /// <summary>
    /// Reads the iProduct string descriptor (marketing name) via
    /// IOCTL_USB_GET_DESCRIPTOR_FROM_NODE_DEVICE, like usbview does.
    /// </summary>
    private static string? QueryProductName(IntPtr h, int port, UsbDeviceDescriptor desc)
    {
        byte index = desc.iProduct;
        if (index == 0)
            return null;

        ushort langid = QueryLangId(h, port);
        int bufSize = UsbDescriptorRequest.HeaderSize + 512;
        IntPtr buf = Marshal.AllocHGlobal(bufSize);
        try
        {
            var req = new UsbDescriptorRequest
            {
                ConnectionIndex = (uint)port,
                bmRequest = 0x80,           // device-to-host, standard, data-in
                bRequest = 0x06,            // GET_DESCRIPTOR
                wValue = (ushort)((3 << 8) | index), // type: string
                wIndex = langid,
                wLength = 255,
            };
            Marshal.StructureToPtr(req, buf, false);
            bool ok = Kernel32.DeviceIoControl(h, UsbIoctl.IOCTL_USB_GET_DESCRIPTOR_FROM_NODE_DEVICE,
                buf, (uint)bufSize, buf, (uint)bufSize, out uint returned, IntPtr.Zero);
            if (!ok)
                return null;
            // In the reply, descriptor data starts after the 12-byte header:
            // Data[0] = bLength, Data[1] = type (3), then UTF-16LE.
            if (returned < UsbDescriptorRequest.HeaderSize + 4)
                return null;
            int bLength = Marshal.ReadByte(buf, UsbDescriptorRequest.HeaderSize);
            if (bLength < 4 || UsbDescriptorRequest.HeaderSize + bLength > returned + 2)
                return null;
            int charCount = (bLength - 2) / 2;
            if (charCount <= 0)
                return null;
            var chars = new char[charCount];
            Marshal.Copy(IntPtr.Add(buf, UsbDescriptorRequest.HeaderSize + 2), chars, 0, charCount);
            string s = new string(chars).TrimEnd('\0');
            return string.IsNullOrWhiteSpace(s) ? null : s;
        }
        finally
        {
            Marshal.FreeHGlobal(buf);
        }
    }

    /// <summary>First language ID reported by the device (usually 0x0409).</summary>
    private static ushort QueryLangId(IntPtr h, int port)
    {
        int bufSize = UsbDescriptorRequest.HeaderSize + 64;
        IntPtr buf = Marshal.AllocHGlobal(bufSize);
        try
        {
            var req = new UsbDescriptorRequest
            {
                ConnectionIndex = (uint)port,
                bmRequest = 0x80,
                bRequest = 0x06,
                wValue = (ushort)(3 << 8),  // string, index 0 = language ID list
                wLength = 62,
            };
            Marshal.StructureToPtr(req, buf, false);
            bool ok = Kernel32.DeviceIoControl(h, UsbIoctl.IOCTL_USB_GET_DESCRIPTOR_FROM_NODE_DEVICE,
                buf, (uint)bufSize, buf, (uint)bufSize, out uint returned, IntPtr.Zero);
            // The first LANGID is a USHORT at header+2.
            if (ok && returned >= (uint)(UsbDescriptorRequest.HeaderSize + 4)
                  && Marshal.ReadByte(buf, UsbDescriptorRequest.HeaderSize) >= 4)
                return (ushort)Marshal.ReadInt16(buf, UsbDescriptorRequest.HeaderSize + 2);
            return 0x0409; // fallback: en-US
        }
        finally
        {
            Marshal.FreeHGlobal(buf);
        }
    }

    /// <summary>
    /// Reads the device BOS descriptor and derives its declared maximum SuperSpeedPlus speed
    /// (Sublink Speed Attribute mantissa, in Gbit/s). Returns 10, 20 or null.
    /// The actual negotiated speed is min(deviceMax, portMax): if the device declares at most
    /// 10 Gbit/s (Gen2x1, the most common case) and is operating at SuperSpeed+, it is running
    /// at exactly 10 Gbit/s. If it declares 20 (Gen2x2), the actual rate also depends on the port.
    /// </summary>
    private static int? QuerySsPlusMaxGbps(IntPtr h, int port)
    {
        const int HS = UsbDescriptorRequest.HeaderSize;
        const int DS = 1024;
        int bufSize = HS + DS;
        IntPtr buf = Marshal.AllocHGlobal(bufSize);
        try
        {
            var req = new UsbDescriptorRequest
            {
                ConnectionIndex = (uint)port,
                bmRequest = 0x80,
                bRequest = 0x06,
                wValue = (ushort)(15 << 8), // type: BOS
                wIndex = 0,
                wLength = (ushort)DS,
            };
            Marshal.StructureToPtr(req, buf, false);
            bool ok = Kernel32.DeviceIoControl(h, UsbIoctl.IOCTL_USB_GET_DESCRIPTOR_FROM_NODE_DEVICE,
                buf, (uint)bufSize, buf, (uint)bufSize, out uint returned, IntPtr.Zero);
            if (!ok || returned < (uint)(HS + 5))
                return null;
            if (Marshal.ReadByte(buf, HS + 1) != 15) // bDescriptorType != BOS
                return null;
            int total = Marshal.ReadInt16(buf, HS + 2);
            int ncaps = Marshal.ReadByte(buf, HS + 4);
            if (total <= 5 || total > DS || ncaps <= 0 || ncaps > 32)
                return null;
            int end = HS + Math.Min(total, (int)returned);
            int off = HS + 5;
            int maxMantissa = 0;
            bool foundSsPlus = false;
            for (int i = 0; i < ncaps && off + 3 <= end; i++)
            {
                int cl = Marshal.ReadByte(buf, off);
                if (cl < 3 || off + cl > end)
                    break;
                int ct = Marshal.ReadByte(buf, off + 1);
                int st = Marshal.ReadByte(buf, off + 2);
                if (ct == 16 && st == 10) // SuperSpeedPlus Device Capability
                {
                    foundSsPlus = true;
                    int ssac = Marshal.ReadByte(buf, off + 4) & 0xF;
                    int n = Math.Max(ssac, (cl - 12) / 4);
                    if (n > 8)
                        n = 8;
                    for (int j = 0; j < n && off + 12 + 4 * (j + 1) <= off + cl; j++)
                    {
                        int mantissa = Marshal.ReadInt16(buf, off + 12 + 4 * j + 2);
                        if (mantissa > maxMantissa)
                            maxMantissa = mantissa;
                    }
                }
                off += cl;
            }
            if (!foundSsPlus)
                return null;
            return maxMantissa >= 20 ? 20 : 10;
        }
        finally
        {
            Marshal.FreeHGlobal(buf);
        }
    }

    /// <summary>
    /// Maps (V1 Speed, V2 flags) to USB standard and nominal speed.
    /// usbview does the same override: when the V2 flags report SuperSpeed,
    /// the V1 Speed (HighSpeed at most) is replaced.
    /// </summary>
    private static (string Standard, string Speed, int Rank, double Mbps) MapSpeed(byte speed, uint ssFlags, int? ssPlusGbps)
    {
        if ((ssFlags & UsbIoctl.FlagOperatingAtSuperSpeedPlusOrHigher) != 0)
        {
            var (s, mbps) = ssPlusGbps switch
            {
                10 => ("10 Gbit/s", 10000.0),
                20 => ("10-20 Gbit/s", 10000.0),
                _ => ("≥ 10 Gbit/s", 10000.0),
            };
            return ("USB 3.1/3.2 (SuperSpeed+)", s, 4, mbps);
        }
        if ((ssFlags & UsbIoctl.FlagOperatingAtSuperSpeedOrHigher) != 0)
            return ("USB 3.0 (SuperSpeed)", "5 Gbit/s", 3, 5000.0);
        return speed switch
        {
            UsbIoctl.SpeedLow => ("USB 1.0/1.1 Low-Speed", "1.5 Mbit/s", 0, 1.5),
            UsbIoctl.SpeedFull => ("USB 1.1 Full-Speed", "12 Mbit/s", 1, 12.0),
            UsbIoctl.SpeedSuperSpeed => ("USB 3.0 (SuperSpeed)", "5 Gbit/s", 3, 5000.0),
            _ => ("USB 2.0 High-Speed", "480 Mbit/s", 2, 480.0),
        };
    }

    private sealed record HubInfo
    {
        public required string Name { get; init; }
        public required string InstanceId { get; init; }
        public required string InterfacePath { get; init; }
    }
}
