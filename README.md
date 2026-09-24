# USBScope

WPF (.NET 8) program showing at which **speed** and **USB standard** the machine's
USB devices are connected, with a live mode that detects connections/disconnections
and logs them.

## Run

```bash
dotnet run --project USBScope.csproj          # development start (or debug.bat)
dotnet publish USBScope.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish   # release build (or publish.bat)
```

## Installer (setup)

Run `setup.bat`: it publishes a self-contained single-file Release build
(no .NET runtime needed on the target machine) and creates
`installer\USBScope-Setup.exe` with Inno Setup 6 (auto-detected in the standard
locations). The setup installs per-machine to `C:\Program Files\USBScope`
and requires administrator rights: Start Menu shortcut (+ optional Desktop shortcut),
Add/Remove Programs entry with uninstaller. The setup version is read
automatically from the published exe (see Versioning below).
The live-mode log file lives in
`%LOCALAPPDATA%\USBScope\events.log` (the program folder is read-only).
The setup is unsigned, so on first run Windows SmartScreen shows a warning:
click "More info" → "Run anyway" to proceed.

## Versioning

The version auto-increments at build time (`USBScope.csproj`):
`Major.YY.MMDD.HHmm` (e.g. `1.26.0924.2047`), shown in `? → About USBScope`
and in the exe properties. Bump `Major` manually for breaking releases.
The Inno Setup script reads the version from the published exe, so the
installer always matches the build with no manual sync.

## What it shows

| Column | Meaning |
|---|---|
| Device | Marketing name (USB string descriptor), enriched with the PID database name when both exist and differ |
| Marca | Vendor brand from the offline VID database (`Data\usb.ids`, trimmed from linux-usb.org `usb.ids`) |
| VID / PID | Vendor/Product ID from the device descriptor |
| USB standard | USB 1.1 / 2.0 / 3.0 / 3.1-3.2 (SuperSpeed, SuperSpeed+) |
| Speed | Actual **negotiated** speed (1.5 Mbit/s … 20 Gbit/s) |
| Hub / Port | Parent hub and port number |
| Status | OK / unreadable hub (permissions) |

Rows are colored by speed: red = Low/Full-Speed, dark green = SuperSpeed+.

Tip: clicking any row (devices or events) copies it to the clipboard,
tab-separated, ready to paste into Excel.

## Live mode

Checking **Live mode** re-takes the snapshot every 2 seconds and, for every
connected/disconnected device, writes a row to the **Event log** and appends to
`events.log` (in `%LOCALAPPDATA%\USBScope`), e.g.:

```
2026-09-24 18:02:11 | Connected | Western Digital | Western Digital My Passport | USB 3.1/3.2 (SuperSpeed+) | ≥ 10 Gbit/s
```

## VID/PID database (marca)

`Data\usb.ids` is an offline database trimmed from linux-usb.org `usb.ids`
(3.427 vendors, ~20.500 devices): vendor lines + device lines only
(comments, C and interface lines removed), embedded in the exe.
Every known VID resolves to the **Marca** column; every known VID/PID
enriches the **Device** column (used as fallback when the device reports
no `iProduct` string, appended as `Name (db name)` when both exist
and differ). Unknown IDs keep the previous behavior.
Run `tools\Update-UsbIds.ps1` to refresh it from upstream, then rebuild.

## Menu

- **File → Export HTML report**: saves the device table to an `.html` file
  (title, date/time, computer, per-standard summary, rows colored as in the window).
- **File → Exit**: closes the program.
- **? → About USBScope**: program version and description.

## How it works (technical detail)

Same approach as Microsoft's official **usbview** application:

1. SetupAPI: enumerate every device with interface
   `GUID_DEVINTERFACE_USB_HUB` (root hubs included).
2. `CreateFile` on each hub's interface.
3. `IOCTL_USB_GET_HUB_INFORMATION_EX` → port count.
4. For each port:
   - `IOCTL_USB_GET_NODE_CONNECTION_INFORMATION_EX` → device, VID/PID, speed (up to High-Speed);
   - `IOCTL_USB_GET_NODE_CONNECTION_INFORMATION_EX_V2` → SuperSpeed / SuperSpeed+ flags;
   - `IOCTL_USB_GET_DESCRIPTOR_FROM_NODE_DEVICE` → marketing name (iProduct string).

Zero NuGet dependencies: only P/Invoke of `kernel32.dll` and `setupapi.dll`.

## Notes and limitations

- If a hub cannot be opened (some systems require elevation), the corresponding rows
  show "Speed unavailable": running the program as administrator fixes it.
- The speed shown is the currently negotiated one, not the device's advertised maximum:
  exactly the metric you need to understand "why is it slow".
- SuperSpeed+: the exact speed comes from the device's BOS descriptor
  (SuperSpeedPlus capability): if it reports at most 10 Gbit/s (Gen2x1, the most common case)
  it shows exact "10 Gbit/s"; if it reports 20 (Gen2x2) it shows "10-20 Gbit/s" since the
  actual rate also depends on the port; if BOS is unreadable it shows "≥ 10 Gbit/s".
