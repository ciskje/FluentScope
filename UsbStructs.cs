using System.Runtime.InteropServices;

namespace FluentScope.Usb;

// Pack = 1 layout: usbioctl.h structs are declared under #include <pshpack1.h>.

/// <summary>USB_DEVICE_DESCRIPTOR (usbd.h), 18 byte.</summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct UsbDeviceDescriptor
{
    public byte bLength;
    public byte bDescriptorType;
    public ushort bcdUSB;
    public byte bDeviceClass;
    public byte bDeviceSubClass;
    public byte bDeviceProtocol;
    public byte bMaxPacketSize0;
    public ushort idVendor;
    public ushort idProduct;
    public ushort bcdDevice;
    public byte iManufacturer;
    public byte iProduct;
    public byte iSerialNumber;
    public byte bNumConfigurations;
}

/// <summary>
/// USB_NODE_CONNECTION_INFORMATION_EX (usbioctl.h), 35 bytes without the flexible PipeList.
/// The flexible PipeList is excluded: extra bytes are still allocated in the buffer.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct UsbNodeConnectionInformationEx
{
    public uint ConnectionIndex;
    public UsbDeviceDescriptor DeviceDescriptor;
    public byte CurrentConfigurationValue;
    public byte Speed;
    public byte DeviceIsHub;
    public ushort DeviceAddress;
    public uint NumberOfOpenPipes;
    public uint ConnectionStatus;
}

/// <summary>
/// USB_PROTOCOLS (usbioctl.h): 4-byte union (bitfield + AsUlong32).
/// Bits are: Usb110 = 0x1, Usb200 = 0x2, Usb300 = 0x4 (see UsbIoctl).
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct UsbProtocols
{
    public uint AsUlong32;
}

/// <summary>USB_NODE_CONNECTION_INFORMATION_EX_V2 (usbioctl.h), 16 byte.</summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct UsbNodeConnectionInformationExV2
{
    public uint ConnectionIndex;
    public uint Length;
    public UsbProtocols SupportedUsbProtocols;
    public uint Flags;
}

/// <summary>
/// USB_HUB_INFORMATION_EX (usbioctl.h): leading HubType (ULONG) + HighestPortNumber (USHORT).
/// The rest is a variable-size union: when reading, allocate a wide buffer
/// (128 bytes) and only use HighestPortNumber at offset 4.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct UsbHubInformationEx
{
    public uint HubType;
    public ushort HighestPortNumber;
}

/// <summary>
/// USB_DESCRIPTOR_REQUEST (usbioctl.h): ConnectionIndex (ULONG, 4) + SetupPacket (8) = 12 bytes,
/// followed by the descriptor data. Reply data therefore starts at offset 12.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct UsbDescriptorRequest
{
    public const int HeaderSize = 12;

    public uint ConnectionIndex;
    public byte bmRequest;
    public byte bRequest;
    public ushort wValue;
    public ushort wIndex;
    public ushort wLength;
}
