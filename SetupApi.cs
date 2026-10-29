using System.Runtime.InteropServices;

namespace FluentScope.Natives;

/// <summary>SetupAPI functions to enumerate USB hub devices (GUID_DEVINTERFACE_USB_HUB interface).</summary>
internal static partial class SetupApi
{
    public const uint DIGCF_PRESENT = 0x00000002;
    public const uint DIGCF_DEVICEINTERFACE = 0x00000010;
    public const uint SPDRP_DEVICEDESC = 0;
    public const uint SPDRP_FRIENDLYNAME = 12;
    public const uint ERROR_NO_MORE_ITEMS = 259;
    public const uint ERROR_INSUFFICIENT_BUFFER = 122;

    [StructLayout(LayoutKind.Sequential)]
    public struct SP_DEVINFO_DATA
    {
        public int cbSize;
        public Guid ClassGuid;
        public uint DevInst;
        public IntPtr Reserved;
    }

    /// <summary>
    /// Real SP_DEVICE_INTERFACE_DATA (setupapi.h): cbSize + GUID + Flags + Reserved.
    /// It holds no path: that comes from SetupDiGetDeviceInterfaceDetail.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct SP_DEVICE_INTERFACE_DATA
    {
        public int cbSize;
        public Guid InterfaceClassGuid;
        public uint Flags;
        public IntPtr Reserved;
    }

    [DllImport("setupapi.dll", SetLastError = true)]
    public static extern IntPtr SetupDiGetClassDevs(
        ref Guid ClassGuid,
        IntPtr Enumerator,
        IntPtr hwndParent,
        uint Flags);

    [DllImport("setupapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetupDiEnumDeviceInfo(IntPtr hDevInfo, uint MemberIndex, ref SP_DEVINFO_DATA DeviceInfoData);

    [DllImport("setupapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetupDiEnumDeviceInterfaces(
        IntPtr hDevInfo,
        ref SP_DEVINFO_DATA DeviceInfoData,
        ref Guid InterfaceClassGuid,
        uint MemberIndex,
        ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData);

    // DeviceInfoData opzionale = NULL: si passa IntPtr.Zero.
    [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetupDiGetDeviceInterfaceDetail(
        IntPtr hDevInfo,
        ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData,
        IntPtr DeviceInterfaceDetailData,
        uint DeviceInterfaceDetailDataSize,
        out uint RequiredSize,
        IntPtr DeviceInfoData);

    [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetupDiGetDeviceRegistryProperty(
        IntPtr hDevInfo,
        ref SP_DEVINFO_DATA DeviceInfoData,
        uint Property,
        out uint PropertyRegDataType,
        byte[]? PropertyBuf,
        uint PropertyBufSize,
        out uint RequiredSize);

    [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetupDiGetDeviceInstanceId(
        IntPtr hDevInfo,
        ref SP_DEVINFO_DATA DeviceInfoData,
        char[] DeviceInstanceId,
        int DeviceInstanceIdSize,
        out int RequiredSize);

    [DllImport("setupapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetupDiDestroyDeviceInfoList(IntPtr hDevInfo);

    /// <summary>
    /// Two-step SetupDiGetDeviceInterfaceDetail: first for the required size,
    /// then for the path (\\?\usb#...). Returns null when the detail is unreadable.
    /// </summary>
    public static string? TryGetDevicePath(IntPtr hDevInfo, ref SP_DEVICE_INTERFACE_DATA ifd)
    {
        // 1. Richiede la dimensione necessaria.
        SetupDiGetDeviceInterfaceDetail(hDevInfo, ref ifd, IntPtr.Zero, 0, out uint required, IntPtr.Zero);
        if (required < 6)
            return null;

        IntPtr detail = Marshal.AllocHGlobal((int)required);
        try
        {
            // cbSize: 8 on x64, 4 + sizeof(WCHAR) = 6 on x86 (per MSDN docs).
            int cbSize = IntPtr.Size == 8 ? 8 : 4 + Marshal.SystemDefaultCharSize;
            Marshal.WriteInt32(detail, cbSize);

            if (!SetupDiGetDeviceInterfaceDetail(hDevInfo, ref ifd, detail, required, out _, IntPtr.Zero))
                return null;

            // DevicePath (WCHAR[]) starts right after the cbSize DWORD → offset 4.
            string? path = Marshal.PtrToStringUni(IntPtr.Add(detail, 4));
            return string.IsNullOrWhiteSpace(path) ? null : path;
        }
        finally
        {
            Marshal.FreeHGlobal(detail);
        }
    }
}
