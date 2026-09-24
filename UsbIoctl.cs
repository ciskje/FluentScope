namespace USBScope.Usb;

/// <summary>
/// IOCTL constants, GUID and flags to read USB information from user mode.
/// Values derived from the usbiodef.h base codes (FILE_DEVICE_USB == FILE_DEVICE_UNKNOWN == 0x22):
///   CTL_CODE(0x22, Function, METHOD_BUFFERED, FILE_ANY_ACCESS) == (0x22 << 16) | (Function << 2)
/// Same approach as Microsoft's official "usbview" application.
/// </summary>
internal static class UsbIoctl
{
    public const uint IOCTL_USB_GET_NODE_INFORMATION = 0x220408;                    // Function 258
    public const uint IOCTL_USB_GET_DESCRIPTOR_FROM_NODE_DEVICE = 0x220410;        // Function 260
    public const uint IOCTL_USB_GET_NODE_CONNECTION_INFORMATION_EX = 0x220448;     // Function 274
    public const uint IOCTL_USB_GET_HUB_INFORMATION_EX = 0x220454;                 // Function 277
    public const uint IOCTL_USB_GET_NODE_CONNECTION_INFORMATION_EX_V2 = 0x22045C;  // Function 279

    // GUID_DEVINTERFACE_USB_HUB (usbiodef.h)
    public static readonly Guid DevInterfaceUsbHub = new("f18a0e88-c30c-11d0-8815-00a0c906bed8");

    // Valori di USB_NODE_CONNECTION_INFORMATION_EX.Speed (USB_DEVICE_SPEED, usb200.h)
    public const byte SpeedLow = 0;   // USB 1.0/1.1 Low-Speed  1.5 Mbit/s
    public const byte SpeedFull = 1;  // USB 1.1 Full-Speed    12 Mbit/s
    public const byte SpeedHigh = 2;  // USB 2.0 High-Speed    480 Mbit/s
    public const byte SpeedSuperSpeed = 3; // valore usato da usbview quando i flag V2 dicono SuperSpeed

    // USB_CONNECTION_STATUS
    public const uint NoDeviceConnected = 0;
    public const uint DeviceConnected = 1;

    // Bit di USB_NODE_CONNECTION_INFORMATION_EX_V2_FLAGS (unione bitfield)
    public const uint FlagOperatingAtSuperSpeedOrHigher = 0x1;
    public const uint FlagSuperSpeedCapableOrHigher = 0x2;
    public const uint FlagOperatingAtSuperSpeedPlusOrHigher = 0x4;
    public const uint FlagSuperSpeedPlusCapableOrHigher = 0x8;

    // Bit di USB_PROTOCOLS.AsUlong32 (unione bitfield)
    public const uint ProtocolUsb110 = 0x1;
    public const uint ProtocolUsb200 = 0x2;
    public const uint ProtocolUsb300 = 0x4;

    // Valori di USB_HUB_TYPE
    public const uint HubTypeUsb11 = 1;
    public const uint HubTypeUsb20 = 2;
    public const uint HubTypeUsb30 = 3;
}
